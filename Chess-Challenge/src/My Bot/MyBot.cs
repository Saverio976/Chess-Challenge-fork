using ChessChallenge.API;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 333, 501, 880, 100000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int bestMoveValue = int.MinValue;
        Move bestMove = allMoves[0];
        int currentPower = EvalPosition(board);

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            board.ForceSkipTurn();
            int power = EvalPosition(board) - currentPower;
            board.UndoSkipTurn();
            board.UndoMove(move);
            power += IsOkToTake(board, move) * 2;

            if (power > bestMoveValue)
            {
                bestMoveValue = power;
                bestMove = move;
            }

        }
        return bestMove;
    }

    int GetPieceValue(PieceType piece) => pieceValues[(int) piece];
    int GetPieceValueFromSquare(Square square, Board board) => GetPieceValue(board.GetPiece(square).PieceType);

    int EvalPosition(Board board)
    {
        int nbPiecePower = 0;
        int nbMoveDiff = 0;
        bool isWhite = board.IsWhiteToMove;

        // get number of move for current position
        nbMoveDiff = board.GetLegalMoves().Count();
        // force opponent turn
        board.ForceSkipTurn();
        // get number of move for opponent position in the same board
        nbMoveDiff -= board.GetLegalMoves().Count();
        // re put board in the state of the game
        board.UndoSkipTurn();
        // get number of piece power (me - opponent)
        foreach (Piece piece in board.GetAllPieceLists().SelectMany(p => p))
        {
            nbPiecePower += ((isWhite == piece.IsWhite) ? 1 : -1) * GetPieceValue(piece.PieceType);
        }
        return (nbPiecePower) + (nbMoveDiff);
    }

    // Test if superior on target (and power)
    int IsOkToTake(Board board, Move move)
    {
        int nbProtected = GetPieceValueFromSquare(move.TargetSquare, board);

        board.MakeMove(move);
        foreach (Move m in board.GetLegalMoves(true))
        {
            if (m.TargetSquare == move.TargetSquare)
            {
                nbProtected -= GetPieceValueFromSquare(m.StartSquare, board);
                board.MakeMove(m);
                foreach (Move mMe in board.GetLegalMoves(true))
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
}
