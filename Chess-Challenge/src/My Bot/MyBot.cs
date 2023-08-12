using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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

// Token count 768

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 550, 900, 10000 };
    int[] bonusParams = { -20, -10, 10, -5, -5, 20, 50, 0, -25, 0, 0, 0, 25, 50, 50, 5, 10, -30 };

    Board b;
    Timer t;
    ulong tt_size = 1048583;
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

    public MyBot()
    {
        tt = new TT_Entry[tt_size];

        pieceSquareBonuses = new int[6,8,8];
        for (int i = 0; i < 384; i++)
        {
            int row = i / 8 % 8;
            int col = i % 8;
            int piece = i / 64;

            // Create piece-square tables
            pieceSquareBonuses[piece, row, col] = bonusParams[piece] + pushBonus(row, bonusParams[piece+6]) + centerBonus(row - 3.5, col - 3.5, bonusParams[piece+12]);
            if (piece == 0 && row == 1 && (col < 3 || col > 4)) pieceSquareBonuses[piece, row, col] = 25;
            if (piece == 3 && (row == 6 || col == 3 || col == 4)) pieceSquareBonuses[piece, row, col] = 5;
        }
    }

    int pushBonus(int col, int amount) => amount * col / 8;

    int centerBonus(double row, double col, int amount) => (int) (amount * (5 - Math.Sqrt(row * row + col * col)) / 5);

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.IncrementMilliseconds + timer.MillisecondsRemaining / 40;

        b = board;
        t = timer;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        cancelled = false;
        while (!cancelled)
        {
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG

            bestMove = searchBestMove;
            search(depth++, -int.MaxValue, int.MaxValue, true, false);

            /*
            Console.Write($"{(cancelled ? "Cancelled " : "")}{eval} {searchBestMove} - "); //#DEBUG
            printPV();
            Console.WriteLine();

            int tt_full = 0;
            for (ulong i = 0; i < tt_size; i++)
            {
                if (tt[i] != null) tt_full++;
            }
            Console.WriteLine($"Transposition table is {((double)tt_full / (double)tt_size)*100}% full");
            */

            Console.WriteLine($"{(cancelled ? "Cancelled":"")} {depth-1} Nodes searched: {nodesSearched} Quiecense nodes: {quiesenceNodes} evaluations: {evaluations} cutoffs: {cutoffs} cache hits: {cacheHits}"); // #DEBUG
        }

        // If we didn't come up with a best move then just take the first one we can get
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }

    /*
    void printPV()
    {
        TT_Entry entry = tt[b.ZobristKey % tt_size];
        if (entry?.key != b.ZobristKey) return;
        if (entry.bestMove == Move.NullMove) return;

        Console.Write($"{entry.bestMove.StartSquare.Name}{entry.bestMove.TargetSquare.Name} ");

        b.MakeMove(entry.bestMove);
        printPV();
        b.UndoMove(entry.bestMove);
    }
    */

    int search(int depth, int alpha, int beta, bool isTopLevel, bool quiesce)
    {
        if (cancelled = t.MillisecondsElapsedThisTurn > msToThink) return 0;

        if (quiesce) quiesenceNodes++; // #DEBUG
        else nodesSearched++; // #DEBUG

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates
        if (b.IsInCheckmate()) return -int.MaxValue + b.PlyCount;
        if (b.IsDraw()) return 0;

        // If we are in quiescense then give the option to not make any captures
        if (quiesce && (alpha = Math.Max(alpha, evaluate())) >= beta) return alpha;

        Move best_move = Move.NullMove;
        TT_Entry entry = tt[b.ZobristKey % tt_size];
        if (entry.key == b.ZobristKey && ((entry.depth >= depth) || quiesce))
        {
            cacheHits++; //#DEBUG
            if (entry.nodeType == 0)
            {
                if (isTopLevel) searchBestMove = entry.bestMove;
                return entry.evaluation;
            }
            if (entry.nodeType == 1 && entry.evaluation <= alpha)
            {
                if (isTopLevel) searchBestMove = entry.bestMove;
                return entry.evaluation;
            }
            if (entry.nodeType == 2 && entry.evaluation >= beta)
            {
                if (isTopLevel) searchBestMove = entry.bestMove;
                return entry.evaluation;
            }

            best_move = entry.bestMove;
        }

        var moves = b.GetLegalMoves(quiesce).OrderByDescending(move => moveOrder(move, best_move));

        best_move = Move.NullMove;
        byte node_type = 1;

        foreach (Move move in moves)
        {
            b.MakeMove(move);
            int move_score = -search(depth - 1, -beta, -alpha, false, depth <= 1);
            b.UndoMove(move);

            if (move_score > alpha)
            {
                best_move = move;
                if (isTopLevel) searchBestMove = move;
                alpha = move_score;
                node_type = 0;
            }
            if (alpha >= beta)
            {
                cutoffs++; //#DEBUG
                node_type = 2;
                break;
            }
        }

        if (entry.depth <= depth) {
            tt[b.ZobristKey % tt_size].key = b.ZobristKey;
            tt[b.ZobristKey % tt_size].depth = depth;
            tt[b.ZobristKey % tt_size].evaluation = alpha;
            tt[b.ZobristKey % tt_size].nodeType = node_type;
            tt[b.ZobristKey % tt_size].bestMove = best_move;
            }

        return alpha;
    }

    int evaluate()
    {
        evaluations++; // #DEBUG
        int score = 0;

        foreach (var pieces in b.GetAllPieceLists())
        {
            foreach (var piece in pieces)
            {
                int value = 
                    pieceValues[(int)piece.PieceType] + 
                    pieceSquareBonuses[(int)piece.PieceType - 1, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank, piece.Square.File] + 
                    10 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, b, piece.IsWhite) & BitboardHelper.GetKingAttacks(b.GetKingSquare(!piece.IsWhite)));
                score += piece.IsWhite ? value : -value;
            }
        }

        return b.IsWhiteToMove ? score : -score;
    }

    int moveOrder(Move move, Move storedBest) => move.Equals(storedBest) ? int.MaxValue : pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];

    struct TT_Entry
    {
        public ulong key;
        public int depth;
        public int evaluation;
        public Move bestMove;

        // 0 - Exact
        // 1 - Upper bound
        // 2 - Lower bound
        public byte nodeType;
    }

    public void benchmarkSearch(Board board, int depth) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        t = new Timer(int.MaxValue); //#DEBUG
        this.cancelled = false;// #DEBUG
        search(depth, -int.MaxValue, int.MaxValue, true, false);// #DEBUG
    }// #DEBUG

    /*
    void printPieceSquareBonuses()
    {
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
    }
    */
}