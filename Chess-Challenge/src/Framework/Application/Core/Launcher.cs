using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChessChallenge.Application.ConsoleHelper;


namespace ChessChallenge.Application
{
    static class Launcher
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("MyBot Tokens: " + ChallengeController.GetTokenCount());
            if (args[0] == "botmatch") BotMatch.BotMatchMain();
            if (args[0] == "program") Program.ProgramMain();
        }
    }
}