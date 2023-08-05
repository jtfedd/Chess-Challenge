using System;

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
        }

        static void printTokens()
        {
            var (total, debug) = ChallengeController.GetTokenCount();
            Console.WriteLine($"MyBot Tokens - Total: {total}, Effective: {total - debug}, Debug: {debug}");
        }
    }
}