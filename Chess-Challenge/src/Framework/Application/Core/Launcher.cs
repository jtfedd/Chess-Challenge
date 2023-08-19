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
            if (args[0] == "sandbox") searchBench();
            if (args[0] == "pvtables") PVTables.Run();
        }

        static void printTokens()
        {
            var (total, debug) = ChallengeController.GetTokenCount();
            Console.WriteLine($"MyBot Tokens - Remaining: {1024 - total + debug} Effective: {total - debug}, Total: {total}, Debug: {debug}");
        }

        static void searchBench()
        {
            DateTime now = DateTime.Now;

            var b = Board.CreateBoardFromFEN("rn1q1rk1/pp2b1pp/3pbn2/4p3/8/1N1BBN2/PPP2PPP/R2Q1RK1 b - - 9 11");
            Console.WriteLine(b.ZobristKey);

            var bot = new MyBot();
            bot.benchmarkSearch(b, 7);

            Console.WriteLine(DateTime.Now - now);
        }

        static void testEval()
        {
            var b = Board.CreateBoardFromFEN("3qk3/pppppppp/8/8/4K3/8/PPPPPPPP/3Q4 w - - 0 1");
            var bot = new MyBot();
            var eval = bot.testEval(b);
            Console.WriteLine("Final eval: " + eval);
        }
    }
}