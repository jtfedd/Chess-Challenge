namespace ChessChallenge.BotMatch
{
    public class MatchController
    {
        MatchParams matchParams;
        GameParams[] games;

        public MatchController(MatchParams matchParams)
        {
            this.matchParams = matchParams;
            games = GenerateGames();
        }

        public void Run()
        {
            
        }

        GameParams[] GenerateGames()
        {
            GameParams[] games = new GameParams[matchParams.fens.Length * 2];

            Bot botA = new Bot(matchParams.PlayerAType, new BotStats(matchParams.PlayerAType.ToString()));
            Bot botB = new Bot(matchParams.PlayerBType, new BotStats(matchParams.PlayerBType.ToString()));

            int gameIndex = 0;
            for (int fenIndex = 0; fenIndex < matchParams.fens.Length; fenIndex++)
            {
                string fen = matchParams.fens[fenIndex];

                games[gameIndex] = new GameParams(
                    gameIndex,
                    fen,
                    botA,
                    botB
                );

                gameIndex++;

                games[gameIndex] = new GameParams(
                    gameIndex,
                    fen,
                    botB,
                    botA
                );

                gameIndex++;
            }

            return games;
        }
    }
}