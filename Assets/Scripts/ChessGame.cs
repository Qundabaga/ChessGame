using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static Board;
using System.Text;
using System.Linq;

public class ChessGame : MonoBehaviour
{


    private enum GameState
    {
        AnimateMove,
        AnimateMoveBackward,
        AnimateMoveForward,
        WaitingForAnimationToFinish,
        AnimationJustFinished,
        WaitingForComputer,
        PlayerMoving,
        GameEnd
    }

    private struct PieceSelection
    {
        public bool isSelected;
        public int squareIndex;
        public List<Move> legalMoves;
    }

    private struct MoveAnimation
    {
        public Move move;
    }

    // for movin backwards
    Move computerMove = new Move();
    Move playerMove = new Move();

    //Made static so it does not change on scene reset
    private static int computerElo = 250;

    private Board board = new Board();
    private BoardGraphics boardGraphics;

    private static string selectedFEN = StartFEN;

    private PieceSelection pieceSelection;
    private MoveAnimation moveAnimation;
    bool animationFinished = false;
    private static Piece.Color playerColor = Piece.Color.White;
    private Piece.Color playerColorChange = Piece.Color.White;
    private GameState gameState = GameState.PlayerMoving;

    // stockfish chess engine

    //private bool engineRunning = true;
    private EngineConnector engineConnector = new EngineConnector();
    private Thread computerThread;
    private System.Threading.Semaphore computerThreadBarrier = new System.Threading.Semaphore(0, 1);
    private System.Threading.Mutex mutex = new System.Threading.Mutex();
    private System.Threading.Mutex engineConnectorMutex = new System.Threading.Mutex();

    //Create text box to save the moves there
    public TMP_Text moveList;

    //save the digits and letters of the board to change them if black is played
    public TMP_Text letter_1, letter_2, letter_3, letter_4, letter_5, letter_6, letter_7, letter_8;
    public TMP_Text digit_1, digit_2, digit_3, digit_4, digit_5, digit_6, digit_7, digit_8;

    // Get the text of FEN input field
    public TMP_InputField fenInput;

    //Add the Game Over screen to show when either of side won
    public GameOverScreen GameOverScreen;

    //select & deselect piece

    private void SelectPiece(int squareIndex, List<Move> legalMoves)
    {
        pieceSelection.isSelected = true;
        pieceSelection.squareIndex = squareIndex;
        pieceSelection.legalMoves = legalMoves;
    }

    private void DeselectPiece()
    {
        pieceSelection.isSelected = false;
    }

    // play as selected color with the selected fen

    public void PlayAsColor(string fen, Piece.Color color)
    {
        Debug.Log("FEN -" + fen);

        if (fen != StartFEN)
        {
            // validating the fen
            if (!FenValidator.IsFenStringValid(fen))
            {
                UnityEngine.Debug.Log(fen + " is not a valid fen string");
                // change to standart fen if providet fen is not valid
                fen = StartFEN;
            }

            color = fen.Split(' ')[1] == "w" ? Piece.Color.White : Piece.Color.Black;
        }
        // send stop to engine
        engineConnector.StopCalculating();

        // change from white to black
        mutex.WaitOne();
        {
            //change human color
            playerColor = color;

            //load fen
            board.LoadFEN(fen);

            // Check if board created by fen is valid in terms of chess rules

            if(MoveGeneration.IsKingInCheck(board, Piece.Color.White) || MoveGeneration.IsKingInCheck(board, Piece.Color.Black))
            {
                // change to standart fen if providet fen is not valid
                fen = StartFEN;

                //load fen again
                board.LoadFEN(fen);
            }




            engineConnectorMutex.WaitOne();
            {
                engineConnector.LoadFEN(fen);
            }
            engineConnectorMutex.ReleaseMutex();


            //update board ui
            DeselectPiece();
            boardGraphics.FlipBoard(color == Piece.Color.Black);
            boardGraphics.UpdateSprites();

            // change game state
            gameState = GameState.PlayerMoving;
        }
        mutex.ReleaseMutex();

        //set the digits and numbers on the board based on the color
        switch (color)
        {
            case Piece.Color.White:
                letter_1.text = "a";
                letter_2.text = "b";
                letter_3.text = "c";
                letter_4.text = "d";
                letter_5.text = "e";
                letter_6.text = "f";
                letter_7.text = "g";
                letter_8.text = "h";

                digit_1.text = "1";
                digit_2.text = "2";
                digit_3.text = "3";
                digit_4.text = "4";
                digit_5.text = "5";
                digit_6.text = "6";
                digit_7.text = "7";
                digit_8.text = "8";
                break;
            case Piece.Color.Black:
                letter_1.text = "h";
                letter_2.text = "g";
                letter_3.text = "f";
                letter_4.text = "e";
                letter_5.text = "d";
                letter_6.text = "c";
                letter_7.text = "b";
                letter_8.text = "a";

                digit_1.text = "8";
                digit_2.text = "7";
                digit_3.text = "6";
                digit_4.text = "5";
                digit_5.text = "4";
                digit_6.text = "3";
                digit_7.text = "2";
                digit_8.text = "1";
                break;
        }

    }

