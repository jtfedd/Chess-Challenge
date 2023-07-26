using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    public Move Think(Board board, Timer timer)
    { 
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        int highestScore = int.MinValue;
        List<Move> candidateMoves = new List<Move>();

        foreach (Move move in allMoves)
        {
            // Find move that results in the highest board score
            int score = moveScore(board, move);
            if (!board.IsWhiteToMove)
            {
                score = -score;
            }

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

    int moveScore(Board board, Move move)
    {
        board.MakeMove(move);
        int score = evaluate(board);
        board.UndoMove(move);
        return score;
    }

    int evaluate(Board board)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
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