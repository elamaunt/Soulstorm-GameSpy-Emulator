using System;

namespace GameSpyEmulator
{
    public class GameFinishedData
    {
        public string Map { get; set; }
        public string SessionId { get; set; }
        public string ModName { get; set; }
        public string ModVersion { get; set; }
        public GameType Type { get; set; }
        public long Duration { get; set; }
        public DateTime Date { get; set; }

        public PlayerData[] Players { get; set; }
        public bool IsRateGame { get; set; }
    }
}
