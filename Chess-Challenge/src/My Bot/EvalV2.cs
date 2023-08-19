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

// Token count 755

public class EvalV2 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 325, 550, 975, 10000 };
    ulong[] packedPV = { 0x32643C3732373732, 0x32643C37322D3C32, 0x3264463C32283C32, 0x3264504D4B321932, 0x000A141414140A00, 0x0A1E323732371E0A, 0x14323C41413C321E, 0x1432414646413714, 0x1E2828282828281E, 0x28323237323C3728, 0x283237373C3C320A, 0x28323C3C3C3C3228, 0x32372D2D2D2D2D32, 0x323C323232323232, 0x323C323232323237, 0x323C323232323237, 0x1E28282D3228281E, 0x2832323232373228, 0x2832373737373728, 0x2D32373737373237, 0x141414141E284646, 0x0A0A0A0A141E4650, 0x0A0A0A0A141E323C, 0x000000000A1E3232, 0x0014141414141400, 0x0A1E282828281414, 0x1428465050463214, 0x1E32505A5A503214 };

    Board b;
    ulong zKey => b.ZobristKey;

    Timer t;
    int msToThink;

    ulong tt_size = 1048583 * 2;
    TT_Entry[] tt;

    int[,,] pieceSquareBonuses;

    // Debug variables
    int nodesSearched; // #DEBUG
    int evaluations; // #DEBUG
    int cutoffs; // #DEBUG
    int quiesenceNodes; // #DEBUG

    bool cancelled => t.MillisecondsElapsedThisTurn > msToThink;

    Move searchBestMove;
    bool endgame;

    public EvalV2()
    {
        tt = new TT_Entry[tt_size];
        pieceSquareBonuses = new int[7, 8, 4];
        
        for (int i = 0; i < 224; i++) pieceSquareBonuses[i / 32, i % 8, i / 8 % 4] = (int)((packedPV[i / 8] >> (i % 8 * 8)) & 0x00000000000000FF);        

        //printPieceSquareBonuses();
    }

    bool isSideEndgame(bool isWhite)
    {
        bool noQueen = b.GetPieceBitboard(PieceType.Queen, isWhite) == 0;
        bool noRook = b.GetPieceBitboard(PieceType.Rook, isWhite) == 0;
        int minorPieceCount = BitboardHelper.GetNumberOfSetBits(b.GetPieceBitboard(PieceType.Bishop, isWhite) | b.GetPieceBitboard(PieceType.Knight, isWhite));
        return noQueen || (noRook && minorPieceCount < 2);
    }

    int getPieceSquareBonus(Piece piece)
    {
        int rank = piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank;
        int file = Math.Min(piece.Square.File, 7 - piece.Square.File);
        if (piece.IsKing) return pieceSquareBonuses[endgame ? 6 : 5, rank, file];
        return pieceSquareBonuses[(int)piece.PieceType - 1, rank, file];
    }

    public int testEval(Board board) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        return evaluate();// #DEBUG
    }// #DEBUG

    int evaluate()
    {
        evaluations++; // #DEBUG
        int score = 0;

        Square wkSquare = b.GetKingSquare(true);
        Square bkSquare = b.GetKingSquare(false);

        ulong whiteKingMobility = BitboardHelper.GetKingAttacks(wkSquare) | b.GetPieceBitboard(PieceType.King, true);
        ulong blackKingMobility = BitboardHelper.GetKingAttacks(bkSquare) | b.GetPieceBitboard(PieceType.King, false);

        foreach (var pieces in b.GetAllPieceLists())
        {
            foreach (var piece in pieces)
            {
                int value = pieceValues[(int)piece.PieceType];
                ulong attacks = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, b, piece.IsWhite);
                value += getPieceSquareBonus(piece);
                value += 10 * BitboardHelper.GetNumberOfSetBits(attacks & (piece.IsWhite ? blackKingMobility : whiteKingMobility));
                value += 3 * BitboardHelper.GetNumberOfSetBits(attacks) / 2;

                score += piece.IsWhite ? value : -value;
            }
        }

        score += 10 * BitboardHelper.GetNumberOfSetBits(whiteKingMobility & b.WhitePiecesBitboard);
        score -= 10 * BitboardHelper.GetNumberOfSetBits(blackKingMobility & b.BlackPiecesBitboard);

        return b.IsWhiteToMove ? score : -score;
    }

    int moveOrder(Move move, Move storedBest)
    {
        if (move.Equals(storedBest)) return 100000;
        int score = pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
        score -= getPieceSquareBonus(new Piece(move.MovePieceType, b.IsWhiteToMove, move.StartSquare));
        if (b.SquareIsAttackedByOpponent(move.TargetSquare)) score -= 10;
        if (move.IsCastles) score += 50;
        return score;
    }

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.IncrementMilliseconds + timer.MillisecondsRemaining / 40;

        b = board;
        t = timer;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        while (!cancelled)
        {
            nodesSearched = 0; // #DEBUG
            evaluations = 0; // #DEBUG
            cutoffs = 0; // #DEBUG
            quiesenceNodes = 0; // #DEBUG

            bestMove = searchBestMove;
            int eval = search(depth, -100000, 100000, true);
            
            if (cancelled) Console.WriteLine($"Cancelled at depth {depth}"); //#DEBUG
            else//#DEBUG
            {//#DEBUG
                Console.WriteLine($"Depth {depth}");//#DEBUG


               // int tt_full = 0;//#DEBUG
               // for (ulong i = 0; i < tt_size; i++)//#DEBUG
               // {//#DEBUG
               //     if (tt[i].key != 0) tt_full++;//#DEBUG
               // }//#DEBUG
               // Console.WriteLine($"Transposition table has {tt_full} entries {((double)tt_full / (double)tt_size) * 100:0.00}% full");//#DEBUG


                Console.Write($"{eval} {searchBestMove} - "); //#DEBUG
                printPV(0);//#DEBUG
                Console.WriteLine();//#DEBUG

                Console.WriteLine($"Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs}"); // #DEBUG

            }//#DEBUG

            depth++;
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

        endgame = isSideEndgame(true) && isSideEndgame(false);

        // If we are in quiescense then adjust alpha for the possibility of not making any captures.
        if (quiesce && !b.IsInCheck()) alpha = Math.Max(alpha, evaluate());

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

                Console.WriteLine($"Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs}"); // #DEBUG
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
        for (int i = 0; i < 7; i++)// #DEBUG
        {// #DEBUG
            for (int row = 7; row >= 0; row--)// #DEBUG
            {// #DEBUG
                for (int col = 0; col < 8; col++)// #DEBUG
                {// #DEBUG
                    Console.Write($"{pieceSquareBonuses[i, row, Math.Min(col, 7-col)]} ");// #DEBUG
                }// #DEBUG
                Console.WriteLine();// #DEBUG
            }// #DEBUG
            Console.WriteLine();// #DEBUG
        } // #DEBUG
    }// #DEBUG
}