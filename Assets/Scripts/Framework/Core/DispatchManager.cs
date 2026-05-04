// Assets/Scripts/Framework/Core/DispatchManager.cs
// AgentSim — 派遣型経営の中核ロジック（MonoBehaviour）
//
// 担当:
//   ① ロスター・案件・進行中ミッションの状態管理
//   ② パーティ編成と案件への派遣
//   ③ ミッションの往復ステートマシン（PartyController と連携）
//   ④ 報酬計算と分配（組織取り分 / キャラクター取り分）
//   ⑤ 定期的な新規案件の自動生成
//
// 全パラメータは SettingsRegistry（JSON）から読み込む。
// C# にゲーム固有の数値・名称をハードコーディングしてはいけない。

using System;
using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Map;
using AgentSim.Party;
using AgentSim.Pathfinding;
using AgentSim.Systems;

namespace AgentSim.Core
{
    public class DispatchManager : MonoBehaviour
    {
        // ── 状態 ──────────────────────────────────────────────────────
        /// <summary>組織の現在通貨残高</summary>
        public int OrgCurrency { get; private set; }

        /// <summary>組織の累計獲得通貨</summary>
        public int TotalOrgEarned { get; private set; }

        public List<Character>         Roster             { get; } = new List<Character>();
        public List<Contract>      AvailableContracts { get; } = new List<Contract>();
        public List<ActiveMission> ActiveMissions     { get; } = new List<ActiveMission>();

        // ── イベント ──────────────────────────────────────────────────
        /// <summary>状態が変化したとき（UI 更新のトリガーに使う）</summary>
        public event Action OnStateChanged;

        /// <summary>派遣が開始されたとき</summary>
        public event Action<ActiveMission> OnMissionDispatched;

        /// <summary>ミッションが完了（帰還）したとき</summary>
        public event Action<ActiveMission> OnMissionComplete;

        /// <summary>新しい案件が追加されたとき</summary>
        public event Action<Contract> OnContractAdded;

        // ── 依存オブジェクト ──────────────────────────────────────────
        private MapData     _map;
        private TimeManager _timeManager;
        private System.Random _rng;

        // ── 案件生成タイマー ──────────────────────────────────────────
        private float _lastContractGenDay = -1f;

        // ── パーティ色（視覚的に区別するためのアルゴリズム定数） ────
        private int _partyColorIndex;
        private static readonly Color32[] PartyColors =
        {
            new Color32(0xf0, 0xd0, 0x60, 0xff), // 金
            new Color32(0x60, 0xc0, 0xf0, 0xff), // 水色
            new Color32(0xf0, 0x70, 0x70, 0xff), // 赤
            new Color32(0x70, 0xf0, 0x70, 0xff), // 緑
            new Color32(0xe0, 0x70, 0xe0, 0xff), // 紫
        };

        // ── 初期化 ────────────────────────────────────────────────────
        /// <summary>
        /// Play モード開始直後に呼ぶ。
        /// GameBootstrap（または MapTest など）から MapData と TimeManager を渡すこと。
        /// </summary>
        public void Initialize(MapData map, TimeManager timeManager, int seed = 42)
        {
            _map         = map;
            _timeManager = timeManager;
            _rng         = new System.Random(seed);

            var cfg = SettingsRegistry.Current.Game;
            OrgCurrency = cfg.initial_currency;

            GenerateRoster(cfg.roster_size);
            GenerateInitialContracts(cfg.initial_contracts);

            OnStateChanged?.Invoke();

            Debug.Log($"[DispatchManager] 初期化完了: {cfg.organization_name} " +
                      $"| {cfg.characters_term} {Roster.Count}名 " +
                      $"| {cfg.contracts_term} {AvailableContracts.Count}件 " +
                      $"| 初期通貨 {OrgCurrency}{cfg.currency_symbol}");
        }

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Update()
        {
            if (_timeManager == null || _timeManager.IsPaused) return;

            var cfg = SettingsRegistry.Current?.Game;
            if (cfg == null) return;

            // 定期的な案件追加
            float currentDay = _timeManager.CurrentTime.Day;
            if (_lastContractGenDay < 0f) _lastContractGenDay = currentDay;

            if (currentDay - _lastContractGenDay >= cfg.contract_gen_interval_days)
            {
                _lastContractGenDay = currentDay;
                if (AvailableContracts.Count < cfg.max_available_contracts)
                {
                    var contract = GenerateContract();
                    AvailableContracts.Add(contract);
                    OnContractAdded?.Invoke(contract);
                    OnStateChanged?.Invoke();
                }
            }
        }

