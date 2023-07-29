﻿using Raylib_cs;
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
        public static void Main()
        {
            Console.WriteLine("MyBot Tokens: " + ChallengeController.GetTokenCount());
            BotMatch.BotMatchMain();
            //Program.ProgramMain();
        }
    }
}