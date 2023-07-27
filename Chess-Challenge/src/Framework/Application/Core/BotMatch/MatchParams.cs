namespace ChessChallenge.BotMatch
{
    public class MatchParams
    {
        public BotType PlayerAType;
        public BotType PlayerBType;

        public int PlayerTimeMS;

        public string[] fens;

        public int numThreads;

        public MatchParams(
            BotType playerAType,
            BotType playerBType,
            string[] fens,
            int playerTimeMS,
            int numThreads
        ) {
            PlayerAType = playerAType;
            PlayerBType = playerBType;
            PlayerTimeMS = playerTimeMS;
            this.fens = fens;
            this.numThreads = numThreads;
        }
    }
}