        // ── 派遣バリデーション ────────────────────────────────────────
        /// <summary>
        /// party が contract の条件を満たすか検証する。
        /// 条件: 人数範囲・全員 IsAvailable・最低ティア
        /// </summary>
        public bool CanDispatch(Contract contract, List<Character> party)
        {
            if (party == null || party.Count < contract.MinPartySize) return false;
            if (party.Count > contract.MaxPartySize) return false;

            var minTier = contract.MinTier;

            foreach (var character in party)
            {
                if (!character.IsAvailable) return false;

                if (minTier != null)
                {
                    var characterTier = SettingsRegistry.Current.GetTier(character.TierId);
                    if (characterTier == null || characterTier.index < minTier.index) return false;
                }
            }
            return true;
        }

        // ── 派遣 ──────────────────────────────────────────────────────
        /// <summary>
        /// contract に party を派遣する。
        /// 成功すれば ActiveMission を返す。失敗なら null。
        /// </summary>
        public ActiveMission Dispatch(Contract contract, List<Character> party)
        {
            if (!CanDispatch(contract, party))
            {
                Debug.LogWarning("[DispatchManager] 派遣条件を満たしていません。");
                return null;
            }

            // A* で経路計算
            var path = Pathfinder.FindPath(_map, _map.BasePosition, contract.Destination);
            if (path == null)
            {
                Debug.LogError($"[DispatchManager] 経路が見つかりません: {contract.Destination}");
                return null;
            }

            // エージェントをロックし、案件を掲示板から除去
            AvailableContracts.Remove(contract);
            foreach (var character in party) character.IsAvailable = false;

            // PartyController を動的生成
            var go = new GameObject($"Party_{contract.Title}");
            var pc = go.AddComponent<PartyController>();
            pc.Initialize(_timeManager, PartyColors[_partyColorIndex % PartyColors.Length]);
            _partyColorIndex++;

            // ETA 計算（往路・復路の移動時間は同じと仮定）
            float travelHours = TravelCalculator.CalculateTravelHours(path, _map);
            var   mission     = new ActiveMission
            {
                Contract        = contract,
                Party           = party,
                PartyController = pc,
                State           = MissionState.TravelingOut,
                EtaArrive       = _timeManager.GetFutureTime(travelHours),
                EtaReturn       = _timeManager.GetFutureTime(travelHours * 2f), // 暫定（帰路確定後に更新）
            };

            // PartyController に経路を渡して移動開始
            pc.StartMoving(path, toDestination: true);
            pc.OnArrivedAtDestination += () => HandleArrivedAtDestination(mission);

            ActiveMissions.Add(mission);
            OnMissionDispatched?.Invoke(mission);
            OnStateChanged?.Invoke();

            Debug.Log($"[DispatchManager] 派遣: {contract.Title} " +
                      $"| パーティ {party.Count}名 | 到着予定 {mission.EtaArrive}");
            return mission;
        }

        // ── ミッション ステートマシン ─────────────────────────────────
        private void HandleArrivedAtDestination(ActiveMission mission)
        {
            if (mission.State != MissionState.TravelingOut) return;
            mission.State = MissionState.TravelingBack;

            // 帰路を計算
            var returnPath = Pathfinder.FindPath(
                _map, mission.Contract.Destination, _map.BasePosition);

            if (returnPath == null)
            {
                Debug.LogError("[DispatchManager] 帰路が見つかりません。ミッションを強制完了します。");
                ForceComplete(mission);
                return;
            }

            float returnHours = TravelCalculator.CalculateTravelHours(returnPath, _map);
            mission.EtaReturn = _timeManager.GetFutureTime(returnHours);

            mission.PartyController.StartMoving(returnPath, toDestination: false);
            mission.PartyController.OnArrivedAtBase += () => HandleReturnedToBase(mission);

            OnStateChanged?.Invoke();
            Debug.Log($"[DispatchManager] 目的地到着: {mission.Contract.Title} " +
                      $"| 帰還予定 {mission.EtaReturn}");
        }

