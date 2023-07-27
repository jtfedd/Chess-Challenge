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

            PlayerWhite = CreatePlayer(gameParams.whitePlayer.type);
        }

        ChessPlayer CreatePlayer(BotType type)
        {
            return type switch
            {
                BotType.MyBot => MakeBot(new MyBot()),
                BotType.EvilBot => MakeBot(new EvilBot()),
            };
        }

        ChessPlayer MakeBot(api::IChessBot bot)
        {
            return new ChessPlayer(bot, ChallengeController.PlayerType.MyBot, gameParams.PlayerTimeMS);
        }
    }
}