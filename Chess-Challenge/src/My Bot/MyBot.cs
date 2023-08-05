using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// TODO
// Transposition table
// Move ordering
// Quiescence search (capture pruning)
// Principal variation search
// Frontier pruning?

// Improve evaluation

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    Board board;
    Timer timer;
    const int tt_size = 1048583;
    TT_Entry[] tt;

    // Debug variables
    int cacheHits;
    int nodesSearched;
    int quiesenceNodes;
    int evaluations;
    int cutoffs;

    int msToThink;
    bool cancelled;

    Move searchBestMove;

    public MyBot()
    {
        tt = new TT_Entry[tt_size];
    }

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.MillisecondsRemaining / 40;
        //msToThink = 500;

        cancelled = false;

        this.board = board;
        this.timer = timer;

        // Always evaluate at depth 2
        int depth = 2;
        search(depth, -int.MaxValue, int.MaxValue, true);
        Move bestMove = searchBestMove;
        Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}");

        while (!cancelled && depth < 5)
        {
            cacheHits = 0;
            nodesSearched = 0;
            evaluations = 0;
            cutoffs = 0;
            quiesenceNodes = 0;
            search(++depth, -int.MaxValue, int.MaxValue, true);
            if (!cancelled) bestMove = searchBestMove;
            Console.WriteLine($"{(cancelled ? "Cancelled":"")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}");
        }
        return bestMove;
    }

    int search(int depth, int alpha, int beta, bool isTopLevel)
    {
        cancelled = timer.MillisecondsElapsedThisTurn > msToThink;
        if (cancelled) return 0;

        nodesSearched++;

        if (board.IsInCheckmate()) return -int.MaxValue;
        else if (board.IsDraw()) return 0;
        else if (depth == 0) return quiesce(alpha, beta);

        int best_score = int.MinValue;

        foreach (Move move in board.GetLegalMoves().OrderByDescending(moveOrder))
        {
            board.MakeMove(move);

            int move_score = -search(depth - 1, -beta, -alpha, false);

            board.UndoMove(move);

            if (move_score > best_score) {
                best_score = move_score;
                if (isTopLevel) searchBestMove = move;
            }
            if (best_score > alpha) alpha = best_score;
            if (alpha >= beta) {
                cutoffs++;
                return alpha;
            }
        }

        return best_score;
    }

    int quiesce(int alpha, int beta)
    {
        cancelled = timer.MillisecondsElapsedThisTurn > msToThink;
        if (cancelled) return 0;

        quiesenceNodes++;

        if (board.IsInCheckmate()) return -int.MaxValue;
        else if (board.IsDraw()) return 0;

        int stand_pat = evaluate() * (board.IsWhiteToMove ? 1 : -1);
        if (stand_pat >= beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        Move[] moves = board.GetLegalMoves(true).OrderByDescending(moveOrder).ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int move_score = -quiesce(-beta, -alpha);

            board.UndoMove(move);

            if (move_score >= beta) return beta;
            if (move_score > alpha) alpha = move_score;
        }

        return alpha;
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

    int moveOrder(Move move)
    {
        int moveScore = 0;
        if (move.CapturePieceType != PieceType.None) moveScore = pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
        return moveScore;
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