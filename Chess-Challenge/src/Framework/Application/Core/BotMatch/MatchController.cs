﻿using ChessChallenge.Chess;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        AutoResetEvent matchCompleteHandle;

        public MatchController(MatchParams matchParams)
        {
            Console.WriteLine($"Launching Bot Match {matchParams.PlayerAType} vs {matchParams.PlayerBType}");

            this.matchParams = matchParams;
            games = GenerateGames();

            gameMutex = new Mutex(false);
            gameIndex = 0;
            gamesComplete = 0;
            pgns = new();

            matchCompleteHandle = new AutoResetEvent(false);
        }

        public void Run()
        {
            for (int i = 0; i < matchParams.numThreads; i++)
            {
                StartNextGame();
            }

            matchCompleteHandle.WaitOne();
        }

        public void StartNextGame()
        {
            gameMutex.WaitOne();

            int gameID = gameIndex++;

            if (gameID >= games.Length)
            {
                gameMutex.ReleaseMutex();
                return;
            }

            GameParams game = games[gameID];

            gameMutex.ReleaseMutex();

            // Launch game thread
            Task.Factory.StartNew(() => GameThread(game), TaskCreationOptions.LongRunning);
        }

        void GameThread(GameParams game)
        {
            GameController gameController = new GameController(game);
            gameController.Play();

            Board finalBoard = gameController.getBoard();
            GameResult finalResult = gameController.getResult();

            OnGameComplete(game, finalBoard, finalResult);
        }

        public void OnGameComplete(GameParams game, Board board, GameResult result)
        {
            bool isMatchComplete = false;

            gameMutex.WaitOne();

            gamesComplete++;
            Console.WriteLine($"{game.id} Finished: " + result);

            game.whitePlayer.stats.UpdateStats(result, true);
            game.blackPlayer.stats.UpdateStats(result, false);

            string pgn = PGNCreator.CreatePGN(board, result, Util.GetPlayerName(game.whitePlayer.type), Util.GetPlayerName(game.blackPlayer.type));
            pgns.AppendLine(pgn);

            isMatchComplete = matchComplete;

            gameMutex.ReleaseMutex();

            if (isMatchComplete)
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
            Console.WriteLine($"Finished Bot Match {matchParams.PlayerAType} vs {matchParams.PlayerBType}");
            Console.WriteLine(pgns.ToString());
            playerA.stats.Print();
            //playerB.stats.Print();
            matchCompleteHandle.Set();
        }

        GameParams[] GenerateGames()
        {
            GameParams[] games = new GameParams[matchParams.numGames];

            playerA = new Bot(matchParams.PlayerAType, new BotStats(matchParams.PlayerAType.ToString()));
            playerB = new Bot(matchParams.PlayerBType, new BotStats(matchParams.PlayerBType.ToString()));

            for (int i = 0; i < matchParams.numGames; i++)
            {
                int fenIndex = i / 2;
                string fen = matchParams.fens[fenIndex % matchParams.fens.Length];

                games[i] = new GameParams(
                    i+1,
                    fen,
                    i % 2 == 0 ? playerA : playerB,
                    i % 2 == 0 ? playerB : playerA,
                    matchParams.PlayerTimeMS
                );
            }

            return games;
        }
    }
}