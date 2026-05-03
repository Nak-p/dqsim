// Assets/Scripts/Framework/Core/ActiveMission.cs
// AgentSim — 実行中の派遣ミッションの状態管理
//
// ステートマシン: TravelingOut → TravelingBack → Complete

using System;
using System.Collections.Generic;
using AgentSim.Core;

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
        public GameTime EtaArrive;   // 目的地到着予定時刻
        public GameTime EtaReturn;   // 帰還予定時刻

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

        public GameTime CurrentEta =>
            State == MissionState.TravelingOut ? EtaArrive : EtaReturn;
    }
}
