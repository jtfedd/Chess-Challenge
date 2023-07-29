using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class Negamax : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    Random rng;

    public Negamax()
    {
        rng = new();
    }

    public Move Think(Board board, Timer timer)
    {
        // Pick a random move to play if nothing better is found
        int highestScore = int.MinValue;
        List<Move> candidateMoves = new List<Move>();
        bool isWhite = board.IsWhiteToMove;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);

            int score = -search(board, !isWhite, 2, -int.MaxValue, int.MaxValue);

            board.UndoMove(move);

            if (score > highestScore)
            {
                candidateMoves.Clear();
                highestScore = score;
            }
            
            if (score == highestScore)
            {
                candidateMoves.Add(move);
            }
        }

        return candidateMoves[rng.Next(candidateMoves.Count)];
    }

    int search(Board board, bool isWhite, int depth, int alpha, int beta)
    {
        int best_score = -int.MaxValue;

        foreach (Move move in board.GetLegalMoves().OrderBy(c => rng.Next()))
        {
            board.MakeMove(move);

            int move_score;
            if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            {
                move_score = evaluate(board) * (isWhite ? 1 : -1);
            }
            else
            {
                move_score = -search(board, !isWhite, depth - 1, -beta, -alpha);
            }

            board.UndoMove(move);

            if (move_score > best_score) best_score = move_score;
            if (best_score > alpha) alpha = best_score;
            if (alpha >= beta) return alpha;
        }

        return best_score;
    }

    int evaluate(Board board)
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