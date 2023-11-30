using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Board;

public class PieceLogic : MonoBehaviour
{

   /* public GameObject chesspiece;


    //Set the starting point for the FEN notation - top left corner
    private float xBoard = -3.5f;
    private float yBoard = 3.5f;

    //Set refernce for each chess piece sprite
    //Uppercase letters = white pieces, lowercase letters = black pieces
    //P - pawn, N - knight, B - bishop, R - rook, Q - queen, K - king
    public Sprite P, N, B, R, Q, K;
    public Sprite p, n, b, r, q, k;

    private BoardGraphics boardGraphics;

    public void Start()
    {
        string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";
        Board game = new Board();
        game.LoadFEN(StartFEN);
        Board.Piece piece = game.pieces[0];
        GameObject obj = Instantiate(chesspiece, new Vector3(0f,0f,-1f), Quaternion.identity);
    }

    public void Activate()
    {

        switch (this.name)
        {
            case "PawnBlack": this.GetComponent<SpriteRenderer>().sprite = p; break;
            case "PawnWhite": this.GetComponent<SpriteRenderer>().sprite = P; break;

            case "KnightBlack": this.GetComponent<SpriteRenderer>().sprite = n; break;
            case "KnightWhite": this.GetComponent<SpriteRenderer>().sprite = N; break;

            case "BishopBlack": this.GetComponent<SpriteRenderer>().sprite = b; break;
            case "BishopWhite": this.GetComponent<SpriteRenderer>().sprite = B; break;

            case "RookBlack": this.GetComponent<SpriteRenderer>().sprite = r; break;
            case "RookWhite": this.GetComponent<SpriteRenderer>().sprite = R; break;

            case "QueenBlack": this.GetComponent<SpriteRenderer>().sprite = q; break;
            case "QueenWhite": this.GetComponent<SpriteRenderer>().sprite = Q; break;

            case "KingBlack": this.GetComponent<SpriteRenderer>().sprite = k; break;
            case "KingWhite": this.GetComponent<SpriteRenderer>().sprite = K; break;
        }
    }*/

}
