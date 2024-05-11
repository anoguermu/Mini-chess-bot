using ChessChallenge.API;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System;


namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        struct transp
        {
            public int eval;
            public Move bestMove;
            public int depth;
        }
        //is a map in a white prespective 
        short[] pawns_map =
        {
        0,0,0,0,0,0,0,0,
        50,50,50,50,50,50,50,50,
        12,12,25,35,35,25,12,12,
        6,6,12,30,30,12,6,6,
        0,0,0,25,25,0,0,0,
        6,-6,-13,0,0,-13,-6,6,
        6,12,12,-25,-25,10,10,6,
        0,0,0,0,0,0,0,0,
    };
        short[] knight_map =     {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,0,0,0,0,-20,-40,
        -30,0,0,0,0,0,0,-30,
        -30,5,15,20,20,15,5,-30,
        -30,0,15,20,20,15,0,-30,
        -30,5,15,20,20,15,5,-30,
        -40,-20,0,0,0,0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };

        short[] bishop_map =     {
        -25,-15,-15,-15,-15,-15,-15,-25,
        -12,0,0,0,0,0,0,-12,
        -11,0,6,10,10,6,0,-11,
        -11,6,6,11,11,6,6,-11,
        -11,0,12,12,12,12,0,-11,
        -11,12,12,12,12,12,12,-11,
        -12,7,0,0,0,0,7,-12,
        -25,-15,-15,-15,-15,-15,-15,-25,
    };


        short[] rook_map =     {
        0,0,0,0,0,0,0,0,
        5,11,11,11,11,11,11,5,
        -6,0,0,0,0,0,0,-6,
        -6,0,0,0,0,0,0,-6,
        -6,0,0,0,0,0,0,-6,
        -6,0,0,0,0,0,0,-6,
        -6,0,0,0,0,0,0,-6,
        0,0,0,7,7,0,0,0,
    };
        short[] queen_map =     {
        -20,-11,-11,-6,-6,-11,-11,-20,
        -11,0,0,0,0,0,0,-11,
        -11,0,7,7,7,7,0,-11,
        -11,0,7,7,7,7,0,-11,
        0,0,6,6,6,6,0,-6,
        -11,6,6,6,6,6,0,-11,
        -11,0,5,0,0,0,0,-11,
        -20,-11,-11,-6,-6,-11,-11,-20,
    };
        short[] king_map =     {
        0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,
        20,20,0,0,0,0,20,20,
        20,30,10,0,0,10,30,20,
    };



        // Piece values: null, pawn, knight, bishop, rook, queen, king
        short[] pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };
        Dictionary<ulong, transp> transpositons = new Dictionary<ulong, transp>();
        const float endgameMaterialStart = 1620; //2 rooks + 1 bishop + 1 knight
        public class Foo
        {
            public Move move { get; set; }
        }
        public Move Think(Board board, ChessChallenge.API.Timer timer)
        {

            Foo move = new Foo();
            Mov(board, 5, move, true, -999999, +999999);
            return move.move;
        }

        public int Searchallcaptures(Board board, int alpha, int beta)
        {
            transp positiontoadd = new transp();
            int eval = Evaluate(board);
            if (eval > beta)
            {
                if (!transpositons.ContainsKey(board.ZobristKey)) transpositons.Add(board.ZobristKey, positiontoadd);
                else transpositons[board.ZobristKey] = positiontoadd;
                return beta;
            }
            alpha = Math.Max(alpha, eval);
            Move[] captures = board.GetLegalMoves(true);
            sortMov(captures, board);

            foreach (Move move in captures)
            {
                board.MakeMove(move);
                eval = -Searchallcaptures(board, -beta, -alpha);
                board.UndoMove(move);
                if (eval > beta)
                {
                    if (!transpositons.ContainsKey(board.ZobristKey)) transpositons.Add(board.ZobristKey, positiontoadd);
                    else transpositons[board.ZobristKey] = positiontoadd;
                    return beta;
                }
                if (eval > alpha)
                {
                    positiontoadd.eval = eval;
                    positiontoadd.bestMove = move;
                    positiontoadd.depth = 0;
                    alpha = eval;
                }
            }
            if (!transpositons.ContainsKey(board.ZobristKey)) transpositons.Add(board.ZobristKey, positiontoadd);
            else transpositons[board.ZobristKey] = positiontoadd;
            return alpha;
        }
        public int king_endgame(Square frendlyKingSquare, Square enemyKingSquare, float endgameWeight)
        {
            int enemyDistToCenterFile = Math.Max(4 - enemyKingSquare.File, enemyKingSquare.File - 3);
            int enemyDistToCenterRank = Math.Max(4 - enemyKingSquare.Rank, enemyKingSquare.Rank - 3);
            int eval = enemyDistToCenterFile + enemyDistToCenterRank;

            int distanceBetweenKingsFile = Math.Abs(frendlyKingSquare.File - enemyKingSquare.File);
            int distanceBetweenKingsRanks = Math.Abs(frendlyKingSquare.Rank - enemyKingSquare.Rank);
            eval += 14 - (distanceBetweenKingsFile + distanceBetweenKingsRanks);
            return (int)(10 * eval * endgameWeight);
        }
        float endgameWeight(int materialWeight)
        {
            return 1 - Math.Min(1, materialWeight * (1 / endgameMaterialStart));
        }
        int evalPiceMap(PieceList pieces)
        {
            int pice = (int)pieces.TypeOfPieceInList;
            int sum = 0;
            foreach (Piece p in pieces)
            {
                int square = (p.IsWhite ? (7 - p.Square.Rank) * 8 + p.Square.File : p.Square.Index);

                switch (pice)
                {

                    case 1:

                        sum += pawns_map[square];
                        break;
                    case 2:
                        sum += knight_map[square];
                        break;
                    case 3:
                        sum += bishop_map[square];
                        break;
                    case 4:
                        sum += rook_map[square];
                        break;
                    case 5:
                        sum += queen_map[square];
                        break;
                    case 6:
                        sum += king_map[square];
                        break;
                }

            }
            return sum;
        }

        public int Evaluate(Board board)
        {
            int whitem = 0;
            int blackm = 0;
            PieceList[] pieces = board.GetAllPieceLists();
            int wpm = 0;
            int bpm = 0;
            foreach (PieceList list in pieces)
            {
                if (list.IsWhitePieceList) { whitem += pieceValues[(int)list.TypeOfPieceInList] * list.Count; wpm += evalPiceMap(list); }
                else { blackm += pieceValues[(int)list.TypeOfPieceInList] * list.Count; bpm += evalPiceMap(list); }
                //suma de material per color quantitat i tipus en la prespectiva de blanques
            }
            whitem += king_endgame(board.GetKingSquare(true), board.GetKingSquare(false), endgameWeight(blackm - ((int)board.GetPieceList((PieceType)1, false).Count) * 100));
            blackm += king_endgame(board.GetKingSquare(false), board.GetKingSquare(true), endgameWeight(whitem - ((int)board.GetPieceList((PieceType)1, true).Count) * 100));
            return (board.IsWhiteToMove ? 1 : -1) * (whitem - blackm);
        }
        public int Mov(Board board, int depth, Foo bestmove, bool root, int alpha, int beta)
        {
            transp pos = new transp();
            if (findPosinTT(pos, board.ZobristKey) && pos.depth == depth) return pos.eval;


            if (board.IsInCheckmate()) return -99999 - depth;
            else if (board.IsDraw()) return 0;
            else if (depth == 0) return Evaluate(board);

            Move[] movements = board.GetLegalMoves();
            sortMov(movements, board);
            transp positiontoadd = new transp();
            foreach (Move move in movements)
            {
                board.MakeMove(move);
                int eval = -Mov(board, depth - 1, bestmove, false, -beta, -alpha);
                board.UndoMove(move);
                //Moviment masa bo pq el rival mel deixi fer
                if (eval >= beta)
                {
                    positiontoadd.depth = depth;
                    positiontoadd.eval = eval;
                    positiontoadd.bestMove = move;
                    if (!transpositons.ContainsKey(board.ZobristKey)) transpositons.Add(board.ZobristKey, positiontoadd);
                    else transpositons[board.ZobristKey] = positiontoadd;
                    return beta;
                }
                if (eval > alpha)
                {
                    positiontoadd.eval = eval;
                    positiontoadd.bestMove = move;
                    positiontoadd.depth = depth;
                    alpha = eval;
                    if (root) bestmove.move = move;
                }
            }
            if (!transpositons.ContainsKey(board.ZobristKey)) transpositons.Add(board.ZobristKey, positiontoadd);
            else transpositons[board.ZobristKey] = positiontoadd;
            return alpha;
        }


        public void sortMov(Move[] moves, Board board)
        {

            int[] moveEval = new int[moves.Length];
            for (int i = 0; i < moves.Length; ++i)
            {
                int evalMove = 0;
                Move CurrentMove = moves[i];
                if (CurrentMove.IsCapture) evalMove += 10 * pieceValues[(int)CurrentMove.CapturePieceType] - pieceValues[(int)CurrentMove.MovePieceType];

                if (CurrentMove.IsPromotion) evalMove += pieceValues[(int)CurrentMove.PromotionPieceType];

                if ((int)CurrentMove.MovePieceType != 1) if (board.SquareIsAttackedByOpponent(CurrentMove.TargetSquare)) evalMove -= 50;
                if (board.IsInCheck()) evalMove += 1000;
                if (transpositons.ContainsKey(board.ZobristKey)) evalMove += 2000;



                moveEval[i] = evalMove;
            }
            ins_sort(moves, moveEval);

        }
        public void ins_sort(Move[] moves, int[] moveEval)
        {
            for (int i = 1; i < moves.Length; i++)
            {
                for (int j = i; j > 0 && moveEval[j - 1] < moveEval[j]; --j)
                {
                    Move aux = moves[j];
                    moves[j] = moves[j - 1];
                    moves[j - 1] = aux;
                    int aux2 = moveEval[j];
                    moveEval[j] = moveEval[j - 1];
                    moveEval[j - 1] = aux2;
                }
            }
        }


        private bool findPosinTT(transp pos, ulong position)
        {
            if (transpositons.TryGetValue(position, out pos)) return true;
            return false;
        }
    }
}