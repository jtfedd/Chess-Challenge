

using ChessChallenge.Chess;
using System;

namespace ChessChallenge.BotMatch
{
    public class BotMatchStats
    {
        public string BotName;
        public int NumWins;
        public int NumLosses;
        public int NumDraws;
        public int NumTimeouts;
        public int NumIllegalMoves;
        public int TotalGames => NumWins + NumLosses + NumDraws;

        public BotMatchStats(string name) => BotName = name;

        public void UpdateStats(GameResult result, bool isWhiteStats)
        {
            // Draw
            if (Arbiter.IsDrawResult(result))
            {
                NumDraws++;
            }
            // Win
            else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
            {
                NumWins++;
            }
            // Loss
            else
            {
                NumLosses++;
                NumTimeouts += result is GameResult.WhiteTimeout or GameResult.BlackTimeout ? 1 : 0;
                NumIllegalMoves += result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove ? 1 : 0;
            }
        }

        public void Print()
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(BotName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"+{NumWins}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" ={NumDraws} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"-{NumLosses}");
            Console.WriteLine();

            float winPct = (float)NumWins / TotalGames;
            float drawPct = (float)NumDraws / TotalGames;
            float lossPct = (float)NumLosses / TotalGames;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {(int)Math.Round(winPct * 100)}% ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" {(int)Math.Round(drawPct * 100)}% ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($" {(int)Math.Round(lossPct * 100)}% ");
            Console.WriteLine();

            int barSize = 50;
            Console.ForegroundColor = ConsoleColor.Green;
            for (int i = 0; i < winPct * barSize; i++) Console.Write("-");

            Console.ForegroundColor = ConsoleColor.Gray;
            for (int i = 0; i < drawPct * barSize; i++) Console.Write("-");

            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < lossPct * barSize; i++) Console.Write("-");

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Timeouts: {NumTimeouts}");
            Console.WriteLine($"Illegal Moves: {NumIllegalMoves}");
            Console.ResetColor();
        }
    }
}