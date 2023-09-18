using Chess_Challenge.src.My_Bot;
using MySql.Data.MySqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.Chess;

namespace ChessChallenge.Tuning
{
    public class Tuning
    {
        public static void Run() {
            var t = new Tuning();
            var bestParams = new TuningParams();

            bestParams = t.tune(bestParams, 2);
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
            bool improved;

            Console.WriteLine(best);
            Console.WriteLine(bestParams.print());

            int nParams = bestParams.values.Length;
            int[] directions = new int[nParams];
            Array.Fill(directions, 1);

            bool[] improvements = new bool[nParams];
            Array.Fill(improvements, false);

            bool isTopLevel = true;
            int innerIterations = 0;

            while(true)
            {
                improved = false;

                for (int pi = 0; pi < nParams; pi++)
                {
                    if (TuningParams.skip(pi)) continue;
                    if (!isTopLevel && !improvements[pi]) continue;

                    for (int j = 0; j < 2; j++) {
                        var delta = step * directions[pi];

                        Console.WriteLine($"Tuning {pi}, {bestParams.values[pi]}, {best}");
                        var (newE, improvedParam) = improve(bestParams, pi, delta, best);
                        improvements[pi] = improvedParam;
                        if (improvedParam)
                        {
                            Console.WriteLine("improved");
                            best = newE;
                            improved = true;

                            break;
                        }

                        directions[pi] *= -1;
                    }
                }

                Console.WriteLine("Iteration finished");

                if (isTopLevel)
                {
                    Console.WriteLine("Top Level");
                    if (!improved) break;
                    Console.WriteLine("Improved");
                    isTopLevel = false;
                } else
                {
                    Console.WriteLine("Inner level");
                    // Don't go too far down a rabbit trail just improving some specific parameters
                    if (!improved || innerIterations > 7)
                    {
                        Console.WriteLine("Not Improved");
                        isTopLevel = true;
                        innerIterations = 0;
                    } else
                    {
                        innerIterations++;
                        Console.WriteLine("Improved");
                    }
                }
            }

            return bestParams;
        }

        Tuple<double, bool> improve(TuningParams p, int pi, int step, double best)
        {
            bool improved = true;
            bool improvedAny = false;

            while(improved)
            {
                improved = false;

                p.values[pi] += step;
                if (p.values[pi] > TuningParams.max(pi) || p.values[pi] < TuningParams.min(pi)) break;

                double newE = calcError(p);
                Console.WriteLine($"{pi}, {p.values[pi]}, {step}, {newE}, best: {best}");
                if (newE < best)
                {
                    best = newE;
                    improved = true;
                    improvedAny = true;
                    Console.WriteLine(best);
                    Console.WriteLine(p.print());
                }
            }

            p.values[pi] -= step;

            return new(best, improvedAny);
        }

        Mutex sumMutex;
        double sumOfDiffs;


        Mutex counterMutex;
        int counter;

        double calcError(TuningParams p)
        {
            sumOfDiffs = 0;
            sumMutex = new Mutex();
            counter = 0;
            counterMutex = new Mutex();
            var testbot = new TuningBot(p);
            int threads = 10;

            Task[] tasks = new Task[threads];

            for (int i = 0; i < threads; i++)
            {
                //Console.WriteLine($"Start thread {captured}");
                tasks[i] = Task.Factory.StartNew(() => calcRunner(p, positions), TaskCreationOptions.LongRunning);
            }

            for (int i = 0; i < threads; i++)
            {
                tasks[i].Wait();
            }

            return sumOfDiffs / positions;
        }

        void calcRunner(TuningParams p, int positions)
        {
            var testbot = new TuningBot(p);
            //Console.WriteLine($"Running thread {index}");

            while(true)
            {
                counterMutex.WaitOne();
                int i = counter++;
                counterMutex.ReleaseMutex();

                if (i >= positions) break;

                //Console.WriteLine($"Thread {index} position {i}");
                int eval = testbot.tuneEval(API.Board.CreateBoardFromPosition(positionEvals[i].pos));
                int diff = positionEvals[i].eval - eval;

                sumMutex.WaitOne();
                sumOfDiffs += diff * diff;
                sumMutex.ReleaseMutex();
            }
        }

        int positions = 100000;

        PositionEval[] fetchPositions() {
            Console.WriteLine("Reading positions...");

            var p = new PositionEval[positions];

            string connstring = "Server=localhost; database=chess; UID=localprogram; password=localprogram";
            var conn = new MySqlConnection(connstring);
            conn.Open();

            string query = $"SELECT id,fen,eval FROM evals WHERE ambiguous=0 AND eval > -1000 AND eval < 1000 ORDER BY id ASC LIMIT {positions}";
            var cmd = new MySqlCommand(query, conn);
            var reader = cmd.ExecuteReader();
            int i = 0;
            while (reader.Read())
            {
                p[i].pos = FenUtility.PositionFromFen(reader.GetString(1));
                p[i].eval = reader.GetInt32(2);
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
            public FenUtility.PositionInfo pos;
            public int eval;

            public PositionEval(FenUtility.PositionInfo pos, int eval)
            {
                this.pos = pos;
                this.eval = eval;
            }
        }
    }
}
