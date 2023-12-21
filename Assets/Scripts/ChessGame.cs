using System;
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

public class ChessGame : MonoBehaviour
{


    private enum GameState
    {
        UndoMove,
        RedoMove,
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
        public bool isHolding;
        public int squareIndex;
        public List<Move> legalMoves;
    }

    private struct MoveAnimation
    {
        public Move move;
        public bool dragged;
    }

    // for movin backwards
    Move computerMove = new Move();
    Move playerMove = new Move();

    private static readonly string checkmateString = "Checkmate!";
    private static readonly string drawString = "Draw!";

    //Made static so it does not change on scene reset
    private static int computerElo = 250;

    private bool startGame = false;

    private Board board = new Board();
    private BoardGraphics boardGraphics;

    private PieceSelection pieceSelection;
    private MoveAnimation moveAnimation;
    bool animationFinished = false;
    private static Piece.Color playerColor = Piece.Color.White;
    private Piece.Color playerColorChange = Piece.Color.White;
    private GameState gameState = GameState.PlayerMoving;

    // stockfish chess engine

    private bool engineRunning = true;
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

    //Add the Game Over screen to show when either of side won
    public GameOverScreen GameOverScreen;

    //select & deselect piece

    private void SelectPiece(int squareIndex, List<Move> legalMoves)
    {
        pieceSelection.isSelected = true;
        pieceSelection.isHolding = true;
        pieceSelection.squareIndex = squareIndex;
        pieceSelection.legalMoves = legalMoves;
    }

    private void DeselectPiece()
    {
        pieceSelection.isSelected = false;
        pieceSelection.isHolding = false;
    }

    // play as selected color with the selected fen

    public void PlayAsColor(string fen, Piece.Color color)
    {
        // validating the fen
        if (!FenValidator.IsFenStringValid(fen))
        {
            UnityEngine.Debug.Log(fen + " is not a valid fen string");
            return;
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

    public void SelectComputerELO(int elo)
    {
        engineConnector.LimitStrengthTo(elo);
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
                        moveAnimation.dragged = false;
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
        Debug.Log("Start 1");
        // precalculate moves
        MoveGeneration.PrecalculatedMoves();

        //get graphics
        boardGraphics = GameObject.Find("GameBoard").GetComponent < BoardGraphics >();

        boardGraphics.ConnectToBoard(board);

        // engine connector
        engineConnector.ConnectToEngine("Assets/ChessEngines/stockfish/stockfish.exe");
        engineConnector.LimitStrengthTo(computerElo);

        //load fen 4Q3/2B2Pp1/p5kp/P7/4q3/b1p4P/5PPK/4r3 w - -

        PlayAsColor(StartFEN, playerColor);

        // computer thread

        computerThread = new Thread(ComputerTurn);
        computerThread.Start();
        Debug.Log("Start 2");
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
            if (board.GetStateCount() > 0)
            {
                gameState = GameState.AnimateMoveBackward;            
            }
        }
    }
    //Redo move which had been undone before
    public void RedoMove()
    {
        if (board.GetUndoneMoveCount() > 0 && gameState != GameState.AnimateMoveForward && gameState != GameState.WaitingForAnimationToFinish)
        {
            gameState = GameState.AnimateMoveForward;
        }
    }

    public void GetHint()
    {
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

        boardGraphics.SelectPiece(move.squareSourceIndex);
        boardGraphics.SelectPiece(move.squareTargetIndex);
    }

    public void ChangeDifficulty(int val)
    {
        switch (val)
        {
            case 1:
                Debug.Log("Novice");
                computerElo = 250;
                break;
            case 2:
                Debug.Log("Beginner");
                computerElo = 400;
                break;
            case 3:
                Debug.Log("Intermediate");
                computerElo = 800;
                break;
            case 4:
                Debug.Log("Advanced");
                computerElo = 1400;
                break;
            case 5:
                Debug.Log("Expert");
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

                    //make the move

                    board.MakeMove(moveAnimation.move);
                    engineConnector.SendMove(moveAnimation.move);

                    // board graphics
                    boardGraphics.DeselectPieceSquare();
                    boardGraphics.DeselectSquare();
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

                        Debug.Log("Player Moves");
                    }
                    break;
                case GameState.AnimateMoveForward:
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
                    Debug.Log("Sprites Updated");
                    boardGraphics.DeselectPieceSquare();
                    //check if game ended
                    List<Move> avaiableMoves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor());
                    bool isKingInCheck = MoveGeneration.IsKingInCheck(board, board.GetTurnColor());
                    
                    if (avaiableMoves.Count <= 0)  //if there are no legal moves
                    {
                        if (isKingInCheck) // if king in check then it is checkmate
                        {
                            Debug.Log("Checkmate!");
                            
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
                            Debug.Log("Draw");
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
                                            moveAnimation.dragged = false;
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
                                            boardGraphics.SelectPieceSquare(squareIndex);
                                            boardGraphics.SetHintMoves(legalMoves);
                                        }
                                        else
                                        {
                                            //deselect the piece
                                            DeselectPiece();

                                            //remove the board hints and deselcet piece

                                            boardGraphics.DeselectPiece();
                                            boardGraphics.DeselectPieceSquare();
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
                                        boardGraphics.SelectPieceSquare(squareIndex);
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
                                boardGraphics.DeselectPieceSquare();
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
                case GameState.UndoMove:

                    //If some moves were already done
                    if (board.GetStateCount() > 0)
                    {
                        engineConnector.MoveBack();
                        board.MoveBack(ref computerMove, ref playerMove);

                        //gameState = GameState.AnimateMoveBackward;
                        gameState = GameState.AnimateMoveBackward;


                        //boardGraphics.UpdateSprites();
                        Debug.Log("Player Moves");
                    }

                    break;
            }

            mutex.ReleaseMutex();

        }
    }
}
