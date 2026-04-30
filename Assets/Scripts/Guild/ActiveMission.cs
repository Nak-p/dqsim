using System;
using System.Collections.Generic;

namespace DQSim
{
    public enum ActiveMissionState
    {
        TravelingOut,
        TravelingBack,
        Complete
    }

    public class ActiveMission
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public Quest Quest;
        public List<Adventurer> Party;
        public PartyController PartyController;
        public ActiveMissionState State = ActiveMissionState.TravelingOut;
        public GameTime ETAArrive;
        public GameTime ETAReturn;

        public string StatusText => State switch
        {
            ActiveMissionState.TravelingOut  => "Traveling",
            ActiveMissionState.TravelingBack => "Returning",
            ActiveMissionState.Complete      => "Complete",
            _ => "?"
        };

        public GameTime CurrentETA => State == ActiveMissionState.TravelingOut ? ETAArrive : ETAReturn;
    }
}
