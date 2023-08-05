﻿using ChessChallenge.Application;
using ChessChallenge.Example;
using api = ChessChallenge.API;

namespace ChessChallenge.BotMatch
{
    public class MatchBots
    {
        public static BotType A = BotType.MyBot;
        public static BotType B = BotType.Quiescence;
    }

    public enum BotType
    {
        MyBot,
        Negamax,
        IterativeDeepening,
        Quiescence,

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
                BotType.Quiescence => MakeBot(new QuiecenseBot()),

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