    public void SelectPromotionPieceType(Piece.Type type)
    {
        mutex.WaitOne();
        {
            board.PromotionPieceType = type;
        }
        mutex.ReleaseMutex();
    }

    public string GetFEN()
    {
        string fen;

        mutex.WaitOne();
        {
            fen = board.GetFEN();
        }
        mutex.ReleaseMutex();

        return fen;
    }

    private void ComputerTurn()
    {
        Board boardCopy = new Board();

        while (true)
        {
            // whait until computer turn
            computerThreadBarrier.WaitOne();

            // check if in the middle of the move calculation a button that aborts this current operation is pressed
            bool aborted;

            //perform a copy of current state of the board
            mutex.WaitOne();
            {
                aborted = gameState != GameState.WaitingForComputer;

                if (!aborted)
                {
                    board.CopyBoardState(boardCopy);
                }
            }
            mutex.ReleaseMutex();

            if (!aborted)
            {
                // get the chosen move using the copy of the board

                Move move;

                engineConnectorMutex.WaitOne();
                {
                    move = engineConnector.GetBestMove(boardCopy);
                }
                engineConnectorMutex.ReleaseMutex();

                mutex.WaitOne();
                {
                    aborted = gameState != GameState.WaitingForComputer;

                    if (!aborted)
                    {
                        moveAnimation.move = move;
                        gameState = GameState.AnimateMove;
                    }
                }
                mutex.ReleaseMutex();

            }
        }
    }




    // Start is called before the first frame update
    void Start()
    {
        // precalculate moves
        MoveGeneration.PrecalculatedMoves();

        //get graphics
        boardGraphics = GameObject.Find("GameBoard").GetComponent < BoardGraphics >();

        boardGraphics.ConnectToBoard(board);

        // engine connector
        engineConnector.ConnectToEngine("Assets/ChessEngines/stockfish/stockfish.exe");
        engineConnector.LimitStrengthTo(computerElo);

        PlayAsColor(selectedFEN, playerColor);
        selectedFEN = StartFEN;


        // computer thread

        computerThread = new Thread(ComputerTurn);
        computerThread.Start();
    }

