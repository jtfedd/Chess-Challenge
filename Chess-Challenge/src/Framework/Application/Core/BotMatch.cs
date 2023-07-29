using ChessChallenge.BotMatch;
using System.Linq;


namespace ChessChallenge.Application
{
    static class BotMatch
    {
        public static void BotMatchMain()
        {
            string[] startFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

            MatchParams matchParams = new(
                MatchBots.A,
                MatchBots.B,
                startFens,
                60 * 1000,
                10
            );

            MatchController runner = new(matchParams);
            runner.Run();
        }
    }
}
