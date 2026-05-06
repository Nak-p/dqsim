// Assets/Scripts/Framework/Battle/BattleTurnManager.cs
// AgentSim — バトルターン管理・プレイヤー入力処理・敵 AI 実行

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public class BattleTurnManager : MonoBehaviour
    {
        public const string PhaseIdle         = "idle";
        public const string PhasePlayerMove   = "player_move";
        public const string PhasePlayerAction = "player_action";
        public const string PhasePlayerTarget = "player_target";
        public const string PhaseEnemyTurn    = "enemy_turn";
        public const string PhaseBattleOver   = "battle_over";

        // ── 公開プロパティ ─────────────────────────────────────────────
        public BattleUnit       ActiveUnit     { get; private set; }
        public string           Phase          { get; private set; } = PhaseIdle;
        public ActionDef        SelectedAction { get; private set; }
        public int              CurrentTurn    { get; private set; }
        public bool             PlayerWon      { get; private set; }
        public List<BattleUnit> TurnOrder      { get; private set; } = new List<BattleUnit>();

        public event Action OnStateChanged;

        // ── 内部状態 ──────────────────────────────────────────────────
        private BattleGrid         _grid;
        private List<BattleUnit>   _allUnits;
        private Tilemap            _hexTilemap;
        private BattleUnitRenderer _unitRenderer;
        private Camera             _camera;
        private int                _turnIndex;

        // ── 初期化 ────────────────────────────────────────────────────
        public void Initialize(BattleGrid grid, List<BattleUnit> allUnits,
                               Tilemap hexTilemap, BattleUnitRenderer unitRenderer)
        {
            _grid         = grid;
            _allUnits     = allUnits;
            _hexTilemap   = hexTilemap;
            _unitRenderer = unitRenderer;
            _camera       = Camera.main;

            // ハイライト用 GameObject の親を作成
            var hlParent = new GameObject("BattleHighlights");
            BattleHighlightRenderer.SetParent(hlParent.transform);

            CurrentTurn = 1;
            BuildTurnOrder();
            _turnIndex = 0;
            AdvanceTurn();
        }

        // ── プレイヤー操作 ────────────────────────────────────────────
        public void OnActionSelected(ActionDef action)
        {
            if (Phase != PhasePlayerAction) return;
            SelectedAction = action;
            EnterPhase(PhasePlayerTarget);
        }

        public void EndPlayerTurn()
        {
            if (Phase != PhasePlayerAction && Phase != PhasePlayerMove) return;
            AdvanceTurn();
        }

        public void CancelTarget()
        {
            if (Phase != PhasePlayerTarget) return;
            SelectedAction = null;
            EnterPhase(PhasePlayerAction);
        }

        /// <summary>
        /// 指定方向に向きを変える。後方3方向への転換は AP 1 を消費する。
        /// AP が不足している場合は転換しない。
        /// </summary>
        public void TryChangeFacing(int dirIndex)
        {
            if (ActiveUnit == null) return;
            if (dirIndex == ActiveUnit.Facing) return;

            bool isFront = BattleMovement.IsFrontDirection(ActiveUnit.Facing, dirIndex);
            if (!isFront)
            {
                float rearPen = SettingsRegistry.Current.Game.battle_rear_ap_penalty;
                if (ActiveUnit.CurrentAp < rearPen) return;
                ActiveUnit.SpendAp(rearPen);
            }

            ActiveUnit.Facing = dirIndex;
            RefreshHighlights();
            OnStateChanged?.Invoke();
        }

        // ── Update（入力） ────────────────────────────────────────────
        private void Update()
        {
            if (Phase == PhaseIdle || Phase == PhaseEnemyTurn || Phase == PhaseBattleOver)
                return;

            var mouse    = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
                HandleClick();

            if (keyboard.escapeKey.wasPressedThisFrame && Phase == PhasePlayerTarget)
                CancelTarget();
        }

        // ── ターン進行 ────────────────────────────────────────────────
        private void AdvanceTurn()
        {
            BuildTurnOrder();
            if (TurnOrder.Count == 0) return;

            _turnIndex     = _turnIndex % TurnOrder.Count;
            ActiveUnit     = TurnOrder[_turnIndex];
            _turnIndex     = (_turnIndex + 1) % TurnOrder.Count;
            CurrentTurn++;
            SelectedAction = null;

            int maxTurns = SettingsRegistry.Current.Game.battle_max_turns;
            if (CurrentTurn > maxTurns + 1)
            {
                PlayerWon = false;
                EnterPhase(PhaseBattleOver);
                return;
            }

            ActiveUnit.ResetAp();

            if (ActiveUnit.Team == BattleTeam.Player)
                EnterPhase(PhasePlayerMove);
            else
            {
                EnterPhase(PhaseEnemyTurn);
                StartCoroutine(RunEnemyTurn(ActiveUnit));
            }
        }

        private void BuildTurnOrder()
        {
            TurnOrder = _allUnits
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.GetSpeedStat())
                .ToList();
        }

        private void EnterPhase(string phase)
        {
            Phase = phase;
            RefreshHighlights();
            OnStateChanged?.Invoke();
        }

        // ── マウスクリック処理 ────────────────────────────────────────
        private void HandleClick()
        {
            if (_camera == null || _hexTilemap == null) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPos = _camera.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, -_camera.transform.position.z));
            worldPos.z = 0f;

            // ── 移動フェーズ中: 方向三角形クリックを最優先チェック ──
            if (Phase == PhasePlayerMove && ActiveUnit != null)
            {
                if (BattleHighlightRenderer.TryGetClickedDirection(
                        worldPos, _hexTilemap, ActiveUnit.Position, out int dirIdx))
                {
                    TryChangeFacing(dirIdx);
                    return;
                }
            }

            var cellPos = _hexTilemap.WorldToCell(worldPos);
            var hex     = new HexCoord(cellPos.x, cellPos.y);

            if (!_grid.IsInBounds(hex)) return;

            if (Phase == PhasePlayerMove)
                HandleMoveClick(hex);
            else if (Phase == PhasePlayerTarget)
                HandleTargetClick(hex);
        }

        private void HandleMoveClick(HexCoord hex)
        {
            if (hex.Equals(ActiveUnit.Position))
            {
                EnterPhase(PhasePlayerAction);
                return;
            }

            var (frontR, rearR) = BattleMovement.GetReachableSplit(ActiveUnit, _grid);
            var reachable = new System.Collections.Generic.HashSet<AgentSim.Battle.HexCoord>(frontR);
            reachable.UnionWith(rearR);
            if (!reachable.Contains(hex)) return;

            float apCost = SettingsRegistry.Current.Game.battle_move_ap_cost;
            int steps    = HexCoord.Distance(ActiveUnit.Position, hex);

            int newFacing = BattleMovement.GetDirectionToward(ActiveUnit.Position, hex);
            _grid.MoveUnit(ActiveUnit, hex);
            ActiveUnit.Facing = newFacing;
            _unitRenderer.MoveUnitSmooth(ActiveUnit, hex);  // アニメーション開始（待機なし）
            ActiveUnit.SpendAp(apCost * steps);

            EnterPhase(PhasePlayerAction);
        }

        private void HandleTargetClick(HexCoord hex)
        {
            if (SelectedAction == null) return;

            var targets = BattleMovement.GetActionTargets(
                ActiveUnit.Position, SelectedAction.range,
                _grid, ActiveUnit.Team, SelectedAction.category);

            if (!targets.Contains(hex)) return;

            var target = _grid.GetUnit(hex);
            if (target == null || !target.IsAlive) return;

            ExecuteAction(ActiveUnit, SelectedAction, target);
            SelectedAction = null;
            EnterPhase(PhasePlayerAction);
        }

        // ── アクション実行 ────────────────────────────────────────────
        private void ExecuteAction(BattleUnit actor, ActionDef action, BattleUnit target)
        {
            int amount = BattleActionResolver.Resolve(actor, action);
            BattleActionResolver.Apply(target, action, amount);
            actor.SpendAp(action.cost);

            if (!target.IsAlive)
            {
                _grid.RemoveUnit(target);
                _unitRenderer.RemoveUnit(target);
            }

            CheckBattleEnd();
            OnStateChanged?.Invoke();
        }

        private void CheckBattleEnd()
        {
            bool anyPlayerAlive = _allUnits.Any(u => u.IsAlive && u.Team == BattleTeam.Player);
            bool anyEnemyAlive  = _allUnits.Any(u => u.IsAlive && u.Team == BattleTeam.Enemy);

            if (!anyPlayerAlive) { PlayerWon = false; EnterPhase(PhaseBattleOver); }
            else if (!anyEnemyAlive) { PlayerWon = true;  EnterPhase(PhaseBattleOver); }
        }

        // ── 敵 AI コルーチン ──────────────────────────────────────────
        private IEnumerator RunEnemyTurn(BattleUnit enemy)
        {
            yield return null;

            if (!enemy.IsAlive || Phase == PhaseBattleOver) yield break;

            var decision = BattleEnemyAI.Decide(enemy, _grid, _allUnits);

            if (decision.MoveTarget.HasValue)
            {
                float apCost = SettingsRegistry.Current.Game.battle_move_ap_cost;
                var to       = decision.MoveTarget.Value;
                int steps    = HexCoord.Distance(enemy.Position, to);

                int enemyNewFacing = BattleMovement.GetDirectionToward(enemy.Position, to);
                _grid.MoveUnit(enemy, to);
                enemy.Facing = enemyNewFacing;
                var moveAnim = _unitRenderer.MoveUnitSmooth(enemy, to);
                if (moveAnim != null) yield return moveAnim;
                enemy.SpendAp(apCost * steps);

                OnStateChanged?.Invoke();
                yield return null;
            }

            if (Phase == PhaseBattleOver) yield break;

            if (decision.Action != null && decision.ActionTarget != null)
            {
                ExecuteAction(enemy, decision.Action, decision.ActionTarget);
                yield return null;
            }

            if (Phase != PhaseBattleOver)
                AdvanceTurn();
        }

        // ── ハイライト更新 ────────────────────────────────────────────
        private void RefreshHighlights()
        {
            if (_hexTilemap == null) return;

            if (Phase == PhasePlayerMove)
            {
                var (front, rear) = BattleMovement.GetReachableSplit(ActiveUnit, _grid);

                // 攻撃可能エリア: 現在地のみから計算（移動先は含まない）
                var attackHexes = ComputeAttackRangeFromCurrentPos(ActiveUnit);

                BattleHighlightRenderer.ShowMoveRange(
                    front, rear, attackHexes, ActiveUnit.Position,
                    ActiveUnit.Facing, _hexTilemap);
            }
            else if (Phase == PhasePlayerTarget && SelectedAction != null)
            {
                // 射程内の全マス（味方ユニット占有セルを除外）
                var rangeHexes = BattleMovement.GetActionRangeHexes(
                    ActiveUnit.Position, SelectedAction.range, _grid,
                    ActiveUnit.Team, SelectedAction.category);
                // 実際に攻撃できるマス（ユニット在籍）
                var validTargets = BattleMovement.GetActionTargets(
                    ActiveUnit.Position, SelectedAction.range,
                    _grid, ActiveUnit.Team, SelectedAction.category);
                BattleHighlightRenderer.ShowActionTargets(
                    rangeHexes, validTargets, ActiveUnit.Position, _hexTilemap);
            }
            else
            {
                BattleHighlightRenderer.Clear();
            }
        }

        /// <summary>
        /// 現在地のみから攻撃できる敵マスを収集する。
        /// </summary>
        private System.Collections.Generic.HashSet<HexCoord> ComputeAttackRangeFromCurrentPos(
            BattleUnit unit)
        {
            var result  = new System.Collections.Generic.HashSet<HexCoord>();
            var actions = SettingsRegistry.Current?.Actions?.actions;
            if (actions == null) return result;

            foreach (var action in actions)
                result.UnionWith(BattleMovement.GetActionTargets(
                    unit.Position, action.range, _grid, unit.Team, action.category));

            return result;
        }
    }
}


