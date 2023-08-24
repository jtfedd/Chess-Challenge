using ChessChallenge.Application;
using ChessChallenge.Example;
using api = ChessChallenge.API;

namespace ChessChallenge.BotMatch
{
    public class MatchBots
    {
        public static BotType A = BotType.MyBot;
        public static BotType B = BotType.EvalV3;
    }

    public enum BotType
    {
        MyBot,
        Negamax,
        IterativeDeepening,
        Quiescence,
        PieceSquareEval,
        Transposition,
        EvalV1,
        EvalV2,
        EvalV3,

        EvilBot,
        ExampleBot,
    }

    public class BotFactory
    {
        public ChessPlayer CreatePlayer(BotType type)
        {
            return type switch
            {
                BotType.MyBot => MakeBot(new MyBot()),
                BotType.Negamax => MakeBot(new Negamax()),
                BotType.IterativeDeepening => MakeBot(new IterativeDeepening()),
                BotType.Quiescence => MakeBot(new Quiescence()),
                BotType.PieceSquareEval => MakeBot(new PieceSquareEval()),
                BotType.Transposition => MakeBot(new Transposition()),
                BotType.EvalV1 => MakeBot(new EvalV1()),
                BotType.EvalV2 => MakeBot(new EvalV2()),
                BotType.EvalV3 => MakeBot(new EvalV3()),

                BotType.EvilBot => MakeBot(new EvilBot()),
                BotType.ExampleBot => MakeBot(new ExampleBot()),
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