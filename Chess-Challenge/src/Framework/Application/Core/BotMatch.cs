using ChessChallenge.BotMatch;


namespace ChessChallenge.Application
{
    static class BotMatch
    {
        public static void BotMatchMain()
        {
            MatchRunner runner = new(MatchRunner.PlayerType.MyBot, MatchRunner.PlayerType.EvilBot, 60 * 1000);
            runner.Run();
        }
    }
}
