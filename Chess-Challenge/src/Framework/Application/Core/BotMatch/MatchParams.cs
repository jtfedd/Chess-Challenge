namespace ChessChallenge.BotMatch
{
    public class MatchParams
    {
        public BotType PlayerAType;
        public BotType PlayerBType;

        public int numGames;

        public int PlayerTimeMS;

        public string[] fens;

        public int numThreads;

        public MatchParams(
            BotType playerAType,
            BotType playerBType,
            int numGames,
            string[] fens,
            int playerTimeMS,
            int numThreads
        ) {
            PlayerAType = playerAType;
            PlayerBType = playerBType;
            this.numGames = numGames;
            PlayerTimeMS = playerTimeMS;
            this.fens = fens;
            this.numThreads = numThreads;
        }
    }
}
