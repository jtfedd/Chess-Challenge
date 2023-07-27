using ChessChallenge.Application;
using ChessChallenge.Example;
using api = ChessChallenge.API;

namespace ChessChallenge.BotMatch
{
    public enum BotType
    {
        MyBot,
        EvilBot,
    }

    public class BotFactory
    {
        public ChessPlayer CreatePlayer(BotType type)
        {
            return type switch
            {
                BotType.MyBot => MakeBot(new MyBot()),
                BotType.EvilBot => MakeBot(new EvilBot()),
            };
        }

        int timeMS;

        public BotFactory(int timeMS)
        {
            this.timeMS = timeMS;
        }

        ChessPlayer MakeBot(api::IChessBot bot)
        {
            return new ChessPlayer(bot, ChallengeController.PlayerType.MyBot, timeMS);
        }
    }
}