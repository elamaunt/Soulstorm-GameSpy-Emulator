namespace GameSpyEmulator
{
    internal class CustomStatsInfo : IStatsInfo
    {
        public string FavouriteRace => "space_marine_race";

        public int Best1v1Winstreak => 33;

        public long AverageDuration => 10000;

        public int Disconnects => 1;

        public int WinsCount => 50;

        public int GamesCount => 100;

        public int StarsCount => 5;

        public int Score3v3_4v4 => 1234;

        public int Score2v2 => 1344;

        public int Score1v1 => 1555;

        public long ModifiedTimeTick => 10000;
    }
}
