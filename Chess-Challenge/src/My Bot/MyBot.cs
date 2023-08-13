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
    int fullSearches;
    int partialSearches;
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
            fullSearches = 0;
            partialSearches = 0;

            bestMove = searchBestMove;
            search(depth++, -int.MaxValue, int.MaxValue, true, false);


            //Console.Write($"{(cancelled ? "Cancelled " : "")}{eval} {searchBestMove} - "); //#DEBUG
            //printPV(0);
            //Console.WriteLine();

            /*
            int tt_full = 0;
            for (ulong i = 0; i < tt_size; i++)
            {
                if (tt[i] ) tt_full++;
            }
            Console.WriteLine($"Transposition table is {((double)tt_full / (double)tt_size)*100}% full");
            */


            //Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth - 1} Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs} Cache: {cacheHits} FS: {fullSearches} PS: {partialSearches}"); // #DEBUG
        }

        // If we didn't come up with a best move then just take the first one we can get
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }
    

    int search(int depth, int alpha, int beta, bool isTopLevel, bool quiesce)
    {
        beta = Math.Min(int.MaxValue - b.PlyCount, beta);
        alpha = Math.Max(b.PlyCount - int.MaxValue, alpha);
        if (alpha >= beta) return alpha;

        if (cancelled = t.MillisecondsElapsedThisTurn > msToThink) return 0;

        if (quiesce) quiesenceNodes++; // #DEBUG
        else nodesSearched++; // #DEBUG

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates
        if (b.IsInCheckmate()) return b.PlyCount - int.MaxValue;
        if (b.IsFiftyMoveDraw() || b.IsRepeatedPosition() || b.IsInsufficientMaterial()) return 0;

        // If we are in quiescense then give the option to not make any captures
        if (quiesce && (alpha = Math.Max(alpha, evaluate())) >= beta) return alpha;

        Move best_move = Move.NullMove;
        TT_Entry entry = tt[b.ZobristKey % tt_size];
        if (entry.key == b.ZobristKey)
        {
            cacheHits++; //#DEBUG
            best_move = entry.bestMove;

            if ((entry.depth >= depth) || quiesce)
            {
                if (entry.nodeType == 0)
                {
                    if (isTopLevel) searchBestMove = entry.bestMove;
                    return entry.evaluation;
                }
                if (entry.nodeType == 1)
                {
                    if (entry.evaluation <= alpha)
                    {
                        if (isTopLevel) searchBestMove = entry.bestMove;
                        return entry.evaluation;
                    }
                    else if (entry.evaluation < beta) beta = entry.evaluation;
                }
                if (entry.nodeType == 2)
                {
                    if (entry.evaluation >= beta)
                    {
                        if (isTopLevel) searchBestMove = entry.bestMove;
                        return entry.evaluation;
                    }
                    else if (entry.evaluation > alpha) alpha = entry.evaluation;
                }
            }
        }

        var moves = b.GetLegalMoves(quiesce).OrderByDescending(move => moveOrder(move, best_move)).ToArray();
        if (moves.Length == 0) return quiesce ? evaluate() : 0;

        best_move = Move.NullMove;
        byte node_type = 1;
        bool firstMove = true;

        foreach (Move move in moves)
        {
            b.MakeMove(move);

            bool needsFullSearch = firstMove;
            firstMove = false;

            int move_score = 0;
            if (!needsFullSearch)
            {
                move_score = -search(depth - 1, -alpha - 1, -alpha, false, depth <= 1);
                if (move_score > alpha && move_score < beta) needsFullSearch = true;
            }
            if (needsFullSearch)
            {
                move_score = -search(depth - 1, -beta, -alpha, false, depth <= 1);
                fullSearches++;
            } 
            else
            {
                partialSearches++;
            }

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

        if (!cancelled && (entry.depth <= Math.Min(depth, 0))) {
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

    public void benchmarkSearch(Board board, int maxDepth) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        t = new Timer(int.MaxValue); //#DEBUG
        this.cancelled = false;// #DEBUG
        this.msToThink = int.MaxValue;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        cancelled = false;
        while (!cancelled && depth <= maxDepth)
        {
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG
            fullSearches = 0;
            partialSearches = 0;

            bestMove = searchBestMove;
            int eval = search(depth++, -int.MaxValue, int.MaxValue, true, false);

            Console.Write($"{depth-1} {eval} {searchBestMove} - "); //#DEBUG
            printPV(0);
            Console.WriteLine();

            Console.WriteLine($"{(cancelled ? "Cancelled" : "")} {depth - 1} Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs} Cache: {cacheHits} FS: {fullSearches} PS: {partialSearches}"); // #DEBUG
        }

        Console.WriteLine(bestMove);
    }// #DEBUG

    void printPV(int depth)
    {
        if (depth > 50) return;
        TT_Entry entry = tt[b.ZobristKey % tt_size];
        if (entry.key != b.ZobristKey) return;
        if (entry.bestMove == Move.NullMove) return;

        Console.Write($"{entry.bestMove.StartSquare.Name}{entry.bestMove.TargetSquare.Name} ");

        b.MakeMove(entry.bestMove);
        printPV(depth+1);
        b.UndoMove(entry.bestMove);
    }

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