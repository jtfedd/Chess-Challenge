using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

// TODO
// Transposition table
// Iterative deepening
// Move ordering
// Principal variation search
// Quiescence search (capture pruning)
// Frontier pruning?

// Improve evaluation

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    Board board;
    Timer timer;
    TT_Entry[] tt = new TT_Entry[1048576];

    // Debug variables
    int cacheHits;
    int nodesSearched;
    int evaluations;

    int msToThink;
    bool cancelled;

    Move searchBestMove;

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.MillisecondsRemaining / 35;
        //msToThink = 500;

        cancelled = false;

        this.board = board;
        this.timer = timer;

        // Always evaluate at depth 2
        int depth = 2;
        search(board.IsWhiteToMove, depth, -int.MaxValue, int.MaxValue, true);
        Move bestMove = searchBestMove;

        while (!cancelled)
        {
            cacheHits = 0;
            nodesSearched = 0;
            evaluations = 0;
            search(board.IsWhiteToMove, ++depth, -int.MaxValue, int.MaxValue, true);
            if (!cancelled) bestMove = searchBestMove;
            Console.WriteLine($"{(cancelled ? "Cancelled":"")} {depth} Nodes searched: {nodesSearched} evaluations: {evaluations} cache hits: {cacheHits}");
        }
        return bestMove;
    }

    int search(bool isWhite, int depth, int alpha, int beta, bool isTopLevel)
    {
        cancelled = timer.MillisecondsElapsedThisTurn > msToThink;
        if (cancelled) return 0;

        nodesSearched++;

        if (board.IsInCheckmate()) return -int.MaxValue;
        else if (board.IsDraw()) return 0;
        else if (depth == 0) return evaluate() * (isWhite ? 1 : -1);

        int best_score = int.MinValue;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);

            int move_score = -search(!isWhite, depth - 1, -beta, -alpha, false);

            board.UndoMove(move);

            if (move_score > best_score) {
                best_score = move_score;
                if (isTopLevel) searchBestMove = move;
            }
            if (best_score > alpha) alpha = best_score;
            if (alpha >= beta) return alpha;
        }

        return best_score;
    }

    int evaluate()
    {
        evaluations++;
        int score = 0;

        foreach (PieceList pieces in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieces)
            {
                int value = pieceValues[(int)piece.PieceType];
                score += piece.IsWhite ? value : -value;
            }
        }

        return score;
    }

    class TT_Entry
    {
        public ulong key;
        public int depth;
        public int evaluation;

        public TT_Entry(ulong key, int depth, int evaluation)
        {
            this.key = key;
            this.depth = depth;
            this.evaluation = evaluation;
        }
    }
}