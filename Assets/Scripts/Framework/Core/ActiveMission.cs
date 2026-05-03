// Assets/Scripts/Framework/Core/ActiveMission.cs
// AgentSim — 実行中の派遣ミッションの状態管理
//
// ステートマシン: TravelingOut → TravelingBack → Complete

using System;
using System.Collections.Generic;

namespace AgentSim.Core
{
    public enum MissionState
    {
        TravelingOut,   // ベース → 目的地
        TravelingBack,  // 目的地 → ベース
        Complete,       // 帰還・完了
    }

    public class ActiveMission
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string Id { get; } = Guid.NewGuid().ToString();

        // ── 内容 ──────────────────────────────────────────────────────
        public Contract      Contract;
        public List<Agent>   Party;

        // ── 状態 ──────────────────────────────────────────────────────
        public MissionState  State = MissionState.TravelingOut;

        // ── ETA（ゲーム内時刻） ───────────────────────────────────────
        // GameTime 型はまだ未定義なので float (totalSeconds) で保持
        public float EtaArrive;   // 目的地到着予定時刻（game seconds）
        public float EtaReturn;   // 帰還予定時刻（game seconds）

        // ── 表示テキスト ──────────────────────────────────────────────
        public string StatusText
        {
            get
            {
                return State switch
                {
                    MissionState.TravelingOut  => "Traveling",
                    MissionState.TravelingBack => "Returning",
                    MissionState.Complete      => "Complete",
                    _                          => "Unknown",
                };
            }
        }

        public float CurrentEta =>
            State == MissionState.TravelingOut ? EtaArrive : EtaReturn;
    }
}
