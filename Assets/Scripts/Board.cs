using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static Board;

public class Board
{
    //piece struct
    public struct Piece
    {
        public enum Type
        {
            None,
            King,
            Queen,
            Bishop,
            Knight,
            Rook,
            Pawn
        }

        public enum Color
        {
            None,
            White,
            Black
        }

        public Type type;
        public Color color;
    }
    //move struct
    public struct Move
    {
        //move Flags
        public enum Flags
        {
            None,
            DoublePush,
            Promotion,
            EnPassant,
            CastleShort,
            CastleLong
        }

        //for every move
        public int squareSourceIndex, squareTargetIndex;
        public Piece pieceSource, pieceTarget;
        public Flags flags;

        // promotion

        public Piece.Type promotionPieceType;
    }

    // game state struct

    public struct State
    {
        public Piece.Color turnColor;
        public Piece.Color doublePushedPawnColor; //color of the doublepushed pawn to check if enPassant is avaiable
        public int enPassantSquareIndex; //pawn tile which can be captured by enPassant
        public bool canCastleWhite, canCastleShortWhite, canCastleLongWhite;
        public bool canCastleBlack, canCastleShortBlack, canCastleLongBlack;
    }

    public static readonly string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    // towers squares & king squares

    public static readonly int A1 = 56, H1 = 63, A8 = 0, H8 = 7; // tower squares
    public static readonly int E1 = 60, E8 = 4; // king squares
    public static readonly int C1 = 58, C8 = 2, G1 = 62, G8 = 6;
    public static readonly int F1 = 61, D1 = 59, F8 = 5, D8 = 3; // tower castling target squares


    // promotion piece

    public Piece.Type PromotionPieceType = Piece.Type.Queen;

    //pieces array
    //private Piece[] pieces = new Piece[64];
    public Piece[] pieces = new Piece[64];
    private List<int> whitePiecesIndicies = new List<int>();
    private List<int> blackPiecesIndicies = new List<int>();

    //stack the moves and states as the game is progressing

    private Stack<Move> moves = new Stack<Move>();
    private Stack<State> states = new Stack<State>();

    //stack to capture the moves that were undone by undo button
    private Stack<Move> undoneMoves = new Stack<Move>();

    private State currentState = new State();

    //gets piece at index
    public Piece GetPiece(int index)
    {
        return pieces[index];
    }

    //gets pieces indicies from color
    public List<int> GetPiecesIndiciesByColor(Piece.Color color)
    {
        switch (color)
        {
            case Piece.Color.White:
                return whitePiecesIndicies;
            case Piece.Color.Black:
                return blackPiecesIndicies;
        }
        return null;
    }

    //get last move
    public bool TryGetLastMove(out Move move)
    {
        return moves.TryPeek(out move);
    }

    //get state
    public ref readonly State GetState()
    {
        return ref currentState;
    }

    //get turn color
    public Piece.Color GetTurnColor()
    {
        return currentState.turnColor;
    }

    //get states count
    public int GetStateCount()
    {
        return states.Count;
    }

    //get undone moves count
    public int GetUndoneMoveCount()
    {
        return undoneMoves.Count;
    }

    // find king

    public int FindKingOfColor(Piece.Color color)
    {
        List<int> indicies = GetPiecesIndiciesByColor(color);

        foreach (int index in indicies)
        {
            if (pieces[index].type == Piece.Type.King)
            {
                return index;
            }
        }

        Debug.Log("No king???");

        return 0;

    }

    // copy board pieces and current state to other board
    public void CopyBoardState(Board board)
    {
        pieces.CopyTo(board.pieces, 0);
        board.currentState = currentState;
        board.whitePiecesIndicies = whitePiecesIndicies;
        board.blackPiecesIndicies = blackPiecesIndicies;
    }


    //Set the FEN string

