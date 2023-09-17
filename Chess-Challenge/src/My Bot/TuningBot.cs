using Chess_Challenge.src.My_Bot;
using ChessChallenge.API;
using System;
using System.Linq;

// TODO
// - Search
//   - [x] Negamax
//   - [x] Iterative deepening
//   - [x] Transposition table
//   - [x] Transposition table move ordering
//   - [x] Quiescence search
//   - [x] Principal variation search
//   - [x] Better time management
//   - [ ] Better move ordering
//   - [ ] History heuristic
//   - [ ] Killer moves heuristic
//   - [ ] Delta pruning
//   - [ ] Checks during quiescence
//   - [ ] Promotions during quiescence
//   - [ ] Late move reduction
// - Evaluation
//   - [x] Piece values
//   - [x] Piece-square tables
//   - [x] Endgame piece-square tables
//   - [x] Attack bonus
//   - [x] Mobility bonus
//   - [x] Pawn structure bonus
//   - [x] Passed pawn bonus
//   - [x] Doubled pawn deduction
//   - [x] Bishop pair bonus
//   - [x] Bishop endgame bonus
//   - [x] Isolated pawn deduction
//   - [x] King safety
//   - [x] Relative material advantage

// Token count 1032

public class TuningBot : IChessBot
{
    // Tuning Parameters
    TuningParams tuning;

