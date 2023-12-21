using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

using static Board;

public class EngineConnector
{

    //engine process related
    private Process engineProcess = new Process();
    private StreamReader engineProcessStdOut;
    private StreamWriter engineProcessStdIn;

    //list with all the moves

    private List<string> moves = new List<string>();

    //list of removed moves
    private Stack<string> removedMoves = new Stack<string>();

    // fen string, move time

    private string fenString = StartFEN;
    public int MoveTime = 1000; // ms

    //for multithreading

    private System.Threading.Mutex mutex = new System.Threading.Mutex();

    //connect to the chess engine

    public void ConnectToEngine(string enginePath)
    {
        //engine process start info

        engineProcess.StartInfo.FileName = enginePath;
        engineProcess.StartInfo.UseShellExecute = false;
        engineProcess.StartInfo.RedirectStandardOutput = true;
        engineProcess.StartInfo.RedirectStandardInput = true;
        engineProcess.StartInfo.CreateNoWindow = true;

        // start engine process

        try
        {
            engineProcess.Start();

            //set std input and output of the child process

            engineProcessStdOut = engineProcess.StandardOutput;
            engineProcessStdIn = engineProcess.StandardInput;

            UnityEngine.Debug.Log("Connected to engine: " + enginePath);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e.Message);
        }
    }

    public void Disconnect()
    {
        // send quit to engine and wait for the exit

        mutex.WaitOne();
        {
            engineProcessStdIn.WriteLine("quit");
        }
        mutex.ReleaseMutex();

        engineProcess.WaitForExit();
    }

    public void LimitStrengthTo(int eloValue)
    {
        mutex.WaitOne();
        {
            if (eloValue != int.MaxValue)
            {
                string command = string.Format("setoption name UCI_LimitStrength value true\nsetoption name UCI_Elo value {0}\n", eloValue);
                UnityEngine.Debug.Log(command);
                engineProcessStdIn.WriteLine(command);
            }
            else
            {
                engineProcessStdIn.WriteLine("setoption name UCI_LimitStrength value false\n");
            }
        }
        mutex.ReleaseMutex();
    }

    public void StopCalculating()
    {
        mutex.WaitOne();
        {
            engineProcessStdIn.WriteLine("stop");
        }
        mutex.ReleaseMutex();
    }

    public void LoadFEN(string fen)
    {
        fenString = fen;
        moves.Clear();
    }

    private string FromMoveToString(Move move)
    {
        // notation

        Dictionary<Piece.Type, char> symbolFromPieceType = new Dictionary<Piece.Type, char>()
        {
            { Piece.Type.Pawn, 'p' }, { Piece.Type.Knight, 'n' }, { Piece.Type.Bishop, 'b' },
            { Piece.Type.Rook, 'r' }, { Piece.Type.Queen , 'q' }, { Piece.Type.King  , 'k' }
        };

        char[] letters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

        // get squares

        int squareSourceI = move.squareSourceIndex % 8;
        int squareSourceJ = move.squareSourceIndex / 8;
        int squareTargetI = move.squareTargetIndex % 8;
        int squareTargetJ = move.squareTargetIndex / 8;

        //str move

        string strMove = string.Format("{0}{1}{2}{3}", letters[squareSourceI], 8 - squareSourceJ, letters[squareTargetI], 8 - squareTargetJ);

        // if needs to add the promotion piece

        if (move.flags == Move.Flags.Promotion)
        {
            strMove += symbolFromPieceType[move.promotionPieceType];
        }

        // return the move

        return strMove;
    }

    private Move FromStringToMove(Board board, string strMove)
    {
        Dictionary<char, Piece.Type> pieceTypeFromSymbol = new Dictionary<char, Piece.Type>()
        {
            { 'p', Piece.Type.Pawn }, { 'n', Piece.Type.Knight }, { 'b', Piece.Type.Bishop },
            { 'r', Piece.Type.Rook }, { 'q', Piece.Type.Queen  }, { 'k', Piece.Type.King   }
        };

        // source tile

        int squareSourceI = strMove[0] - 'a';
        int squareSourceJ = 7 - (strMove[1] - '1');
        int squareSourceIndex = squareSourceI + squareSourceJ * 8;

        // target tile

        int squareTargetI = strMove[2] - 'a';
        int squareTargetJ = 7 - (strMove[3] - '1');
        int squareTargetIndex = squareTargetI + squareTargetJ * 8;

        // get moves

        List<Move> moves = MoveGeneration.GetPseudoLegalMoves(board, squareSourceIndex); // Assumption that the engine wont chose a non legal move
        Move chosenMove = new Move();

        foreach (Move move in moves)
        {
            if (move.squareTargetIndex == squareTargetIndex)
            {
                chosenMove = move;
                break;
            }
        }
        
        if (chosenMove.flags == Move.Flags.Promotion)
        {
            chosenMove.promotionPieceType = pieceTypeFromSymbol[strMove[4]];
        }

        return chosenMove;

    }

    public void SendMove(Move move)
    {
        string strMove = FromMoveToString(move);
        moves.Add(strMove);
        UnityEngine.Debug.Log("Move: " + strMove);
        UnityEngine.Debug.Log("Moves: " + moves);
    }

    public void MoveBack()
    {
        if (moves.Count > 0)
        {
            removedMoves.Push(moves[moves.Count - 1]);
            moves.RemoveAt(moves.Count - 1);

            removedMoves.Push(moves[moves.Count - 1]);
            moves.RemoveAt(moves.Count - 1);
        }
    }

    public void MoveForward()
    {
        if(removedMoves.Count > 0)
        {
            moves.Add(removedMoves.Pop());
            moves.Add(removedMoves.Pop());
        }
    }

    public Move GetBestMove(Board board)
    {
        //construct the moves string

        StringBuilder command = new StringBuilder();

        command.AppendFormat("position fen {0} moves", fenString);

        foreach (string strMove in moves)
        {
            command.AppendFormat(" {0}", strMove);
        }

        command.AppendFormat("\ngo movetime {0}\n", MoveTime);

        // write to the engine process std in
        UnityEngine.Debug.Log(command);
        mutex.WaitOne();
        {
            engineProcessStdIn.Write(command);
        }
        mutex.ReleaseMutex();

        // read the output from the engine until it found the move

        bool moveFound = false;
        string bestMoveString = null;

        do
        {
            string engineOutputLine = engineProcessStdOut.ReadLine();

            if (engineOutputLine.Contains("bestmove"))
            {
                string[] bestMoveLine = engineOutputLine.Split(' ');
                bestMoveString = bestMoveLine[1];
                moveFound = true;
            }
        } while (!moveFound);

        // from string to actual move
        UnityEngine.Debug.Log("Best move is " +  bestMoveString);
        Move bestMove = FromStringToMove(board, bestMoveString);
        UnityEngine.Debug.Log("Best move is from " + bestMove.squareSourceIndex + " to " + bestMove.squareTargetIndex);
        return bestMove;

    }

    public string[] GetAnalysis(string fenString)
    {
        UnityEngine.Debug.Log("Start Analysis");
        //construct the analysis string
        StringBuilder command = new StringBuilder();

        command.AppendFormat("position fen {0}", fenString);
        command.Append("\ngo depth 20\n");

        UnityEngine.Debug.Log(command);

        mutex.WaitOne();
        {
            engineProcessStdIn.Write(command);
        }
        mutex.ReleaseMutex();

        UnityEngine.Debug.Log("Read Analysis");

        //to store each line of enigne output
        string output;
        //to store full output
        List<string> fullOutput = new List<string>();
        //string[] fullOutput;
        bool moveFound = false;

        do
        {
            output = engineProcessStdOut.ReadLine();
            fullOutput.Add(output);
            UnityEngine.Debug.Log(output);
            if (output.Contains("bestmove"))
            { 
                moveFound = true;
            }
        } while (!moveFound);

        return fullOutput.ToArray();
    }
}
