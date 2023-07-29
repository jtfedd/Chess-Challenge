using ChessChallenge.Application;
using ChessChallenge.Chess;
using System;

namespace ChessChallenge.BotMatch
{
    public class GameController
    {
        GameParams gameParams;

        ChessPlayer PlayerWhite;
        ChessPlayer PlayerBlack;
        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;

        Board board;
        GameResult result;

        public GameResult getResult() => result;
        public Board getBoard() => board;

        public GameController(GameParams gameParams)
        {
            this.gameParams = gameParams;
            initPlayers();

            board = new Board();
            board.LoadPosition(gameParams.fen);
        }

        void initPlayers()
        {
            BotFactory botFactory = new BotFactory(gameParams.playerTimeMS);
            PlayerWhite = botFactory.CreatePlayer(gameParams.whitePlayer.type);
            PlayerBlack = botFactory.CreatePlayer(gameParams.blackPlayer.type);
        }

        public void Play()
        {
            return;
        }
    }
}