    public void LoadFEN(string fen)
    {
        //clear
        states.Clear();
        moves.Clear();
        whitePiecesIndicies.Clear();
        blackPiecesIndicies.Clear();

        //split FEN string
        string[] splittedFEN = fen.Split(' ');
        // For fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -"
        // The splittedFEN array:
        // splittedFEN[0] = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"
        // splittedFEN[1] = "w"
        // splittedFEN[2] = "KQkq"
        // splittedFEN[3] = "-"


        //piece placement splittedFEN[0]
        Dictionary<char, Piece.Type> pieceTypeFromSymbol = new Dictionary<char, Piece.Type>()
        {
            { 'p', Piece.Type.Pawn }, { 'n', Piece.Type.Knight }, { 'b', Piece.Type.Bishop },
            { 'r', Piece.Type.Rook }, { 'q', Piece.Type.Queen  }, { 'k', Piece.Type.King   }
        };

        //bottom rank is 7
        int rank = 0, file = 0;

        //fill up the board with fen notation starting from above
        foreach (char symbol in splittedFEN[0])
        {
            if (symbol == '/')
            {
                file = 0;
                rank++;
            }
            else if (char.IsDigit(symbol))
            {
                int n = symbol - '0';// number of empty squares

                for (int i = 0; i < n; i++)
                {
                    pieces[(file + i) + rank * 8] = new Piece(); // "none" piece
                }

                file += n;
            }
            else
            {
                int index = file + rank * 8;
                Piece.Type pieceType = pieceTypeFromSymbol[char.ToLower(symbol)]; // must be a piece symbol
                Piece.Color pieceColor = char.IsUpper(symbol) ? Piece.Color.White : Piece.Color.Black;
                pieces[index] = new Piece { type = pieceType, color = pieceColor };
                file++;

                //pieces list
                switch (pieceColor)
                {
                    case Piece.Color.White:
                        whitePiecesIndicies.Add(index);
                        break;
                    case Piece.Color.Black:
                        blackPiecesIndicies.Add(index);
                        break;
                }

            }
        }

        // turn color splittedFEN[1]
        currentState.turnColor = splittedFEN[1].Equals("w") ? Piece.Color.White : Piece.Color.Black;

        // castling rights splittedFEN[2]

        currentState.canCastleShortWhite = false;
        currentState.canCastleLongWhite = false;
        currentState.canCastleShortBlack = false;
        currentState.canCastleLongBlack = false;

        if (splittedFEN[2].Equals("-"))
        {
            // no side can castle
            currentState.canCastleWhite = false;
            currentState.canCastleBlack = false;
        }
        else
        {
            foreach (char symbol in splittedFEN[2])
            {
                switch (symbol)
                {
                    case 'K':
                        currentState.canCastleShortWhite = true;
                        break;
                    case 'Q':
                        currentState.canCastleLongWhite = true;
                        break;
                    case 'k':
                        currentState.canCastleShortBlack = true;
                        break;
                    case 'q':
                        currentState.canCastleLongBlack = true;
                        break;
                }
            }

            currentState.canCastleWhite = currentState.canCastleShortWhite || currentState.canCastleLongWhite;
            currentState.canCastleBlack = currentState.canCastleShortBlack || currentState.canCastleLongBlack;
        }

        //en passant splitttedFEN[3]

        if (splittedFEN[3].Equals("-"))
        {
            currentState.doublePushedPawnColor = Piece.Color.None;
            currentState.enPassantSquareIndex = 0;
        }
        else
        {
            int column = splittedFEN[3][0] - 'a'; //convert file letter to number
            int row = 0; //rank

            switch (splittedFEN[3][1])
            {
                case '3':
                    row = 4;
                    currentState.doublePushedPawnColor = Piece.Color.White;
                    break;
                case '6':
                    row = 3;
                    currentState.doublePushedPawnColor = Piece.Color.Black;
                    break;
            }

            currentState.enPassantSquareIndex = column + row * 8;
        }

    }

