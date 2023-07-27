using ChessChallenge.Chess;
using ChessChallenge.Example;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using api = ChessChallenge.API;
using ChessChallenge.Application;


namespace ChessChallenge.BotMatch
{
    public class MatchRunner
    {
        MatchParams matchParams;

        // Game state
        Random rng;
        int gameID;
        bool isPlaying;
        Board board;
        public ChessPlayer PlayerWhite { get; private set; }
        public ChessPlayer PlayerBlack { get; private set; }

        bool isWaitingToPlayMove;
        double lastUpdateTime;
        Move moveToPlay;

        // Bot match state
        int botMatchGameIndex;
        public BotStats BotStatsA { get; private set; }
        public BotStats BotStatsB { get; private set; }
        bool botAPlaysWhite;


        // Bot task
        AutoResetEvent botTaskWaitHandle;
        bool hasBotTaskException;
        ExceptionDispatchInfo botExInfo;

        // Other
        readonly MoveGenerator moveGenerator;
        readonly StringBuilder pgns;

        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
        public int TotalGameCount => matchParams.fens.Length * 2;
        public int CurrGameNumber => Math.Min(TotalGameCount, botMatchGameIndex + 1);
        public string AllPGNs => pgns.ToString();

        public bool MatchInProgress;

        double getTime()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public MatchRunner(MatchParams matchParams)
        {
            Console.WriteLine($"Launching Bot Match version {Settings.Version}");
            Warmer.Warm();

            this.matchParams = matchParams;

            rng = new Random();
            moveGenerator = new();
            board = new Board();
            pgns = new();

            BotStatsA = new BotStats(Util.GetPlayerName(matchParams.PlayerAType));
            BotStatsB = new BotStats(Util.GetPlayerName(matchParams.PlayerBType));
            botTaskWaitHandle = new AutoResetEvent(false);
        }

        public void Run()
        {
            StartNewBotMatch();

            while(MatchInProgress)
            {
                Update();
            }
        }

        void StartNewGame(BotType whiteType, BotType blackType)
        {
            // End any ongoing game
            EndGame(GameResult.DrawByArbiter, autoStartNextBotMatch: false);
            gameID = rng.Next();

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
            board.LoadPosition(matchParams.fens[fenIndex]);

            // Player Setup
            BotFactory factory = new BotFactory(matchParams.PlayerTimeMS);
            PlayerWhite = factory.CreatePlayer(whiteType);
            PlayerBlack = factory.CreatePlayer(blackType);

            // Start
            isPlaying = true;
            NotifyTurnToMove();
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred while bot was thinking.\n" + e.ToString());
                Console.ResetColor();
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
                Console.ForegroundColor= ConsoleColor.Red;
                Console.WriteLine($"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}");
                Console.ResetColor();
                GameResult result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                EndGame(result);
            }
        }

        void PlayMove(Move move)
        {
            if (!isPlaying) return;

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

            string pgn = PGNCreator.CreatePGN(board, result, Util.GetPlayerName(PlayerWhite), Util.GetPlayerName(PlayerBlack));
            pgns.AppendLine(pgn);

            // If 2 bots playing each other, start next game automatically.
            BotStatsA.UpdateStats(result, botAPlaysWhite);
            BotStatsB.UpdateStats(result, !botAPlaysWhite);
            botMatchGameIndex++;

            Console.WriteLine($"({botMatchGameIndex}/{TotalGameCount}) [+{BotStatsA.NumWins} ={BotStatsA.NumDraws} -{BotStatsA.NumLosses}] Game Over: " + result);

            if (botMatchGameIndex < TotalGameCount && autoStartNextBotMatch)
            {
                botAPlaysWhite = !botAPlaysWhite;
                int originalGameID = gameID;
                AutoStartNextBotMatchGame(originalGameID);
            }
            else if (autoStartNextBotMatch)
            {
                Console.WriteLine("Match finished");
                BotStatsA.Print();
                BotStatsB.Print();
                MatchInProgress = false;
            }
        }

        private void AutoStartNextBotMatchGame(int originalGameID)
        {
            if (originalGameID == gameID)
            {
                StartNewGame(botAPlaysWhite ? matchParams.PlayerAType : matchParams.PlayerBType, botAPlaysWhite ? matchParams.PlayerBType : matchParams.PlayerAType);
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

        void StartNewBotMatch()
        {
            MatchInProgress = true;
            EndGame(GameResult.DrawByArbiter, autoStartNextBotMatch: false);
            botMatchGameIndex = 0;
            string nameA = Util.GetPlayerName(matchParams.PlayerAType);
            string nameB = Util.GetPlayerName(matchParams.PlayerBType);
            if (nameA == nameB)
            {
                nameA += " (A)";
                nameB += " (B)";
            }
            BotStatsA = new BotStats(nameA);
            BotStatsB = new BotStats(nameB);
            botAPlaysWhite = true;
            Console.WriteLine($"Starting new match: {nameA} vs {nameB}");
            StartNewGame(matchParams.PlayerAType, matchParams.PlayerBType);
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
    }
}