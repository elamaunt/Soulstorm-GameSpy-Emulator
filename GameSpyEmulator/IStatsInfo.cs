namespace GameSpyEmulator
{
    public interface IStatsInfo
    {
        string FavouriteRace { get; }
        int Best1v1Winstreak { get; }
        long AverageDuration { get; }
        int Disconnects { get; }
        int WinsCount { get; }
        int GamesCount { get; }
        int StarsCount { get; }
        int Score3v3_4v4 { get; }
        int Score2v2 { get; }
        int Score1v1 { get; }
        long ModifiedTimeTick { get; }
    }
}
