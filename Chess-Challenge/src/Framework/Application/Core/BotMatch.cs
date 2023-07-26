using ChessChallenge.BotMatch;


namespace ChessChallenge.Application
{
    static class BotMatch
    {
        public static void BotMatchMain()
        {
            MatchParams matchParams = new(
                PlayerType.MyBot,
                PlayerType.EvilBot,
                60 * 1000
            );

            MatchRunner runner = new(matchParams);
            runner.Run();
        }
    }
}
