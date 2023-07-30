using ChessChallenge.API;
using System;


public class MyBot : IChessBot {

    struct TreeNode {
        public float Visits;
        public float Value;
        public TreeNode[] Children;
        public Move[] Moves;
    }


    // public enum PieceType
    // {
    //     None,   // 0
    //     Pawn,   // 1
    //     Knight, // 2
    //     Bishop, // 3
    //     Rook,   // 4
    //     Queen,  // 5
    //     King    // 6
    // }
    // -----------------------------------------------------------------------
    // ---------------------- From https://discord.com/channels/1132289356011405342/1133068703588700362/1134687403102179491
    // -----------------------------------------------------------------------
    // None, Pawn, Knight, Bishop, Rook, Queen, King 
    private readonly int[] PieceMiddlegameValues = { 82, 337, 365, 477, 1025, 0 };
    private readonly int[] PieceEndgameValues =    { 94, 281, 297, 512, 936, 0 };
    private readonly int[] GamePhaseIncrement = { 0, 1, 1, 2, 4, 0 };
    // Big table packed with data from premade piece square tables
    // Unpack using PackedEvaluationTables[set, rank] = file
    private readonly ulong[] PackedEvaluationTables = {
        0, 17876852006827220035, 17442764802556560892, 17297209133870877174, 17223739749638733806, 17876759457677835758, 17373217165325565928, 0,
        13255991644549399438, 17583506568768513230, 2175898572549597664, 1084293395314969850, 18090411128601117687, 17658908863988562672, 17579252489121225964, 17362482624594506424,
        18088114097928799212, 16144322839035775982, 18381760841018841589, 18376121450291332093, 218152002130610684, 507800692313426432, 78546933140621827, 17502669270662184681,
        2095587983952846102, 2166845185183979026, 804489620259737085, 17508614433633859824, 17295224476492426983, 16860632592644698081, 14986863555502077410, 17214733645651245043,
        2241981346783428845, 2671522937214723568, 2819295234159408375, 143848006581874414, 18303471111439576826, 218989722313687542, 143563254730914792, 16063196335921886463,
        649056947958124756, 17070610696300068628, 17370107729330376954, 16714810863637820148, 15990561411808821214, 17219209584983537398, 362247178929505537, 725340149412010486,
        0, 9255278100611888762, 4123085205260616768, 868073221978132502, 18375526489308136969, 18158510399056250115, 18086737617269097737, 0,
        13607044546246993624, 15920488544069483503, 16497805833213047536, 17583469180908143348, 17582910611854720244, 17434276413707386608, 16352837428273869539, 15338966700937764332,
        17362778423591236342, 17797653976892964347, 216178279655209729, 72628283623606014, 18085900871841415932, 17796820590280441592, 17219225120384218358, 17653536572713270000,
        217588987618658057, 145525853039167752, 18374121343630509317, 143834816923107843, 17941211704168088322, 17725034519661969661, 18372710631523548412, 17439054852385800698,
        1010791012631515130, 5929838478495476, 436031265213646066, 1812447229878734594, 1160546708477514740, 218156326927920885, 16926762663678832881, 16497506761183456745,
        17582909434562406605, 580992990974708984, 656996740801498119, 149207104036540411, 17871989841031265780, 18015818047948390131, 17653269455998023918, 16424899342964550108,
    };

