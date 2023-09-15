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
            if (args[0] == "sandbox") testSearch();
            if (args[0] == "pvtables") PVTables.Run();
            if (args[0] == "tune") Tuning.Tuning.Run();
        }

        static void printTokens()
        {
            var (total, debug) = ChallengeController.GetTokenCount();
            Console.WriteLine($"MyBot Tokens - Remaining: {1024 - total + debug} Effective: {total - debug}, Total: {total}, Debug: {debug}");
        }

        static void testSearch()
        {
            DateTime now = DateTime.Now;

            var b = Board.CreateBoardFromFEN("rn1q1rk1/pp2b1pp/3pbn2/6N1/4p3/1N1BB3/PPP2PPP/R2Q1RK1 b - - 1 12");
            Console.WriteLine(b.ZobristKey);

            var bot = new MyBot();
            bot.benchmarkSearch(b, 7);

            Console.WriteLine(DateTime.Now - now);
        }

        static void testEval()
        {
            var b = Board.CreateBoardFromFEN("8/p1p5/1kp1q3/8/pP4p1/2P5/1K1R4/3R4 w - - 0 1");
            var bot = new MyBot();
            var eval = bot.testEval(b);
            Console.WriteLine("eval: " + eval);
        }
    }
}