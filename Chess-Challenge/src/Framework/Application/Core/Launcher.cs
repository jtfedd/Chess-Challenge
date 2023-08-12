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
            var b = Board.CreateBoardFromFEN("r2qk2r/1pp1p1pp/2nb1n2/1PpPNp2/2Bp1Bb1/3QPN2/P1P2PPP/R4RK1 w Qkq - 0 1");
            Console.WriteLine(b.ZobristKey);
        }
    }
}