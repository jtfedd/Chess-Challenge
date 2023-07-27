using ChessChallenge.Application;
using ChessChallenge.Chess;


namespace ChessChallenge.BotMatch
{
    public class GameController
    {
        GameParams gameParams;

        ChessPlayer PlayerWhite;
        ChessPlayer PlayerBlack;
        ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;

        Board board;

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
    }
}