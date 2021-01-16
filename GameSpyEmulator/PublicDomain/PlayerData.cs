namespace GameSpyEmulator
{
    public class PlayerData
    {
        public string Name { get; set; }
        public int Team { get; set; }
        public string Race { get; set; }
        public PlayerFinalState FinalState { get; set; }
        public long Rating { get; set; }
        public long RatingDelta { get; set; }
    }
}
