﻿using ChessChallenge.BotMatch;
using System.Linq;


namespace ChessChallenge.Application
{
    static class BotMatch
    {
        public static void BotMatchMain()
        {
            string[] startFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

            MatchParams matchParams = new(
                BotType.MyBot,
                BotType.EvilBot,
                startFens,
                60 * 1000
            );

            MatchRunner runner = new(matchParams);
            runner.Run();
        }
    }
}
