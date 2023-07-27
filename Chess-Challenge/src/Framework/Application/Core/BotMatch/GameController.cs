using ChessChallenge.Application;
using ChessChallenge.Example;
using api = ChessChallenge.API;


namespace ChessChallenge.BotMatch
{
    public class GameController
    {
        GameParams gameParams;

        ChessPlayer PlayerWhite;
        ChessPlayer PlayerBlack;

        public GameController(GameParams gameParams)
        {
            this.gameParams = gameParams;
            initPlayers();
        }

        void initPlayers()
        {
            BotFactory botFactory = new BotFactory(gameParams.playerTimeMS);
            PlayerWhite = botFactory.CreatePlayer(gameParams.whitePlayer.type);
            PlayerBlack = botFactory.CreatePlayer(gameParams.blackPlayer.type);
        }
    }
}