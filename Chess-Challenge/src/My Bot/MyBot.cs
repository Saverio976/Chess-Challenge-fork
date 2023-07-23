using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;
        int powerValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            int capturedPieceValue = GetPieceValueFromSquare(move.TargetSquare, board);
            if (capturedPieceValue > highestValueCapture)
            {
                // Check if not negativ brain to take
                int power = IsOkToTake(board, move);
                if (power > powerValueCapture) {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                    powerValueCapture = power;
                }
            }
        }

        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Test if superior on target (and power)
    int IsOkToTake(Board board, Move move)
    {
        int nbProtected = GetPieceValueFromSquare(move.TargetSquare, board);
        board.MakeMove(move);
        Move[] allMovesOponent = board.GetLegalMoves(true);
        foreach (Move m in allMovesOponent)
        {
            if (m.TargetSquare == move.TargetSquare)
            {
                nbProtected -= GetPieceValueFromSquare(m.StartSquare, board);
                board.MakeMove(m);
                Move[] allMovesMe = board.GetLegalMoves(true);
                foreach (Move mMe in allMovesMe)
                {
                    if (mMe.TargetSquare == move.TargetSquare)
                    {
                        nbProtected += IsOkToTake(board, mMe);
                    }
                }
                board.UndoMove(m);
            }
        }
        board.UndoMove(move);
        return nbProtected;
    }

    int GetPieceValueFromSquare(Square square, Board board)
    {
        return pieceValues[(int) board.GetPiece(square).PieceType];
    }
}
