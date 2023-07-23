using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 333, 501, 880, 100000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Move moveToPlay = GetNotSoRandomMove(board, allMoves, board.IsWhiteToMove);
        int highestValueCapture = 0;
        int powerValueCapture = 0;

        // -------------------------------------------------------------------
        // Check if opponent can make a move that will checkmate
        Nullable<Square> needDefend = DoesNeedDefend(board);
        if (needDefend.HasValue)
        {
            // check if possible to go defend attacked position
            foreach (Move mMe in allMoves)
            {
                board.MakeMove(mMe);
                if (board.TrySkipTurn())
                {
                    Move[] allMovesNew = board.GetLegalMoves();
                    foreach (Move m in allMovesNew)
                    {
                        if (m.TargetSquare == needDefend.Value)
                        {
                            board.UndoSkipTurn();
                            board.UndoMove(mMe);
                            return mMe;
                        }
                    }
                    board.UndoSkipTurn();
                }
                board.UndoMove(mMe);
            }

            // check if possible to move away the king from the check
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
                if (MoveIsCheckmate(board, move, 2))
                {
                    board.UndoSkipTurn();
                    return move.TargetSquare;
                }
            }
            board.UndoSkipTurn();
        }
        return null;
    }

    int GetPieceValueFromSquare(Square square, Board board)
    {
        return pieceValues[(int) board.GetPiece(square).PieceType];
    }

    int GetPawnInvValue(Board board, Move move, bool isWhite)
    {
        int[] pos = {move.TargetSquare.File, move.TargetSquare.Rank};

        if ((pos[0] == 4 || pos[0] == 5) &&
                (pos[1] == 4 || pos[1] == 5))
        {
            return 0;
        }
        PieceList pawns = board.GetPieceList(PieceType.Pawn, isWhite);
        foreach (Piece p in pawns)
        {
            if (p.Square != move.StartSquare)
            {
                if (p.Square.File == move.StartSquare.File - 1 || p.Square.File == move.StartSquare.File + 1)
                {
                    return 0;
                }
            }
        }
        return 2;
    }

    int GetMoveValueInv(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] allMovesOpponent = board.GetLegalMoves(true);
        foreach (Move mOpp in allMovesOpponent)
        {
            if (move.TargetSquare == mOpp.TargetSquare && IsOkToTake(board, mOpp) > 0) {
                board.UndoMove(move);
                return 2;
            }
        }
        board.UndoMove(move);
        return 1;
    }

    Move GetNotSoRandomMove(Board board, Move[] allMoves, bool isWhite)
    {
        // 0: Pawn with diagonals or Pawns at center
        // 1: Knight Beshop Rook Queen not taken if move
        // 2: Other
        List<Move>[] allMoveTmp = {new(), new(), new()};

        foreach (Move move in allMoves)
        {
            switch (move.MovePieceType)
            {
                case PieceType.King:
                    break;
                case PieceType.Pawn:
                    allMoveTmp[GetPawnInvValue(board, move, isWhite)].Add(move);
                    break;
                default:
                    allMoveTmp[GetMoveValueInv(board, move)].Add(move);
                    break;
            }
        }
        Random rng = new();
        foreach (List<Move> moveTmp in allMoveTmp)
        {
            if (moveTmp.Count > 0)
            {
                return moveTmp[rng.Next(moveTmp.Count)];
            }
        }
        return allMoves[rng.Next(allMoves.Length)];
    }
}
