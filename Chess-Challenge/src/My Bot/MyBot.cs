using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    Random rng;
    Board board;

    public MyBot()
    {
        rng = new();
    }

    public Move Think(Board board, Timer timer)
    {
        this.board = board;

        int highestScore = int.MinValue;
        List<Move> candidateMoves = new();
        bool isWhite = board.IsWhiteToMove;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int score = -search(!isWhite, 2, -int.MaxValue, int.MaxValue);
            board.UndoMove(move);

            if (score > highestScore)
            {
                candidateMoves.Clear();
                highestScore = score;
            }
            
            if (score == highestScore) candidateMoves.Add(move);
        }

        return candidateMoves[rng.Next(candidateMoves.Count)];
    }

    int search(bool isWhite, int depth, int alpha, int beta)
    {
        int best_score = -int.MaxValue;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);

            int move_score = (depth == 0 || board.IsInCheckmate() || board.IsDraw()) 
                ? evaluate() * (isWhite ? 1 : -1)
                : -search(!isWhite, depth - 1, -beta, -alpha);

            board.UndoMove(move);

            if (move_score > best_score) best_score = move_score;
            if (best_score > alpha) alpha = best_score;
            if (alpha >= beta) return alpha;
        }

        return best_score;
    }

    int evaluate()
    {
        if (board.IsInCheckmate()) return -int.MaxValue;
        if (board.IsDraw()) return 0;

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
}