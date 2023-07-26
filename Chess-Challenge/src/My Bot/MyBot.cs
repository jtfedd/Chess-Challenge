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
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

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
        int score = pieceValueScore(board);
        board.UndoMove(move);
        return score;
    }

    int pieceValueScore(Board board)
    {
        int s = 0;

        foreach(PieceList pieces in board.GetAllPieceLists())
        {
            foreach(Piece piece in pieces)
            {
                int value = pieceValues[(int)piece.PieceType];
                s += piece.IsWhite ? value : -value;
            }
        }

        return s;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}