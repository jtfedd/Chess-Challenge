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

public class PieceSquareEval : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 550, 900, 10000 };
    int[] bonusParams = { -20, -10, 10, -5, -5, 20, 50, 0, -25, 0, 0, 0, 25, 50, 50, 5, 10, -30 };

    Board board;
    Timer timer;
    int tt_size = 1048583;
    TT_Entry[] tt;

    int[,,] pieceSquareBonuses;

    // Debug variables
    int cacheHits; // #DEBUG
    int nodesSearched; // #DEBUG
    int evaluations; // #DEBUG
    int cutoffs; // #DEBUG
    int quiesenceNodes; // #DEBUG
    int msToThink;
    bool cancelled;

    Move searchBestMove;

    public PieceSquareEval()
    {
        tt = new TT_Entry[tt_size];

        pieceSquareBonuses = new int[6,8,8];
        for (int i = 0; i < 384; i++)
        {
            int row = (i / 8) % 8;
            int col = i % 8;
            int piece = i / 64;

            // Create piece-square tables
            pieceSquareBonuses[piece, row, col] = bonusParams[piece] + pushBonus(row, bonusParams[piece+6]) + centerBonus(row - 3.5, col - 3.5, bonusParams[piece+12]);
            if (piece == 0 && row == 1 && (col < 3 || col > 4)) pieceSquareBonuses[piece, row, col] = 25;
            if (piece == 3 && (row == 6 || col == 3 || col == 4)) pieceSquareBonuses[piece, row, col] = 5;
        }

        /*
        for (int i = 0; i < 6; i++)// #DEBUG
        {// #DEBUG
            for (int row = 7; row >= 0; row--)// #DEBUG
            {// #DEBUG
                for (int col = 0; col < 8; col++)// #DEBUG
                {// #DEBUG
                    Console.Write($"{pieceSquareBonuses[i, row, col]} ");// #DEBUG
                }// #DEBUG
                Console.WriteLine();// #DEBUG
            }// #DEBUG
            Console.WriteLine();// #DEBUG
        } // #DEBUG
        */
    }

    int pushBonus(int col, int amount) => amount * (col) / 8;

    int centerBonus(double row, double col, int amount) => (int) (amount * (5 - Math.Sqrt(row * row + col * col)) / 5);

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.MillisecondsRemaining / 40;

        cancelled = false;

        this.board = board;
        this.timer = timer;

        //Console.WriteLine($"Current evaluation {evaluate()}"); // #DEBUG

        // Always evaluate at depth 2
        int depth = 2;
        search(depth, -int.MaxValue, int.MaxValue, true);
        Move bestMove = searchBestMove;
        //Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}"); // #DEBUG

        while (!cancelled)
        {
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG
            search(++depth, -int.MaxValue, int.MaxValue, true);
            if (!cancelled) bestMove = searchBestMove;
            //Console.WriteLine($"{(cancelled ? "Cancelled":"")} {depth} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}"); // #DEBUG
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

        nodesSearched++; // #DEBUG

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

        quiesenceNodes++; // #DEBUG

        if (board.IsInCheckmate()) return -int.MaxValue;
        else if (board.IsDraw()) return 0;

        int stand_pat = evaluate();
        if (stand_pat >= beta) return beta;
        if (alpha < stand_pat) alpha = stand_pat;

        var moves = board.GetLegalMoves(true).OrderByDescending(moveOrder).ToArray();

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
        evaluations++; // #DEBUG
        int score = 0;

        foreach (var pieces in board.GetAllPieceLists())
        {
            foreach (var piece in pieces)
            {
                int value = pieceValues[(int)piece.PieceType] + pieceSquareBonuses[(int)piece.PieceType - 1, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank, piece.Square.File] + 10 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite) & BitboardHelper.GetKingAttacks(board.GetKingSquare(!piece.IsWhite)));
                score += piece.IsWhite ? value : -value;
            }
        }

        return score * (board.IsWhiteToMove ? 1 : -1);
    }

    int moveOrder(Move move) => pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];

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