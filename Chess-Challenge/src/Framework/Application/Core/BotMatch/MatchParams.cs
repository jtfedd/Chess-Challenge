namespace ChessChallenge.BotMatch
{
    public class MatchParams
    {
        public PlayerType PlayerAType;
        public PlayerType PlayerBType;

        public int PlayerTimeMS;

        public MatchParams(PlayerType playerAType, PlayerType playerBType, int playerTimeMS)
        {
            PlayerAType = playerAType;
            PlayerBType = playerBType;
            PlayerTimeMS = playerTimeMS;
        }
    }
}