    public string GetFEN()
    {
        Dictionary<Piece.Type, char> symbolFromPieceType = new Dictionary<Piece.Type, char>()
        {
            { Piece.Type.Pawn, 'p' }, { Piece.Type.Knight, 'n' }, { Piece.Type.Bishop, 'b' },
            { Piece.Type.Rook, 'r' }, { Piece.Type.Queen , 'q' }, { Piece.Type.King  , 'k' }
        };

        StringBuilder fenString = new StringBuilder();

        //pieces
        for (int j = 0; j < 8; j++)
        {
            int emptyCounter = 0;

            for (int i = 0; i < 8; i++)
            {
                int index = i + j * 8;

                Piece piece = pieces[index];

                if (piece.type != Piece.Type.None)
                {
                    if (emptyCounter != 0)
                    {
                        fenString.Append(emptyCounter);
                    }

                    char pieceSymbol = piece.color == Piece.Color.White ? char.ToUpper(symbolFromPieceType[piece.type]) : symbolFromPieceType[piece.type];
                    fenString.Append(pieceSymbol);

                    emptyCounter = 0;
                }
                else
                {
                    emptyCounter++;
                }
            }

            if (emptyCounter != 0)
            {
                fenString.Append(emptyCounter);
            }

            if (j < 7)
            {
                fenString.Append('/');
            }
        }

        // turn color
        char turnColor = currentState.turnColor == Piece.Color.White ? 'w' : 'b';
        fenString.AppendFormat(" {0} ", turnColor);

        // castling rights

        if (currentState.canCastleWhite || currentState.canCastleBlack)
        {
            if (currentState.canCastleWhite)
            {
                if (currentState.canCastleShortWhite)
                {
                    fenString.Append('K');
                }

                if (currentState.canCastleLongWhite)
                {
                    fenString.Append('Q');
                }
            }

            if (currentState.canCastleBlack)
            {
                if (currentState.canCastleShortBlack)
                {
                    fenString.Append('k');
                }

                if (currentState.canCastleLongBlack)
                {
                    fenString.Append('q');
                }
            }
        }
        else
        {
            fenString.Append("-");
        }

        // en passant
        if (currentState.doublePushedPawnColor != Piece.Color.None)
        {
            char[] letters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
            int column = currentState.enPassantSquareIndex % 8;
            char columnLetter = letters[column];

            switch (currentState.doublePushedPawnColor)
            {
                case Piece.Color.White:
                    fenString.AppendFormat(" {0}{1}", columnLetter, 4);
                    break;
                case Piece.Color.Black:
                    fenString.AppendFormat(" {0}{1}", columnLetter, 6);
                    break;
            }
        }
        else
        {
            fenString.Append(" -");
        }

        return fenString.ToString();
    }