    public void StartGame()
    {
        // Wait for the thread to finish
        computerThread.Abort();

        //change the player color
        playerColor = playerColorChange;

        //disconnect from the Stockfish Chess Engine
        engineConnector.Disconnect();

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //Undo moves using undo button
    public void UndoMove()
    {
        if (gameState == GameState.PlayerMoving)
        {
            if (board.GetStateCount() > 1)
            {
                gameState = GameState.AnimateMoveBackward;            
            }
        }
    }
    //Redo move which had been undone before
    public void RedoMove()
    {
        if (board.GetUndoneMoveCount() > 0 && gameState == GameState.PlayerMoving)
        {
            gameState = GameState.AnimateMoveForward;
        }
    }

    public void GetHint()
    {
        // hide previous hint
        boardGraphics.DeselectHint();

        // copy board
        Board boardCopy = new Board();
        board.CopyBoardState(boardCopy);


        // get the chosen move using th copy of the board

        Move move;

        engineConnectorMutex.WaitOne();
        {
            move = engineConnector.GetBestMove(boardCopy);
        }
        engineConnectorMutex.ReleaseMutex();

        boardGraphics.HilightHint(move.squareSourceIndex);
        boardGraphics.HilightHint(move.squareTargetIndex);
    }

    public void ChangeDifficulty(int val)
    {
        switch (val)
        {
            case 1:
                computerElo = 250;
                break;
            case 2:
                computerElo = 400;
                break;
            case 3:
                computerElo = 800;
                break;
            case 4:
                computerElo = 1400;
                break;
            case 5:
                computerElo = 1800;
                break;
        }
    }

    public void ChangeColor(int val)
    {
        switch (val)
        {
            case 0:
                playerColorChange = Piece.Color.White;
                break;
            case 1:
                playerColorChange = Piece.Color.Black;
                break;
        }
    }

    public void GetInfo()
    {
        moveList.text = "";
        string[] analysisOutput = engineConnector.GetAnalysis(board.GetFEN());

        foreach (string output in  analysisOutput)
        {
            if(output.Contains("info depth 18 seldepth") || output.Contains("info depth 19 seldepth") || output.Contains("info depth 20 seldepth"))
            {
                string[] line = output.Split(' ');
                StringBuilder analysis = new StringBuilder();

                analysis.AppendFormat("score cp {0} ", int.Parse(line[9]) > 0 ? "+" + line[9] : line[9]);
                analysis.AppendLine(string.Join(" ", line, 21,line.Length - 21));

                moveList.text += analysis + "\n";
            }
        }

        moveList.text += analysisOutput[analysisOutput.Length - 1];  
    }

    public void GetFenInput(string fen)
    {
        Debug.Log(fen);
        selectedFEN = fen;
    }


    public void ReadRandomFenFromFile()
    {
        // Get random number from 1 to 3000000
        System.Random rand = new System.Random();
        int randomRow = rand.Next(1, 3000000);

        // get the fen string at the random row
        string fenString;

        using (var sr = new StreamReader("Assets/unique04.fen"))
        {
            for (int i = 1; i < randomRow; i++)
            {
                sr.ReadLine();
            }

            fenString = sr.ReadLine();
        }

        // Get only BoardPosition, color, castling, enPassant parts
        fenInput.text = string.Join(" ", fenString.Split().Take(4));
    }


    // Update is called once per frame
    void Update()
    {
        mutex.WaitOne();
        {
            switch (gameState)
            {
                case GameState.AnimateMove:
                    //Clean the undone move stack if new move is done
                    board.ClearUndoneMoves();

                    // hide the best move hint
                    boardGraphics.DeselectHint();

                    //make the move

                    board.MakeMove(moveAnimation.move);
                    engineConnector.SendMove(moveAnimation.move);

                    // board graphics
                    boardGraphics.SetHintMoves(null);

                    animationFinished = false;

                    //true means that it is a standard move animation
                    boardGraphics.AnimateMove(moveAnimation.move, () => {
                        gameState = GameState.AnimationJustFinished;
                        animationFinished = true;
                    });

                    if (animationFinished == false)
                    {
                        gameState = GameState.WaitingForAnimationToFinish;
                    }
                    break;
                //For move back button
                case GameState.AnimateMoveBackward:

                    // hide the best move hint
                    boardGraphics.DeselectHint();

                    //If some moves were already done
                    if (board.GetStateCount() > 0)
                    {
                        engineConnector.MoveBack();
                        board.MoveBack(ref computerMove, ref playerMove);

                        animationFinished = false;

                        //Make two moves with backward animation
                        boardGraphics.AnimateMoveBackward(computerMove, () => {

                            boardGraphics.AnimateMoveBackward(playerMove, () => {
                                boardGraphics.UpdateSprites();
                                gameState = GameState.PlayerMoving;
                                animationFinished = true;
                            });
                        });
                        
                        if (animationFinished == false)
                        {
                            gameState = GameState.WaitingForAnimationToFinish;
                        }
                    }
                    break;
                case GameState.AnimateMoveForward:

                    // hide the best move hint
                    boardGraphics.DeselectHint();

                    if (board.GetUndoneMoveCount() > 0)
                    {
                        engineConnector.MoveForward();
                        board.MoveForward(ref computerMove, ref playerMove);

                        animationFinished = false;

                        //Make two moves again
                        boardGraphics.AnimateMove(playerMove, () =>
                        {

                            boardGraphics.AnimateMove(computerMove, () =>
                            {
                                boardGraphics.UpdateSprites();
                                gameState = GameState.PlayerMoving;
                                animationFinished = true;
                            });
                        });

                        if (animationFinished == false)
                        {
                            gameState = GameState.WaitingForAnimationToFinish;
                        }

                    }
                    break;
                case GameState.AnimationJustFinished:
                    //clean analysis output
                    moveList.text = "";

                    //update sprites
                    boardGraphics.UpdateSprites();
                    //check if game ended
                    List<Move> avaiableMoves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor());
                    bool isKingInCheck = MoveGeneration.IsKingInCheck(board, board.GetTurnColor());
                    
                    if (avaiableMoves.Count <= 0)  //if there are no legal moves
                    {
                        if (isKingInCheck) // if king in check then it is checkmate
                        {
                            if(board.GetTurnColor() == Piece.Color.White)
                            {
                                GameOverScreen.Message("Checkmate!\nBlack wins.");
                            }
                            else
                            {
                                GameOverScreen.Message("Checkmate!\nWhite wins.");
                            }
                        }
                        else
                        {
                            GameOverScreen.Message("Draw!");
                        }

                        gameState = GameState.GameEnd;
                    }
                    else if(isKingInCheck)
                    {
                        gameState = GameState.PlayerMoving;
                    }
                    else
                    {
                        gameState = GameState.PlayerMoving;
                    }
                    break;
                case GameState.PlayerMoving:
                    // get turn color 
                    
                    Piece.Color turnColor = board.GetTurnColor();

                    if (turnColor == playerColor)
                    {
                        //human turn

                        //get mouse position relative to camera
                        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                        // get the square at mouse position and calculate its index
                        bool isSquareInBoard = boardGraphics.TryGetSquareIndexFromCoords(mousePosition, out int squareIndex);

                        // select & deselect piece & make moves
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (isSquareInBoard)
                            {
                                if(pieceSelection.isSelected)
                                {
                                    //check if it is an legal move
                                    bool isMoveLegal = false;

                                    foreach (Move move in pieceSelection.legalMoves)
                                    {
                                        if (move.squareTargetIndex == squareIndex)
                                        {
                                            isMoveLegal = true;

                                            //deselect Piece
                                            DeselectPiece();
                                            boardGraphics.DeselectPiece();

                                            //animate move
                                            moveAnimation.move = move;
                                            gameState = GameState.AnimateMove;

                                            break;
                                        }
                                    }

                                    if (!isMoveLegal)
                                    {
                                        // check if other piece is selected
                                        Piece piece = board.GetPiece(squareIndex);

                                        if(piece.color == playerColor)
                                        {
                                            boardGraphics.DeselectPiece();

                                            //get legal moves for the piece
                                            List<Move> legalMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                                            //select the piece
                                            SelectPiece(squareIndex, legalMoves);

                                            // board select the piece
                                            boardGraphics.SelectPiece(squareIndex);
                                            boardGraphics.SetHintMoves(legalMoves);
                                        }
                                        else
                                        {
                                            //deselect the piece
                                            DeselectPiece();

                                            //remove the board hints and deselcet piece

                                            boardGraphics.DeselectPiece();
                                            boardGraphics.SetHintMoves(null);
                                        }
                                    }
                                }
                                else
                                {
                                    boardGraphics.DeselectPiece();
                                    Piece piece = board.GetPiece(squareIndex);

                                    if (piece.color == playerColor)
                                    {

                                        //get legal moves for the 
                                        List<Move> legalMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                                        //Select the piece
                                        SelectPiece(squareIndex , legalMoves);

                                        //board select piece
                                        boardGraphics.SelectPiece(squareIndex);
                                        boardGraphics.SetHintMoves(legalMoves);
                                    }
                                }
                            }
                            else
                            {
                                //deselect the piece
                                DeselectPiece();

                                // remove the board hints and deselect piece
                                boardGraphics.DeselectPiece();
                                boardGraphics.SetHintMoves(null);
                            }
                        }
                    }
                    else
                    {
                        //computer turn

                        gameState = GameState.WaitingForComputer;
                        computerThreadBarrier.Release();
                    }

                    break;
            }

            mutex.ReleaseMutex();

        }
    }
}