    private int GetSquareBonus(int type, int isWhite, int file, int rank)
    {
        // Mirror vertically for white pieces, since piece arrays are flipped vertically
        if (isWhite == 0)
            rank = 7 - rank;

        // Grab the correct byte representing the value
        // And multiply it by the reduction factor to get our original value again
        return (Math.Round(unchecked((sbyte)((PackedEvaluationTables[(type * 8) + rank] >> file * 8) & 0xFF)) * 1.461);
    }
    // --------------------------- END ---------------------------------------
    // -----------------------------------------------------------------------

    Board board;

    // -----------------------------------------------------------------------
    // ---------------------- From https://discord.com/channels/1132289356011405342/1134586635871326330/1134586635871326330
    // -----------------------------------------------------------------------
    public Move Think(Board startBoard, Timer timer)
    {
        board = startBoard;

        TreeNode root = new TreeNode {};

        int threshold = 2000 * timer.MillisecondsRemaining / 60000;
        while (timer.MillisecondsElapsedThisTurn < threshold && root.Visits < 1000000)
        {
            iteration(ref root);
        }

        // Following code could be reduced considerably,
        // After the search it plays which move/child averaged the best and returns it.
        float bestAvg = -1;
        Move bestMove = root.Moves[0];

        for (int i = 0; i < root.Moves.Length; i++){
            TreeNode child = root.Children[i];
            float avg = -child.Value / child.Visits;
            if (avg > bestAvg)
            {
                bestAvg = avg;
                bestMove = root.Moves[i];
            }
        }
        return bestMove;
    }



    // Iteration() handles all the core steps of the MCTS algorithm
    float iteration(ref TreeNode node)
    {
        // If we have reached a leaf node, we enter the EXPANSION step base case
        if (node.Visits == 0)
        {
            node.Visits = 1;
            node.Value = evaluation();
            return node.Value;
        }

        // Most leaf nodes will not be revisited, so only call expensive movegen on revisit
        if (node.Visits == 1)
        {
            node.Moves = board.GetLegalMoves();
            node.Children = new TreeNode[node.Moves.Length]; // You can save some memory by allocating this is as dynamically growing list, but it complicates other parts
        }

        // It can also be a leaf node because it is a terminal node, i.e. checkmate
        if (node.Moves.Length == 0)
            return node.Value / node.Visits;

        // Otherwise proceed to SELECTION step, we compute UCT for all children and select maximizing node
        // For more on UCT formula see [2] for details
        float part = 1.41f * MathF.Log(node.Visits); 
        float bestUCT = -1;

        // We store index because we're gonna look up child and its respective move, theres probably some way to save quite a few tokens
        int bestChildIdx = 0; 

        for (int i = 0; i < node.Moves.Length; i++)
        {
            TreeNode child = node.Children[i];

            // Avoid division by 0, further important discussion on FPU in footnotes
            float uct = child.Visits == 0 ?
                2000f
                : 
                (-child.Value / child.Visits) + MathF.Sqrt(part / child.Visits);

            if (uct >= bestUCT)
            {
                bestUCT = uct;
                bestChildIdx = i;
            }
        }

        // Move a level down the tree to our chosen node, and BACKPROPOGATE when it returns an evaluation up the tree
        Move exploreMove = node.Moves[bestChildIdx];
        board.MakeMove(exploreMove);
        float eval = -iteration(ref node.Children[bestChildIdx]);
        node.Value += eval;
        node.Visits++;
        board.UndoMove(exploreMove);

        return eval;
        // Thats it. Thats MCTS. https://www.youtube.com/watch?v=T1XgFsitnQw
    }
    // --------------------------- END ---------------------------------------
    // -----------------------------------------------------------------------

    float evaluation()
    {
        if (board.IsInsufficientMaterial() || board.IsRepeatedPosition()) return 0f;
        if (board.IsInCheckmate()) return -1f;

        int[] startGame_Score = {0, 0};
        int[] endGame_Score = {0, 0};
        int CurrentPhase = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            int type = (int)pieceList.TypeOfPieceInList - 1;
            int isWhite = pieceList.IsWhitePieceList ? 0 : 1;
            CurrentPhase += GamePhaseIncrement[type];
            foreach (Piece piece in pieceList)
            {
                int value =
                startGame_Score[isWhite]
                    += GetSquareBonus(type, isWhite, piece.Square.File, piece.Square.Rank)
                    + PieceMiddlegameValues[type];
                endGame_Score[isWhite]
                    += GetSquareBonus(type + 6, isWhite, piece.Square.File, piece.Square.Rank)
                    + PieceEndgameValues[type];
            }
        }
        int startGamePhase = CurrentPhase > 24 ? 24 : CurrentPhase;
        float score = (
            (startGame_Score[0] - startGame_Score[1]) * startGamePhase +
            (endGame_Score[0] - endGame_Score[1]) * (24 - startGamePhase)) / 24;

        if (!board.IsWhiteToMove)
            score = -score;
        return 0.9f * MathF.Tanh(score / 250f);
    }
}