    public void MakeMove(Move move)
    {
        //push current state into the stack
        states.Push(currentState);

        // modify indicies list
        switch (move.pieceSource.color)
        {
            case Piece.Color.White:
                whitePiecesIndicies.Remove(move.squareSourceIndex); 
                whitePiecesIndicies.Add(move.squareTargetIndex);
                break;
            case Piece.Color.Black:
                blackPiecesIndicies.Remove(move.squareSourceIndex);
                blackPiecesIndicies.Add(move.squareTargetIndex);
                break;
        }

        switch (move.pieceTarget.color)
        {
            case Piece.Color.White:
                whitePiecesIndicies.Remove(move.squareTargetIndex);
                break;
            case Piece.Color.Black:
                blackPiecesIndicies.Remove(move.squareTargetIndex);
                break;
        }

        pieces[move.squareSourceIndex] = new Piece(); // remove piece at the "Source" square

        //move flags

        switch (move.flags)
        {
            case Move.Flags.DoublePush:
                currentState.doublePushedPawnColor = move.pieceSource.color;
                currentState.enPassantSquareIndex = move.squareTargetIndex;
                pieces[move.squareTargetIndex] = move.pieceSource;
                break;
            case Move.Flags.Promotion:
                currentState.doublePushedPawnColor = Piece.Color.None;
                pieces[move.squareTargetIndex] = new Piece { type = move.promotionPieceType, color = move.pieceSource.color }; //create piece of choosen type
                break;
            case Move.Flags.EnPassant:
                switch (currentState.doublePushedPawnColor)
                {
                    case Piece.Color.White:
                        whitePiecesIndicies.Remove(currentState.enPassantSquareIndex); 
                        break;
                    case Piece.Color.Black:
                        blackPiecesIndicies.Remove(currentState.enPassantSquareIndex);
                        break;
                }

                pieces[currentState.enPassantSquareIndex] = new Piece(); // remove piece
                currentState.doublePushedPawnColor = Piece.Color.None;
                pieces[move.squareTargetIndex] = move.pieceSource;

                break;

            case Move.Flags.CastleShort:
                currentState.doublePushedPawnColor = Piece.Color.None;
                pieces[move.squareSourceIndex] = new Piece(); // delete the piece at the place it was located before
                pieces[move.squareTargetIndex] = move.pieceSource; // add the piece at the ner location

                switch (move.pieceSource.color)
                {
                    case Piece.Color.White:
                        pieces[F1] = pieces[H1];
                        pieces[H1] = new Piece();

                        whitePiecesIndicies.Remove(H1);
                        whitePiecesIndicies.Add(F1);

                        break;
                    case Piece.Color.Black:
                        pieces[F8] = pieces[H8];
                        pieces[H8] = new Piece();

                        blackPiecesIndicies.Remove(H8);
                        blackPiecesIndicies.Add(F8);
                        break;

                }

                break;

            case Move.Flags.CastleLong:
                currentState.doublePushedPawnColor = Piece.Color.None;
                pieces[move.squareSourceIndex] = new Piece();
                pieces[move.squareTargetIndex] = move.pieceSource;

                switch (move.pieceSource.color)
                {
                    case Piece.Color.White:
                        pieces[D1] = pieces[A1];
                        pieces[A1] = new Piece();

                        whitePiecesIndicies.Remove(A1);
                        whitePiecesIndicies.Add(D1);

                        break;
                    case Piece.Color.Black:
                        pieces[D8] = pieces[A8];
                        pieces[A8] = new Piece();

                        blackPiecesIndicies.Remove(A8);
                        blackPiecesIndicies.Add(D8);

                        break;
                }

                break;

            default:
                currentState.doublePushedPawnColor = Piece.Color.None;
                //Might need to delete the squareSourceIndex piece // To check later
                pieces[move.squareTargetIndex] = move.pieceSource;
                break;
        }

        // check if the king or the towers moved 
        if (move.squareSourceIndex == E1 || move.squareTargetIndex == E1) // check if white king was moved
        {
            currentState.canCastleWhite = false;
        }
        else if (move.squareSourceIndex == E8 || move.squareTargetIndex == E8) // check if black king was moved
        {
            currentState.canCastleBlack = false;
        }

        if (move.squareSourceIndex == A1 || move.squareTargetIndex == A1) // white queen side rook
        {
            currentState.canCastleLongWhite = false;
        }
        else if (move.squareSourceIndex == H1 || move.squareTargetIndex == H1) //white king side rook
        {
            currentState.canCastleShortWhite = false;
        }

        if (move.squareSourceIndex == A8 || move.squareTargetIndex == A8) // black queen side rook
        {
            currentState.canCastleLongBlack = false;
        }
        else if (move.squareSourceIndex == H8 || move.squareTargetIndex == H8) // black king side rook
        {
            currentState.canCastleShortBlack = false;
        }

        // push the move into the stack

        moves.Push(move);

        // change turn color

        currentState.turnColor = currentState.turnColor == Piece.Color.White ? Piece.Color.Black : Piece.Color.White;

    }

