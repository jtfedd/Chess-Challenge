using System;
using System.Text;
using System.Threading;

namespace ChessChallenge.BotMatch
{
    public class MatchController
    {
        MatchParams matchParams;
        GameParams[] games;

        Bot playerA;
        Bot playerB;

        Mutex gameMutex;
        int gameIndex;
        int gamesComplete;
        bool matchComplete => gamesComplete == games.Length;
        StringBuilder pgns;

        public MatchController(MatchParams matchParams)
        {
            this.matchParams = matchParams;
            games = GenerateGames();

            gameMutex = new Mutex(false);
            gameIndex = 0;
            gamesComplete = 0;
            pgns = new();
        }

        public void Run()
        {
            for (int i = 0; i < matchParams.numThreads; i++)
            {
                StartNextGame();
            }
        }

        public void StartNextGame()
        {
            gameMutex.WaitOne();

            int gameID = gameIndex++;
            GameParams game = games[gameIndex];

            gameMutex.ReleaseMutex();

            // Launch game thread
        }

        public void OnGameComplete(string pgn)
        {
            bool isMatchComplete = false;

            gameMutex.WaitOne();

            gamesComplete++;
            pgns.AppendLine(pgn);
            isMatchComplete = matchComplete;

            gameMutex.ReleaseMutex();

            if (matchComplete)
            {
                OnMatchComplete();
            }
            else
            {
                StartNextGame();
            }
        }

        public void OnMatchComplete()
        {
            Console.WriteLine("Match Finished");
            playerA.stats.Print();
            playerB.stats.Print();
        }

        GameParams[] GenerateGames()
        {
            GameParams[] games = new GameParams[matchParams.fens.Length * 2];

            playerA = new Bot(matchParams.PlayerAType, new BotStats(matchParams.PlayerAType.ToString()));
            playerB = new Bot(matchParams.PlayerBType, new BotStats(matchParams.PlayerBType.ToString()));

            int gameIndex = 0;
            for (int fenIndex = 0; fenIndex < matchParams.fens.Length; fenIndex++)
            {
                string fen = matchParams.fens[fenIndex];

                games[gameIndex] = new GameParams(
                    gameIndex,
                    fen,
                    playerA,
                    playerB,
                    matchParams.PlayerTimeMS
                );

                gameIndex++;

                games[gameIndex] = new GameParams(
                    gameIndex,
                    fen,
                    playerB,
                    playerA,
                    matchParams.PlayerTimeMS
                );

                gameIndex++;
            }

            return games;
        }
    }
}