    public TuningBot(TuningParams p)
    {
        t = new Timer(int.MaxValue);
        msToThink = int.MaxValue;
        tuning = p;
        packedPV = p.pv;
        pieceValues[1] = p.pawn;
        pieceValues[2] = p.knight;
        pieceValues[3] = p.bishop;
        pieceValues[4] = p.rook;
        pieceValues[5] = p.queen;
    }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 325, 550, 975, 10000 };
    ulong[] packedPV;// = { 0x32643C3732373732, 0x32643C37322D3C32, 0x3264463C32283C32, 0x3264504D4B321932, 0x000A141414140A00, 0x0A1E323732371E0A, 0x14323C41413C321E, 0x1432414646413714, 0x1E2828282828281E, 0x28323237323C3728, 0x283237373C3C320A, 0x28323C3C3C3C3228, 0x32372D2D2D2D2D32, 0x323C323232323232, 0x323C323232323237, 0x323C323232323237, 0x1E28282D3228281E, 0x2832323232373228, 0x2832373737373728, 0x2D32373737373237, 0x141414141E284646, 0x0A0A0A0A141E4650, 0x0A0A0A0A141E323C, 0x000000000A1E3232, 0x0014141414141400, 0x0A1E282828281414, 0x1428465050463214, 0x1E32505A5A503214 };

    Board b;
    ulong zKey => b.ZobristKey;

    Timer t;
    int msToThink;

    TT_Entry[] tt = new TT_Entry[1048583];

    // Debug variables
    int nodesSearched; // #DEBUG
    int evaluations; // #DEBUG
    int cutoffs; // #DEBUG
    int quiesenceNodes; // #DEBUG

    bool cancelled => t.MillisecondsElapsedThisTurn > msToThink;

    Move searchBestMove;

    bool endgame;
    bool isSideEndgame(bool isWhite) => b.GetPieceBitboard(PieceType.Queen, isWhite) == 0 || (b.GetPieceBitboard(PieceType.Rook, isWhite) == 0 && BitboardHelper.GetNumberOfSetBits(b.GetPieceBitboard(PieceType.Bishop, isWhite) | b.GetPieceBitboard(PieceType.Knight, isWhite)) < 2);

    int score(bool isWhite)
    {
        int value = 0;

        var enemyKing = BitboardHelper.GetKingAttacks(b.GetKingSquare(!isWhite));
        var enemyPawns = b.GetPieceBitboard(PieceType.Pawn, !isWhite);

        int i = 0;
        while (++i < 7)
        {
            ulong pieces, pieceIter;
            int count = 0;

            pieces = pieceIter = b.GetPieceBitboard((PieceType)i, isWhite);

            while (pieceIter != 0)
            {
                count++;
                var index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceIter);
                var pieceSquareIndex = isWhite ? index : 63 - index;
                var pieceAttacks = BitboardHelper.GetPieceAttacks((PieceType)i, new Square(index), b, isWhite);

                value +=
                    pieceValues[i] +
                    // Add piece square bonus
                    (int)(packedPV[(endgame && i == 6 ? i : i - 1) * 4 + Math.Min(pieceSquareIndex % 8, 7 - pieceSquareIndex % 8)] >> pieceSquareIndex / 8 * 8 & 0x00000000000000FF) - 100 +
                    // Prefer piece mobility
                    tuning.mobility * BitboardHelper.GetNumberOfSetBits(pieceAttacks) +
                    // We like attacking the enemy king
                    tuning.kingAttack * BitboardHelper.GetNumberOfSetBits(pieceAttacks & enemyKing);

                // Pawns
                if (i == 1)
                {
                    int file = index % 8;

                    // We like pawns that defend other pawns
                    value += tuning.pawnChain * BitboardHelper.GetNumberOfSetBits(pieceAttacks & pieces);

                    // We don't like doubled pawns
                    if ((pieceIter & (0x0101010101010101ul << file)) != 0) value -= tuning.doubledPawns;

                    // We don't like isolated pawns
                    if ((index == 0 ? true : (pieces & (0x0101010101010101ul << (file - 1))) == 0) && (index == 7 ? true : (pieces & (0x0101010101010101ul << (file + 1))) == 0)) value -= tuning.isolatedPawns;

                    // We like passed pawns
                    pieceAttacks |= 1ul << index + (isWhite ? 8 : -8);
                    while (pieceAttacks != 0 && (pieceAttacks & enemyPawns) == 0) pieceAttacks = isWhite ? pieceAttacks << 8 : pieceAttacks >> 8;
                    if (pieceAttacks == 0) value += tuning.passedPawns;
                }

                // King
                if (i == 6) value += tuning.kingDefense * BitboardHelper.GetNumberOfSetBits(pieceAttacks & (isWhite ? b.WhitePiecesBitboard : b.BlackPiecesBitboard));
            }

            // Bishops
            if (i == 3)
            {
                // We want the bishop pair
                if (count > 1) value += tuning.bishopPair;
                // We want bishops in the endgame
                if (endgame) value += tuning.bishopEndgame * count;
            }
        }

        return value;
    }

    int evaluate(bool isWhite)
    {
        evaluations++; // #DEBUG
        endgame = isSideEndgame(true) && isSideEndgame(false);

        return score(isWhite) - score(!isWhite);
    }

    public int tuneEval(Board board)
    {
        b = board;
        return search(0, -100000, 100000, false);
    }

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.IncrementMilliseconds + Math.Min(timer.MillisecondsRemaining / 20, timer.GameStartTimeMilliseconds / 40);

        b = board;
        t = timer;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        while (!cancelled)
        {
            //resetCounters(); // #DEBUG

            bestMove = searchBestMove;
            search(depth++, -100000, 100000, true);

            //printMetrics(depth - 1, eval); // #DEBUG
        }

        // If we didn't come up with a best move then just take the first one we can get
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }

    int moveOrder(Move move, Move storedBest) => move.Equals(storedBest) ? 1000000 : pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];

    int search(int depth, int alpha, int beta, bool isTopLevel = false)
    {
        bool quiesce = depth <= 0;

        if (quiesce) quiesenceNodes++; // #DEBUG
        else nodesSearched++; // #DEBUG

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates.
        if (b.IsInCheckmate()) return b.PlyCount - 100000;

        // Check for draw by means other than stalemate. Stalemate will be checked when we generate moves.
        if (cancelled || b.IsFiftyMoveDraw() || b.IsRepeatedPosition() || b.IsInsufficientMaterial()) return 0;

        // Adjust alpha and beta for the current ply of the game.
        beta = Math.Min(100000 - b.PlyCount, beta);
        alpha = Math.Max(b.PlyCount - 100000, alpha);

        // If we are in quiescense then adjust alpha for the possibility of not making any captures.
        int eval = 0;
        if (quiesce && !b.IsInCheck())
        {
            eval = evaluate(b.IsWhiteToMove);
            if (eval < alpha - 1000) return alpha;
            alpha = Math.Max(alpha, eval);
        }

        Move bestMove = Move.NullMove;
        TT_Entry entry = tt[zKey % 1048583];
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
            if (quiesce && !b.IsInCheck() && move.IsCapture)
            {
                if (eval + pieceValues[(int)move.CapturePieceType] < alpha - 200) continue;
            }

            b.MakeMove(move);

            int move_score = -search(depth - 1, -alpha - 1, -alpha);
            if (move_score > alpha && move_score < beta) move_score = -search(depth - 1, -beta, -alpha);

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

        if (!cancelled && entry.depth <= Math.Max(depth, 0)) tt[zKey % 1048583] = entry with { key = zKey, depth = depth, evaluation = alpha, nodeType = nodeType, bestMove = bestMove };

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

    /*
    public TuningBot()//#DEBUG
    {//#DEBUG
        //printPieceSquareBonuses(); //#DEBUG
    }//#DEBUG
    */

    public int testEval(Board board) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        return evaluate(b.IsWhiteToMove);// #DEBUG
    }// #DEBUG

    public void benchmarkSearch(Board board, int maxDepth) // #DEBUG
    {// #DEBUG
        b = board;// #DEBUG
        t = new Timer(int.MaxValue); //#DEBUG
        msToThink = int.MaxValue; //#DEBUG

        int depth = 2;//#DEBUG
        Move bestMove = searchBestMove = Move.NullMove;//#DEBUG

        var start = DateTime.Now;//#DEBUG

        while (!cancelled && depth <= maxDepth)//#DEBUG
        {//#DEBUG
            resetCounters(); //#DEBUG

            bestMove = searchBestMove;//#DEBUG
            int eval = search(depth++, -100000, 100000, true);//#DEBUG

            printMetrics(depth - 1, eval); //#DEBUG
        }//#DEBUG

        Console.WriteLine(bestMove);//#DEBUG
        Console.WriteLine(DateTime.Now - start);//#DEBUG
        Console.WriteLine("Finished");//#DEBUG
    }// #DEBUG

    void resetCounters()// #DEBUG
    {// #DEBUG
        nodesSearched = 0; // #DEBUG
        evaluations = 0; // #DEBUG
        cutoffs = 0; // #DEBUG
        quiesenceNodes = 0; // #DEBUG
    }// #DEBUG

    void printMetrics(int depth, int eval)//#DEBUG
    {//#DEBUG
        if (cancelled) Console.WriteLine($"Cancelled at depth {depth}"); //#DEBUG
        else//#DEBUG
        {//#DEBUG
            Console.WriteLine($"Depth {depth}");//#DEBUG


            // int tt_full = 0;//#DEBUG
            // for (ulong i = 0; i < 1048583; i++)//#DEBUG
            // {//#DEBUG
            //     if (tt[i].key != 0) tt_full++;//#DEBUG
            // }//#DEBUG
            // Console.WriteLine($"Transposition table has {tt_full} entries {((double)tt_full / (double)1048583) * 100:0.00}% full");//#DEBUG


            Console.Write($"{eval} {searchBestMove} - "); //#DEBUG
            printPV(0);//#DEBUG
            Console.WriteLine();//#DEBUG

            Console.WriteLine($"Nodes: {nodesSearched} Quiesce: {quiesenceNodes} Evals: {evaluations} Cuts: {cutoffs}"); // #DEBUG

        }//#DEBUG
    }//#DEBUG

    void printPV(int depth)//#DEBUG
    {//#DEBUG
        if (depth > 10) return;//#DEBUG
        TT_Entry entry = tt[zKey % 1048583];//#DEBUG
        if (entry.key != zKey) return;//#DEBUG
        if (entry.bestMove == Move.NullMove) return;//#DEBUG

        Console.Write($"{entry.bestMove.StartSquare.Name}{entry.bestMove.TargetSquare.Name} ");//#DEBUG

        b.MakeMove(entry.bestMove);//#DEBUG
        printPV(depth + 1);//#DEBUG
        b.UndoMove(entry.bestMove);//#DEBUG
    }//#DEBUG


    public void printPieceSquareBonuses()// #DEBUG
    {// #DEBUG
        for (int i = 0; i < 7; i++)// #DEBUG
        {// #DEBUG
            for (int row = 7; row >= 0; row--)// #DEBUG
            {// #DEBUG
                for (int col = 0; col < 8; col++)// #DEBUG
                {// #DEBUG
                    Console.Write($"{getPieceSquareBonus(i, new Square(col, row).Index)} ");// #DEBUG
                }// #DEBUG

                Console.Write("\t\t");//#DEBUG

                for (int col = 0; col < 8; col++)// #DEBUG
                {// #DEBUG
                    Console.Write($"{getPieceSquareBonus(i, 63 - new Square(col, row).Index)} ");// #DEBUG
                }// #DEBUG
                Console.WriteLine();// #DEBUG
            }// #DEBUG
            Console.WriteLine();// #DEBUG
        } // #DEBUG
    }// #DEBUG

    int getPieceSquareBonus(int pieceType, int pieceSquareIndex) => (int)(packedPV[pieceType * 4 + Math.Min(pieceSquareIndex % 8, 7 - pieceSquareIndex % 8)] >> pieceSquareIndex / 8 * 8 & 0x00000000000000FF) - 100; //#DEBUG
}