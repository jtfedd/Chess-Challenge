using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

// TODO
// - Search
//   - [x] Negamax
//   - [x] Iterative deepening
//   - [ ] Transposition table
//   - [ ] Transposition table move ordering
//   - [x] Quiescence search
//   - [ ] Principal variation search
// - Evaluation
//   - [x] Piece values
//   - [x] Piece-square tables
//   - [ ] Endgame piece-square tables
//   - [x] Attack bonus
//   - [ ] Mobility bonus
//   - [ ] Pawn structure bonus
//   - [ ] Defended/attacked pieces bonus

public class Quiescence : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 550, 900, 10000 };

    Board board;
    Timer timer;
    int tt_size = 1048583;
    TT_Entry[] tt;

    // Debug variables
    int cacheHits; // #DEBUG
    int nodesSearched; // #DEBUG
    int evaluations; // #DEBUG
    int cutoffs; // #DEBUG
    int quiesenceNodes;
    int msToThink;
    bool cancelled;

    Move searchBestMove;

    public Quiescence()
    {
        tt = new TT_Entry[tt_size];
    }

    int pushBonus(int col, int amount) => amount * (col) / 8;

    int centerBonus(double row, double col, int amount)
    {
        row -= 3.5;
        col -= 3.5;
        double dist = Math.Sqrt(row * row + col * col);
        return (int)(amount * (5 - dist) / 5);
    }

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.MillisecondsRemaining / 40;
        //msToThink = 500;

        cancelled = false;

        this.board = board;
        this.timer = timer;

        Console.WriteLine($"Current evaluation {evaluate()}");

        // Always evaluate at depth 2
        int depth = 2;
        search(depth, -int.MaxValue, int.MaxValue, true);
        Move bestMove = searchBestMove;
        Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}");

        while (!cancelled && depth < 5)
        {
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG
            search(++depth, -int.MaxValue, int.MaxValue, true);
            if (!cancelled) bestMove = searchBestMove;
            Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}"); // #DEBUG
        }

        return bestMove;
    }

    public void benchmarkSearch(Board board, int depth) // #DEBUG
    {// #DEBUG
        this.board = board;// #DEBUG
        this.timer = new Timer(int.MaxValue); //#DEBUG
        this.cancelled = false;// #DEBUG
        search(depth, -int.MaxValue, int.MaxValue, true);// #DEBUG
    }// #DEBUG

    int search(int depth, int alpha, int beta, bool isTopLevel)
    {
        cancelled = timer.MillisecondsElapsedThisTurn > msToThink;
        if (cancelled) return 0;

        nodesSearched++;

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates
        if (board.IsInCheckmate()) return -int.MaxValue + board.PlyCount;
        else if (board.IsDraw()) return 0;
        else if (depth == 0) return quiesce(alpha, beta);

        int best_score = int.MinValue;

        foreach (Move move in board.GetLegalMoves().OrderByDescending(moveOrder))
        {
            board.MakeMove(move);

            int move_score = -search(depth - 1, -beta, -alpha, false);

            board.UndoMove(move);

            if (move_score > best_score)
            {
                best_score = move_score;
                if (isTopLevel) searchBestMove = move;
            }
            if (best_score > alpha) alpha = best_score;
            if (alpha >= beta)
            {
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

        int stand_pat = evaluate();
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

        return score * (board.IsWhiteToMove ? 1 : -1);
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