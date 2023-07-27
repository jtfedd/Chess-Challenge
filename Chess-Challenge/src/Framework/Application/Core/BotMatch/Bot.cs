namespace ChessChallenge.BotMatch
{
    public class Bot
    {
        public BotType type;
        public BotStats stats;

        public string name => type.ToString();

        public Bot(BotType type, BotStats stats)
        {
            this.type = type;
            this.stats = stats;
        }
    }
}