        private void HandleReturnedToBase(ActiveMission mission)
        {
            if (mission.State != MissionState.TravelingBack) return;
            CompleteAndReward(mission);
        }

        private void ForceComplete(ActiveMission mission)
        {
            CompleteAndReward(mission);
        }

        private void CompleteAndReward(ActiveMission mission)
        {
            mission.State = MissionState.Complete;

            DistributeRewards(mission);

            foreach (var character in mission.Party) character.IsAvailable = true;
            ActiveMissions.Remove(mission);

            if (mission.PartyController != null)
                Destroy(mission.PartyController.gameObject);

            OnMissionComplete?.Invoke(mission);
            OnStateChanged?.Invoke();

            Debug.Log($"[DispatchManager] ミッション完了: {mission.Contract.Title} " +
                      $"| 報酬 {mission.Contract.Reward}");
        }

        // ── 報酬分配 ──────────────────────────────────────────────────
        /// <summary>
        /// Largest-remainder 法で報酬を端数なく配分する。
        /// 各エージェントの取り分は TierDef.reward_weight に比例する。
        /// 組織取り分は game_config.json の org_share_percent から読み込む。
        /// </summary>
        private void DistributeRewards(ActiveMission mission)
        {
            var cfg    = SettingsRegistry.Current.Game;
            int reward = mission.Contract.Reward;
            var party  = mission.Party;
            int count  = party?.Count ?? 0;

            // 組織取り分
            int orgShare = reward * cfg.org_share_percent / 100;
            OrgCurrency   += orgShare;
            TotalOrgEarned += orgShare;

            if (count <= 0) return;

            // エージェント分配プール
            int partyPool = reward - orgShare;

            // ティアの reward_weight で比例配分
            float totalWeight = 0f;
            foreach (var character in party)
            {
                var tier = SettingsRegistry.Current.GetTier(character.TierId);
                totalWeight += tier?.reward_weight ?? 1f;
            }
            if (totalWeight <= 0f) totalWeight = count;

            // まず floor で切り捨て計算
            var exactShares  = new float[count];
            var payouts      = new int[count];
            var fractional   = new float[count];
            int distributed  = 0;

            for (int i = 0; i < count; i++)
            {
                var tier = SettingsRegistry.Current.GetTier(party[i].TierId);
                float w      = tier?.reward_weight ?? 1f;
                exactShares[i]  = partyPool * (w / totalWeight);
                payouts[i]      = Mathf.FloorToInt(exactShares[i]);
                fractional[i]   = exactShares[i] - payouts[i];
                distributed    += payouts[i];
            }

            // 端数を小数部の大きい順に 1 ずつ加算（Largest-remainder 法）
            int remainder = partyPool - distributed;
            while (remainder > 0)
            {
                int best = 0;
                for (int i = 1; i < count; i++)
                    if (fractional[i] > fractional[best]) best = i;
                payouts[best]++;
                fractional[best] = -1f;
                remainder--;
            }

            // 各エージェントに付与
            for (int i = 0; i < count; i++)
            {
                party[i].CurrentCurrency += payouts[i];
                party[i].EarnedCurrency  += payouts[i];
            }
        }

        // ── 初期データ生成 ────────────────────────────────────────────
        private void GenerateRoster(int count)
        {
            for (int i = 0; i < count; i++)
                Roster.Add(Character.Generate(_rng));
        }

        private void GenerateInitialContracts(int count)
        {
            for (int i = 0; i < count; i++)
                AvailableContracts.Add(GenerateContract());
        }

        private Contract GenerateContract()
        {
            var tiers = SettingsRegistry.Current.Tiers.tiers;
            int tierIdx = _rng.Next(tiers.Length);
            var dest    = _map.GetRandomWalkableTile(_rng, _map.BasePosition);
            return Contract.Generate(dest, tierIdx, _rng);
        }
    }
}