    public void UndoMove()
    {
        if (states.Count > 0)
        {
            //get back to the last state
            currentState = states.Pop();

            // get the last move and undo it
            Move move = moves.Pop();

            //modify pieces indicies list
            switch (move.pieceSource.color)
            {
                case Piece.Color.White:
                    whitePiecesIndicies.Remove(move.squareTargetIndex);
                    whitePiecesIndicies.Add(move.squareSourceIndex);
                    break;
                case Piece.Color.Black:
                    blackPiecesIndicies.Remove(move.squareTargetIndex);
                    blackPiecesIndicies.Add(move.squareSourceIndex);
                    break;
            }

            //To check if bellow is needed  // highly doubteed 
            switch (move.pieceTarget.color)
            {
                case Piece.Color.White:
                    whitePiecesIndicies.Add(move.squareTargetIndex);
                    break;
                case Piece.Color.Black:
                    blackPiecesIndicies.Add(move.squareTargetIndex);
                    break;
            }

            //undo move
            pieces[move.squareSourceIndex] = move.pieceSource;
            pieces[move.squareTargetIndex] = move.pieceTarget;

            switch (move.flags)
            {
                case Move.Flags.CastleShort:
                    switch (move.pieceSource.color)
                    {
                        case Piece.Color.White:
                            pieces[F1] = new Piece();
                            pieces[H1] = new Piece { type = Piece.Type.Rook, color = Piece.Color.White };

                            whitePiecesIndicies.Remove(F1);
                            whitePiecesIndicies.Add(H1);

                            break;
                        case Piece.Color.Black:
                            pieces[F8] = new Piece();
                            pieces[H8] = new Piece { type = Piece.Type.Rook, color = Piece.Color.Black };

                            blackPiecesIndicies.Remove(F8);
                            blackPiecesIndicies.Add(H8);

                            break;
                    }
                    break;

                case Move.Flags.CastleLong:
                    switch (move.pieceSource.color)
                    {
                        case Piece.Color.White:
                            pieces[D1] = new Piece();
                            pieces[A1] = new Piece { type = Piece.Type.Rook, color = Piece.Color.White };

                            whitePiecesIndicies.Remove(D1);
                            whitePiecesIndicies.Add(A1);

                            break;
                        case Piece.Color.Black:
                            pieces[D8] = new Piece();
                            pieces[A8] = new Piece { type = Piece.Type.Rook, color = Piece.Color.Black };

                            blackPiecesIndicies.Remove(D8);
                            blackPiecesIndicies.Add(A8);

                            break;
                    }
                    break;

                case Move.Flags.EnPassant:
                    pieces[currentState.enPassantSquareIndex] = new Piece { type = Piece.Type.Pawn, color = currentState.doublePushedPawnColor };

                    switch (currentState.doublePushedPawnColor)
                    {
                        case Piece.Color.White:
                            whitePiecesIndicies.Add(currentState.enPassantSquareIndex);
                            break;
                        case Piece.Color.Black:
                            blackPiecesIndicies.Add(currentState.enPassantSquareIndex);
                            break;
                    }
                    break;
            }
        } 
    }

    //Undo moves and save them to the stack
    public void MoveBack(ref Move computerMove, ref Move playerMove)
    {
        computerMove = moves.Peek();
        undoneMoves.Push(computerMove);
        UndoMove();

        playerMove = moves.Peek();
        undoneMoves.Push(playerMove);
        UndoMove();
    }

    //Redo moves and delete them from the stack
    public void MoveForward(ref Move computerMove, ref Move playerMove)
    {
        playerMove = undoneMoves.Pop();
        MakeMove(playerMove);
        computerMove = undoneMoves.Pop();
        MakeMove(computerMove);
    }

    public void ClearUndoneMoves()
    {
        undoneMoves.Clear();
    }

}
