

using ChessChallenge.Chess;
using System;
using System.Threading;

namespace ChessChallenge.BotMatch
{
    public class BotStats
    {
        public string BotName;

        public int NumWins;
        public int NumLosses;
        public int NumDraws;

        public int NumTimeouts;
        public int NumIllegalMoves;

        public int TotalGames => NumWins + NumLosses + NumDraws;

        public BotStats(string name)
        {
            BotName = name;
        }

        public void UpdateStats(GameResult result, bool isWhiteStats)
        {
            if (Arbiter.IsWhiteWinsResult(result) && isWhiteStats) NumWins++;
            else if (Arbiter.IsBlackWinsResult(result) && !isWhiteStats) NumWins++;
            else if (Arbiter.IsDrawResult(result)) NumDraws++;
            else NumLosses++;

            NumTimeouts += result is GameResult.WhiteTimeout or GameResult.BlackTimeout ? 1 : 0;
            NumIllegalMoves += result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove ? 1 : 0;        }

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
            char barChar = '█';
            Console.ForegroundColor = ConsoleColor.Green;
            for (int i = 0; i < winPct * barSize; i++) Console.Write(barChar);

            Console.ForegroundColor = ConsoleColor.Gray;
            for (int i = 0; i < drawPct * barSize; i++) Console.Write(barChar);

            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < lossPct * barSize; i++) Console.Write(barChar);

            Console.WriteLine();

            Console.ForegroundColor = NumTimeouts > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
            Console.WriteLine($"Timeouts: {NumTimeouts}");
            Console.ForegroundColor = NumTimeouts > 0 ? ConsoleColor.Red : ConsoleColor.Gray;
            Console.WriteLine($"Illegal Moves: {NumIllegalMoves}");
            Console.ResetColor();
        }
    }
}