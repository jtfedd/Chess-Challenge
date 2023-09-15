using Chess_Challenge.src.My_Bot;
using MySql.Data.MySqlClient;
using ChessChallenge.API;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChessChallenge.Tuning
{
    public class Tuning
    {
        public static void Run() {
            var t = new Tuning();
            var bestParams = new TuningParams();
            Console.WriteLine(bestParams.print());
            //bestParams = t.tune(bestParams, 2);
            bestParams = t.tune(bestParams, 1);
            Console.WriteLine(bestParams.print());
        }

        PositionEval[] positionEvals;

        public Tuning()
        {
            positionEvals = fetchPositions();
        }

        public TuningParams tune(TuningParams startingParams, int step)
        {
            var bestParams = startingParams;
            double best = calcError(bestParams);
            bool improved = true;

            Console.WriteLine(best);
            Console.WriteLine(bestParams.print());

            int nParams = bestParams.values.Length;
            int[] directions = new int[nParams];
            Array.Fill(directions, 1);

            while(improved)
            {
                improved = false;

                for (int pi = 0; pi < nParams; pi++)
                {
                    if (TuningParams.skip(pi)) continue;

                    for (int j = 0; j < 2; j++) {
                        var delta = step * directions[pi];
                        if (bestParams.values[pi] + delta > bestParams.max(pi) || bestParams.values[pi] + delta < bestParams.min(pi)) {
                            directions[pi] *= -1;
                            continue;
                        }

                        var testParams = new TuningParams(bestParams);
                        testParams.values[pi] += delta;
                        double newE = calcError(testParams);
                        Console.WriteLine($"{pi}, {bestParams.values[pi]}, {delta}, {newE}, best: {best}");
                        if (newE < best)
                        {
                            best = newE;
                            bestParams = testParams;
                            improved = true;
                            Console.WriteLine(best);
                            Console.WriteLine(bestParams.print());
                            break;
                        } else
                        {
                            directions[pi] *= -1;
                            continue;
                        }
                    }
                }
            }

            return bestParams;
        }

        Mutex sumMutex;
        double sumOfDiffs;

        double calcError(TuningParams p)
        {
            sumOfDiffs = 0;
            sumMutex = new Mutex();
            var testbot = new TuningBot(p);
            int positions = 100000;
            int threads = 10;

            Task[] tasks = new Task[threads];

            for (int i = 0; i < threads; i++)
            {
                int captured = i;
                //Console.WriteLine($"Start thread {captured}");
                tasks[i] = Task.Factory.StartNew(() => calcRunner(p, captured, threads, positions), TaskCreationOptions.LongRunning);
            }

            for (int i = 0; i < threads; i++)
            {
                tasks[i].Wait();
            }

            return sumOfDiffs / positions;
        }

        void calcRunner(TuningParams p, int index, int threads, int positions)
        {
            var testbot = new TuningBot(p);
            //Console.WriteLine($"Running thread {index}");

            for (int i = index; i < positions; i += threads)
            {
                //Console.WriteLine($"Thread {index} position {i}");
                int eval = testbot.tuneEval(Board.CreateBoardFromFEN(positionEvals[i].fen));
                int diff = positionEvals[i].eval - eval;

                sumMutex.WaitOne();
                sumOfDiffs += diff * diff;
                sumMutex.ReleaseMutex();
            }
        }

        PositionEval[] fetchPositions() {
            Console.WriteLine("Reading positions...");

            var p = new PositionEval[11000000];

            string connstring = "Server=localhost; database=chess; UID=localprogram; password=localprogram";
            var conn = new MySqlConnection(connstring);
            conn.Open();

            string query = "SELECT fen,eval FROM evals WHERE ambiguous=0 AND eval > -1000 AND eval < 1000";
            var cmd = new MySqlCommand(query, conn);
            var reader = cmd.ExecuteReader();
            int i = 0;
            while (reader.Read())
            {
                p[i].fen = reader.GetString(0);
                p[i].eval = reader.GetInt32(1);
                i++;
            }
            conn.Close();

            var p2 = new PositionEval[i];
            Array.Copy(p, p2, i);

            Console.WriteLine("Done");
            Console.WriteLine($"{i} positions fetched");

            return p2;
        }

        struct PositionEval
        {
            public string fen;
            public int eval;

            public PositionEval(string fen, int eval)
            {
                this.fen = fen;
                this.eval = eval;
            }
        }
    }
}
