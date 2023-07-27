namespace ChessChallenge.BotMatch
{
    public class GameParams
    {
        public int id;
        public string fen;

        public Bot whitePlayer;
        public Bot blackPlayer;

        public int playerTimeMS;

        public GameParams(int id, string fen, Bot whitePlayer, Bot blackPlayer, int playerTimeMS)
        {
            this.id = id;
            this.fen = fen;
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            this.playerTimeMS = playerTimeMS;
        }
    }
}