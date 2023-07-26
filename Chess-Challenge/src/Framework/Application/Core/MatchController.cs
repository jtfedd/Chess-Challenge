using ChessChallenge.Chess;
using ChessChallenge.Example;
using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application
{
    public class MatchController
    {
        ChallengeController.PlayerType PlayerAType = ChallengeController.PlayerType.MyBot;
        ChallengeController.PlayerType PlayerBType = ChallengeController.PlayerType.EvilBot;

        // Game state
        Random rng;
        int gameID;
        bool isPlaying;
        Board board;
        public ChessPlayer PlayerWhite { get; private set; }
        public ChessPlayer PlayerBlack { get; private set; }

        double lastMoveMadeTime;
        bool isWaitingToPlayMove;
        double lastUpdateTime;
        Move moveToPlay;
        double playMoveTime;
        public bool HumanWasWhiteLastGame { get; private set; }

        // Bot match state
        readonly string[] botMatchStartFens;
        int botMatchGameIndex;
        public BotMatchStats BotStatsA { get; private set; }
        public BotMatchStats BotStatsB { get; private set; }
        bool botAPlaysWhite;


        // Bot task
        AutoResetEvent botTaskWaitHandle;
        bool hasBotTaskException;
        ExceptionDispatchInfo botExInfo;

        // Other
        readonly MoveGenerator moveGenerator;
        readonly StringBuilder pgns;

        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
        public int TotalGameCount => botMatchStartFens.Length * 2;
        public int CurrGameNumber => Math.Min(TotalGameCount, botMatchGameIndex + 1);
        public string AllPGNs => pgns.ToString();

        public bool MatchInProgress;

        double getTime()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public MatchController()
        {
            Log($"Launching Bot Match version {Settings.Version}");
            Warmer.Warm();

            rng = new Random();
            moveGenerator = new();
            board = new Board();
            pgns = new();

            BotStatsA = new BotMatchStats(GetPlayerName(PlayerAType));
            BotStatsB = new BotMatchStats(GetPlayerName(PlayerBType));
            botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();
            botTaskWaitHandle = new AutoResetEvent(false);

            MatchInProgress = true;

            StartNewGame(PlayerAType, PlayerBType);
        }

        public void StartNewGame(ChallengeController.PlayerType whiteType, ChallengeController.PlayerType blackType)
        {
            // End any ongoing game
            EndGame(GameResult.DrawByArbiter, autoStartNextBotMatch: false);
            gameID = rng.Next();

            lastMoveMadeTime = getTime();
            lastUpdateTime = getTime();

            // Stop prev task and create a new one
            // Allow task to terminate
            botTaskWaitHandle.Set();
            // Create new task
            botTaskWaitHandle = new AutoResetEvent(false);
            Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);

            // Board Setup
            board = new Board();
            int fenIndex = botMatchGameIndex / 2;
            board.LoadPosition(botMatchStartFens[fenIndex]);

            // Player Setup
            PlayerWhite = CreatePlayer(whiteType);
            PlayerBlack = CreatePlayer(blackType);

            // Start
            isPlaying = true;
            NotifyTurnToMove();
        }

        ChessPlayer CreatePlayer(ChallengeController.PlayerType type)
        {
            return type switch
            {
                ChallengeController.PlayerType.MyBot => new ChessPlayer(new MyBot(), type, GameDurationMilliseconds),
                ChallengeController.PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
            };
        }

        void NotifyTurnToMove()
        {
            botTaskWaitHandle.Set();
        }

        void BotThinkerThread()
        {
            int threadID = gameID;
            //Console.WriteLine("Starting thread: " + threadID);

            while (true)
            {
                // Sleep thread until notified
                botTaskWaitHandle.WaitOne();
                // Get bot move
                if (threadID == gameID)
                {
                    var move = GetBotMove();

                    if (threadID == gameID)
                    {
                        OnMoveChosen(move);
                    }
                }
                // Terminate if no longer playing this game
                if (threadID != gameID)
                {
                    break;
                }
            }
            //Console.WriteLine("Exitting thread: " + threadID);
        }

        Move GetBotMove()
        {
            API.Board botBoard = new(new(board));
            try
            {
                API.Timer timer = new(PlayerToMove.TimeRemainingMs);
                API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
                return new Move(move.RawValue);
            }
            catch (Exception e)
            {
                Log("An error occurred while bot was thinking.\n" + e.ToString(), true, ConsoleColor.Red);
                hasBotTaskException = true;
                botExInfo = ExceptionDispatchInfo.Capture(e);
            }
            return Move.NullMove;
        }

        void OnMoveChosen(Move chosenMove)
        {
            if (IsLegal(chosenMove))
            {
                moveToPlay = chosenMove;
                isWaitingToPlayMove = true;
            }
            else
            {
                string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
                string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
                Log(log, true, ConsoleColor.Red);
                GameResult result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                EndGame(result);
            }
        }

        void PlayMove(Move move)
        {
            if (!isPlaying) return;

            lastMoveMadeTime = getTime();

            board.MakeMove(move, false);

            GameResult result = Arbiter.GetGameState(board);
            if (result == GameResult.InProgress)
            {
                NotifyTurnToMove();
            }
            else
            {
                EndGame(result);
            }
        }

        void EndGame(GameResult result, bool autoStartNextBotMatch = true)
        {
            if (!isPlaying) return;

            isPlaying = false;
            isWaitingToPlayMove = false;
            gameID = -1;

            string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite), GetPlayerName(PlayerBlack));
            pgns.AppendLine(pgn);

            // If 2 bots playing each other, start next game automatically.
            BotStatsA.UpdateStats(result, botAPlaysWhite);
            BotStatsB.UpdateStats(result, !botAPlaysWhite);
            botMatchGameIndex++;

            Log($"({botMatchGameIndex}/{TotalGameCount}) [+{BotStatsA.NumWins} ={BotStatsA.NumDraws} -{BotStatsA.NumLosses}] Game Over: " + result, false, ConsoleColor.Blue);

            if (botMatchGameIndex < TotalGameCount && autoStartNextBotMatch)
            {
                botAPlaysWhite = !botAPlaysWhite;
                int originalGameID = gameID;
                AutoStartNextBotMatchGame(originalGameID);
            }
            else if (autoStartNextBotMatch)
            {
                Log("Match finished", false, ConsoleColor.Blue);
                BotStatsA.Print();
                BotStatsB.Print();
                MatchInProgress = false;
            }
        }

        private void AutoStartNextBotMatchGame(int originalGameID)
        {
            if (originalGameID == gameID)
            {
                StartNewGame(PlayerBlack.PlayerType, PlayerWhite.PlayerType);
            }
        }

        public void Update()
        {
            if (isPlaying)
            {
                double time = getTime();
                PlayerToMove.UpdateClock((time - lastUpdateTime) / 1000.0);
                lastUpdateTime = time;

                if (PlayerToMove.TimeRemainingMs <= 0)
                {
                    EndGame(PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout);
                }
                else
                {
                    if (isWaitingToPlayMove)
                    {
                        isWaitingToPlayMove = false;
                        PlayMove(moveToPlay);
                    }
                }
            }

            if (hasBotTaskException)
            {
                hasBotTaskException = false;
                botExInfo.Throw();
            }
        }

        static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
        static string GetPlayerName(ChallengeController.PlayerType type) => type.ToString();

        public void StartNewBotMatch(ChallengeController.PlayerType botTypeA, ChallengeController.PlayerType botTypeB)
        {
            EndGame(GameResult.DrawByArbiter, autoStartNextBotMatch: false);
            botMatchGameIndex = 0;
            string nameA = GetPlayerName(botTypeA);
            string nameB = GetPlayerName(botTypeB);
            if (nameA == nameB)
            {
                nameA += " (A)";
                nameB += " (B)";
            }
            BotStatsA = new BotMatchStats(nameA);
            BotStatsB = new BotMatchStats(nameB);
            botAPlaysWhite = true;
            Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);
            StartNewGame(botTypeA, botTypeB);
        }

        bool IsLegal(Move givenMove)
        {
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public class BotMatchStats
        {
            public string BotName;
            public int NumWins;
            public int NumLosses;
            public int NumDraws;
            public int NumTimeouts;
            public int NumIllegalMoves;

            public BotMatchStats(string name) => BotName = name;

            public void Print()
            {
                Log("");
                Log(BotName, false, ConsoleColor.Blue);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"+{NumWins}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" ={NumDraws} ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"-{NumLosses}");
                Console.WriteLine();
                Console.ResetColor();
                Log($"Timeouts: {NumTimeouts}", false, ConsoleColor.Blue);
                Log($"Illegal Moves: {NumIllegalMoves}", false, ConsoleColor.Blue);
            }

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
                    NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
                    NumIllegalMoves += (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
                }
            }
        }
    }
}