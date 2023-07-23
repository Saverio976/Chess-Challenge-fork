using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Move moveToPlay = GetNotSoRandomMove(board, allMoves);
        int highestValueCapture = 0;
        int powerValueCapture = 0;

        // -------------------------------------------------------------------
        // Check if opponent can make a move that will checkmate
        Nullable<Square> needDefend = DoesNeedDefend(board);
        if (needDefend.HasValue)
        {
            Nullable<Move> move = FindWayToGoSquare(board, allMoves, needDefend.Value);
            if (move.HasValue)
            {
                return move.Value;
            }

            foreach (Move moveTmp in allMoves)
            {
                if (moveTmp.StartSquare == board.GetKingSquare(board.IsWhiteToMove))
                {
                    board.MakeMove(moveTmp);
                    if (board.TrySkipTurn())
                    {
                        if (DoesNeedDefend(board).HasValue == false)
                        {
                            board.UndoSkipTurn();
                            board.UndoMove(moveTmp);
                            return moveTmp;
                        }
                        board.UndoSkipTurn();
                    }
                    board.UndoMove(moveTmp);
                }
            }
        }
        // -------------------------------------------------------------------

        // -------------------------------------------------------------------
        // Pick a promote queen if available
        foreach (Move move in allMoves)
        {
            if (move.PromotionPieceType == PieceType.Queen) {
                return move;
            }
        }
        // -------------------------------------------------------------------

        foreach (Move move in allMoves)
        {
            // ---------------------------------------------------------------
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move, 1))
            {
                moveToPlay = move;
                break;
            }
            // ---------------------------------------------------------------

            // ---------------------------------------------------------------
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
            // ---------------------------------------------------------------
        }

        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move, int recurseMax = 2)
    {
        board.MakeMove(move);
        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return true;
        }
        Move[] allMovesOponent = board.GetLegalMoves();
        bool isMate = recurseMax > 0 ? true : false;
        for (int oppI = 0; oppI < allMovesOponent.Length && isMate; oppI++)
        {
            Move mOpponent = allMovesOponent[oppI];
            isMate = false;
            board.MakeMove(mOpponent);
            Move[] allMovesMe = board.GetLegalMoves();
            for (int meI = 0; meI < allMovesMe.Length && !isMate ; meI++)
            {
                isMate = MoveIsCheckmate(board, allMovesMe[meI], recurseMax - 1);
            }
            board.UndoMove(mOpponent);
        }
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

    // Test if opponent can make a move that will checkmate
    Nullable<Square> DoesNeedDefend(Board board)
    {
        if (board.TrySkipTurn())
        {
            Move[] allMovesOpen = board.GetLegalMoves();
            foreach (Move move in allMovesOpen)
            {
                if (MoveIsCheckmate(board, move))
                {
                    board.UndoSkipTurn();
                    return move.TargetSquare;
                }
            }
            board.UndoSkipTurn();
        }
        return null;
    }

    // Find a way to go to a target in next turn
    Nullable<Move> FindWayToGoSquare(Board board, Move[] allMoves, Square target)
    {
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            if (board.TrySkipTurn())
            {
                Move[] allMovesNew = board.GetLegalMoves();
                foreach (Move m in allMovesNew)
                {
                    if (m.TargetSquare == target)
                    {
                        board.UndoSkipTurn();
                        board.UndoMove(move);
                        return move;
                    }
                }
                board.UndoSkipTurn();
            }
            board.UndoMove(move);
        }
        return null;
    }

    int GetPieceValueFromSquare(Square square, Board board)
    {
        return pieceValues[(int) board.GetPiece(square).PieceType];
    }

    Move GetNotSoRandomMove(Board board, Move[] allMoves)
    {
        List<Move> allMovesTmp = new();
        List<Move> allMovesTmp2 = new();

        foreach (Move move in allMoves)
        {
            if (move.MovePieceType != PieceType.King && move.MovePieceType != PieceType.Queen)
            {
                allMovesTmp.Add(move);
            }
            if (move.MovePieceType == PieceType.Queen)
            {
                allMovesTmp2.Add(move);
            }
        }
        Random rng = new();
        if (allMovesTmp.Count > 0) {
            return allMovesTmp[rng.Next(allMovesTmp.Count)];
        } else if (allMovesTmp2.Count > 0) {
            return allMovesTmp2[rng.Next(allMovesTmp2.Count)];
        }
        return allMoves[rng.Next(allMoves.Length)];
    }
}
