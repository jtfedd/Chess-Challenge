

using ChessChallenge.Application;
using ChessChallenge.Chess;
using System;
using System.IO;

namespace ChessChallenge.BotMatch
{
    public class BotStats
    {
        public string BotName;

        public int WhiteWins;
        public int BlackWins;
        public int TotalWins => WhiteWins + BlackWins;

        public int WhiteLosses;
        public int BlackLosses;
        public int TotalLosses => WhiteLosses + BlackLosses;

        public int WhiteDraws;
        public int BlackDraws;
        public int TotalDraws => WhiteDraws + BlackDraws;

        public int NumTimeouts;
        public int NumIllegalMoves;

        public int TotalGames => TotalWins + TotalLosses + TotalDraws;

        public BotStats(string name)
        {
            BotName = name;
        }

        public void UpdateStats(GameResult result, bool isWhiteStats)
        {
            if (isWhiteStats)
            {
                if (Arbiter.IsWhiteWinsResult(result)) WhiteWins++;
                else if (Arbiter.IsDrawResult(result)) WhiteDraws++;
                else WhiteLosses++;
            } else
            {
                if (Arbiter.IsBlackWinsResult(result)) BlackWins++;
                else if (Arbiter.IsDrawResult(result)) BlackDraws++;
                else BlackLosses++;
            }

            NumTimeouts += result is GameResult.WhiteTimeout or GameResult.BlackTimeout ? 1 : 0;
            NumIllegalMoves += result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove ? 1 : 0;        }

        public void Print()
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(BotName);

            Console.ForegroundColor = ConsoleColor.Gray;
            try
            {
                Console.WriteLine($"Token Count: {GetTokenCount()}");
            }
            catch
            {
                Console.WriteLine("Token Count: <unknown>");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("As White");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"+{WhiteWins}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" ={WhiteDraws} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"-{WhiteLosses}");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("As Black");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"+{BlackWins}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" ={BlackDraws} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"-{BlackLosses}");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Overall Stats");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"+{TotalWins}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" ={TotalDraws} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"-{TotalLosses}");
            Console.WriteLine();

            float winPct = (float)TotalWins / TotalGames;
            float drawPct = (float)TotalDraws / TotalGames;
            float lossPct = (float)TotalLosses / TotalGames;

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

        public (int, int) GetTokenCount()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", $"{BotName}.cs");

            using StreamReader reader = new(path);
            string txt = reader.ReadToEnd();
            return TokenCounter.CountTokens(txt);
        }
    }
}