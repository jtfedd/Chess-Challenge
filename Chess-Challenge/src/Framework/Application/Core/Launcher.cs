using System;
using ChessChallenge.API;

namespace ChessChallenge.Application
{
    static class Launcher
    {
        public static void Main(string[] args)
        {
            printTokens();
            if (args[0] == "botmatch") BotMatch.BotMatchMain();
            if (args[0] == "program") Program.ProgramMain();
            if (args[0] == "benchmark") Benchmarks.Benchmarks.Run();
            if (args[0] == "sandbox") sandbox();
        }

        static void printTokens()
        {
            var (total, debug) = ChallengeController.GetTokenCount();
            Console.WriteLine($"MyBot Tokens - Effective: {total - debug}, Total: {total}, Debug: {debug}");
        }

        static void sandbox()
        {
            var b = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");
            Console.WriteLine(b.ZobristKey);

            var bot = new MyBot();
            bot.benchmarkSearch(b, 10);
        }
    }
}