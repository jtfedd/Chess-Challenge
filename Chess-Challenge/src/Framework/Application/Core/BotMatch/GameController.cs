using ChessChallenge.Application;
using ChessChallenge.Chess;
using System;

namespace ChessChallenge.BotMatch
{
    public class GameController
    {
        GameParams gameParams;
        MoveGenerator moveGenerator;

        ChessPlayer PlayerWhite;
        ChessPlayer PlayerBlack;
        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;

        Board board;
        GameResult result;

        System.Timers.Timer tickTimer;

        public GameResult getResult() => result;
        public Board getBoard() => board;

        double getTime()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public GameController(GameParams gameParams)
        {
            moveGenerator = new();

            this.gameParams = gameParams;
            initPlayers();

            board = new Board();
            board.LoadPosition(gameParams.fen);

            result = GameResult.NotStarted;
        }

        void initPlayers()
        {
            BotFactory botFactory = new BotFactory(gameParams.playerTimeMS);
            PlayerWhite = botFactory.CreatePlayer(gameParams.whitePlayer.type);
            PlayerBlack = botFactory.CreatePlayer(gameParams.blackPlayer.type);
        }

        public void Play()
        {
            result = GameResult.InProgress;

            while(result == GameResult.InProgress)
            {
                double startTime = getTime();

                Move move;
                try
                {
                    move = GetBotMove();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred while bot was thinking.\n" + e.ToString());
                    Console.ResetColor();
                    result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                    throw;
                }

                if (!IsLegal(move))
                {
                    string moveName = MoveUtility.GetMoveNameUCI(move);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}");
                    Console.ResetColor();
                    result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
                    return;
                }

                double moveTime = startTime - getTime();
                PlayerToMove.UpdateClock(moveTime / 1000.0);
                if (PlayerToMove.TimeRemainingMs <= 0)
                {
                    result = PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout;
                }

                board.MakeMove(move, false);
                result = Arbiter.GetGameState(board);
            }
        }

        Move GetBotMove()
        {
            API.Board botBoard = new(new(board));
            API.Timer timer = new(PlayerToMove.TimeRemainingMs);
            API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
            return new Move(move.RawValue);
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