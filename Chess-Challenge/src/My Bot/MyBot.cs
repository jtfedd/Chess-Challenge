using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// TODO
// - Search
//   - [x] Negamax
//   - [x] Iterative deepening
//   - [x] Transposition table
//   - [x] Transposition table move ordering
//   - [x] Quiescence search
//   - [x] Principal variation search
//   - [ ] Killer moves
//   - [ ] Delta pruning?
//   - [ ] Horizon pruning?
//   - [ ] Late move reduction
// - Evaluation
//   - [x] Piece values
//   - [x] Piece-square tables
//   - [ ] Endgame piece-square tables
//   - [x] Attack bonus
//   - [ ] Mobility bonus
//   - [ ] Pawn structure bonus
//   - [ ] Defended/attacked pieces bonus

// Token count 852

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 550, 900, 10000 };
    int[] bonusParams = { -20, -10, 10, -5, -5, 20, 50, 0, -25, 0, 0, 0, 25, 50, 50, 5, 10, -30 };

    Board b;
    ulong zKey => b.ZobristKey;

    Timer t;
    int msToThink;

    ulong tt_size = 1048583;
    TT_Entry[] tt;

    int[,,] pieceSquareBonuses;

    // Debug variables
    int cacheHits; // #DEBUG
    int nodesSearched; // #DEBUG
    int evaluations; // #DEBUG
    int cutoffs; // #DEBUG
    int quiesenceNodes; // #DEBUG

    bool cancelled => t.MillisecondsElapsedThisTurn > msToThink;

    Move searchBestMove;

    public MyBot()
    {
        tt = new TT_Entry[tt_size];

        pieceSquareBonuses = new int[6, 8, 8];
        for (int i = 0; i < 384; i++)
        {
            int row = i / 8 % 8;
            int col = i % 8;
            int piece = i / 64;

            // Create piece-square tables
            pieceSquareBonuses[piece, row, col] = bonusParams[piece] + pushBonus(row, bonusParams[piece + 6]) + centerBonus(row - 3.5, col - 3.5, bonusParams[piece + 12]);
            if (piece == 0 && row == 1 && (col < 3 || col > 4)) pieceSquareBonuses[piece, row, col] = 25;
            if (piece == 3 && (row == 6 || col == 3 || col == 4)) pieceSquareBonuses[piece, row, col] = 5;
        }
    }

    int pushBonus(int col, int amount) => amount * col / 8;

    int centerBonus(double row, double col, int amount) => (int)(amount * (5 - Math.Sqrt(row * row + col * col)) / 5);

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.IncrementMilliseconds + timer.MillisecondsRemaining / 40;

        b = board;
        t = timer;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        while (!cancelled)
        {
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG

            bestMove = searchBestMove;
            search(depth++, -100000, 100000, true);

            /*
            if (cancelled) Console.WriteLine($"Cancelled at depth {depth}"); //#DEBUG
            else//#DEBUG
            {//#DEBUG
                Console.WriteLine($"Depth {depth}");//#DEBUG


                int tt_full = 0;//#DEBUG
                for (ulong i = 0; i < tt_size; i++)//#DEBUG
                {//#DEBUG
                    if (tt[i].key != 0) tt_full++;//#DEBUG
                }//#DEBUG
                Console.WriteLine($"Transposition table has {tt_full} entries {((double)tt_full / (double)tt_size) * 100:0.00}% full");//#DEBUG


                Console.Write($"{eval} {searchBestMove} - "); //#DEBUG
                printPV(0);//#DEBUG
                Console.WriteLine();//#DEBUG

                Console.WriteLine($"Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs} Cache: {cacheHits}"); // #DEBUG

            }//#DEBUG

            depth++;
            */
        }

        // If we didn't come up with a best move then just take the first one we can get
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }


    int search(int depth, int alpha, int beta, bool isTopLevel)
    {
        bool quiesce = depth <= 0;

        if (quiesce) quiesenceNodes++; // #DEBUG
        else nodesSearched++; // #DEBUG

        if (cancelled) return 0;

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates.
        if (b.IsInCheckmate()) return b.PlyCount - 100000;

        // Check for draw by means other than stalemate. Stalemate will be checked when we generate moves.
        if (b.IsFiftyMoveDraw() || b.IsRepeatedPosition() || b.IsInsufficientMaterial()) return 0;

        // Adjust alpha and beta for the current ply of the game.
        beta = Math.Min(100000 - b.PlyCount, beta);
        alpha = Math.Max(b.PlyCount - 100000, alpha);

        // If we are in quiescense then adjust alpha for the possibility of not making any captures.
        if (quiesce) alpha = Math.Max(alpha, evaluate());

        Move bestMove = Move.NullMove;
        TT_Entry entry = tt[zKey % tt_size];
        if (entry.key == zKey)
        {
            bestMove = entry.bestMove;
            if (isTopLevel) searchBestMove = bestMove;

            if (quiesce || depth <= entry.depth)
            {
                // exact or upper bound
                if (entry.nodeType > 1) beta = Math.Min(entry.evaluation, beta);
                // exact or lower bound
                if (entry.nodeType < 3) alpha = Math.Max(entry.evaluation, alpha);
            }
        }

        // Early out if any of these conditions has caused the alpha/beta window to cut off.
        if (alpha >= beta) return alpha;

        var moves = b.GetLegalMoves(quiesce && !b.IsInCheck()).OrderByDescending(move => moveOrder(move, bestMove)).ToArray();

        // Check for stalemate
        if (moves.Length == 0) return quiesce ? alpha : 0;

        byte nodeType = 3; // Upper bound

        foreach (Move move in moves)
        {
            b.MakeMove(move);

            int move_score = -search(depth - 1, -alpha - 1, -alpha, false);
            if (move_score > alpha && move_score < beta) move_score = -search(depth - 1, -beta, -alpha, false);

            b.UndoMove(move);

            if (move_score > alpha)
            {
                bestMove = move;
                if (isTopLevel) searchBestMove = move;
                alpha = move_score;
                nodeType = 2; // Exact
            }

            if (alpha >= beta)
            {
                cutoffs++; //#DEBUG
                nodeType = 1; // Lower bound
                break;
            }
        }

        if (!cancelled && (entry.depth <= Math.Max(depth, 0))) tt[zKey % tt_size] = entry with { key = zKey, depth = depth, evaluation = alpha, nodeType = nodeType, bestMove = bestMove };

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

    int moveOrder(Move move, Move storedBest) => move.Equals(storedBest) ? 100000 : pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];

    struct TT_Entry
    {
        public ulong key;
        public int depth;
        public int evaluation;
        public Move bestMove;

        // 1 - Lower bound
        // 2 - Exact
        // 3 - Upper bound
        public byte nodeType;
    }

    public void benchmarkSearch(Board board, int maxDepth) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        t = new Timer(int.MaxValue); //#DEBUG
        this.msToThink = int.MaxValue; //#DEBUG

        int depth = 2;//#DEBUG
        Move bestMove = searchBestMove = Move.NullMove;//#DEBUG

        while (!cancelled && depth <= maxDepth)//#DEBUG
        {//#DEBUG
            cacheHits = 0; // #DEBUG
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG

            bestMove = searchBestMove;//#DEBUG
            int eval = search(depth, -100000, 100000, true);//#DEBUG

            if (cancelled) Console.WriteLine($"Cancelled at depth {depth}"); //#DEBUG
            else//#DEBUG
            {//#DEBUG
                Console.WriteLine($"Depth {depth}");//#DEBUG

                int tt_full = 0;//#DEBUG
                for (ulong i = 0; i < tt_size; i++)//#DEBUG
                {//#DEBUG
                    if (tt[i].key != 0) tt_full++;//#DEBUG
                }//#DEBUG
                Console.WriteLine($"Transposition table has {tt_full} entries {((double)tt_full / (double)tt_size) * 100:0.00}% full");//#DEBUG

                Console.Write($"{eval} {searchBestMove} - "); //#DEBUG
                printPV(0);//#DEBUG
                Console.WriteLine();//#DEBUG

                Console.WriteLine($"Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs} Cache: {cacheHits}"); // #DEBUG
            }//#DEBUG

            depth++;
        }//#DEBUG

        Console.WriteLine(bestMove);//#DEBUG
    }// #DEBUG

    void printPV(int depth)//#DEBUG
    {//#DEBUG
        if (depth > 10) return;//#DEBUG
        TT_Entry entry = tt[zKey % tt_size];//#DEBUG
        if (entry.key != zKey) return;//#DEBUG
        if (entry.bestMove == Move.NullMove) return;//#DEBUG

        Console.Write($"{entry.bestMove.StartSquare.Name}{entry.bestMove.TargetSquare.Name} ");//#DEBUG

        b.MakeMove(entry.bestMove);//#DEBUG
        printPV(depth + 1);//#DEBUG
        b.UndoMove(entry.bestMove);//#DEBUG
    }//#DEBUG


    void printPieceSquareBonuses()// #DEBUG
    {// #DEBUG
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
    }// #DEBUG
}