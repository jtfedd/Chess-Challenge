using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessChallenge.Benchmarks
{
    public static class Benchmarks
    {
        public static void Run()
        {
            string[] fens = FileHelper.ReadResourceFile("AllFens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();

            Parallel.Invoke(
                () => benchmark("Search", fens, 10, benchmarkSearch)
            );
        }

        static double benchmarkSearch(string fen)
        {
            Board board = Board.CreateBoardFromFEN(fen);
            MyBot myBot = new MyBot();
            var start = DateTime.Now;
            myBot.benchmarkSearch(board, 2);
            return (DateTime.Now - start).TotalSeconds;
        }

        static void benchmark(string name, string[] fens, int count, Func<string, double> f)
        {
            List<double> durations = new List<double>();
            for (int i = 0; i < count; i++)
            {
                foreach (string fen in fens)
                {
                    var duration = f(fen);
                    durations.Add(duration);
                }
            }
            Console.WriteLine($"{name} - Total: {durations.Sum()} Average: {durations.Average()}");
        }
    }
}