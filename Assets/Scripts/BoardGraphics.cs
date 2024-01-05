using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using static Board;

public class BoardGraphics : MonoBehaviour
{
    //Set refernce for each chess piece sprite
    //Uppercase letters = white pieces, lowercase letters = black pieces
    //P - pawn, N - knight, B - bishop, R - rook, Q - queen, K - king
    public Sprite P, N, B, R, Q, K;
    public Sprite p, n, b, r, q, k;

    //public GameObject P, N, B, R, Q, K;
    //public GameObject p, n, b, r, q, k;
    public GameObject None;

    //For instantiating squares
    public GameObject lightTile;
    public GameObject darkTile;

    private bool isBoardFlipped = false;

    //Hint GameObject
    public GameObject hint;

    private Board board = new Board();

    [SerializeField]
    private GameObject[] chessPieces = new GameObject[64];
    private Sprite[] piecesSprites = new Sprite[64];
    private GameObject[] hints = new GameObject[64];

    // connect to board with graphics
    public void ConnectToBoard(Board board)
    {
        this.board = board;
    }

    // get piece sprite
    public Sprite GetPieceSprite(int index)
    {
        return piecesSprites[index];
    }

    // set color
    private void setColor(GameObject pieceTile)
    {
        if (pieceTile == null)
        {
            return;
        }

        Color lightColor = new Color(245.0f / 255.0f, 177.0f / 255.0f, 145.0f / 255.0f, 1.0f);
        Color darkColor = new Color(147 / 255.0f, 104.0f / 255.0f, 75.0f / 255.0f, 1.0f);

        int index;
        bool isInPlace = TryGetSquareIndexFromCoords(pieceTile.transform.position, out index);

        int rank = index % 8;
        int file = index / 8;

        if (isInPlace)
        {
            pieceTile.GetComponent<SpriteRenderer>().color = (file + rank) % 2 == 0 ? lightColor : darkColor;
        }

        pieceTile.tag = "Untagged";
    }

    //select piece
    public void SelectPiece(int index)
    {
        GameObject pieceTile = isBoardFlipped ? GameObject.Find($"{63-index}") : GameObject.Find($"{index}");
        pieceTile.GetComponent<SpriteRenderer>().color = new Color(149.0f / 255.0f, 130.0f / 255.0f, 0.0f, 1.0f);
        pieceTile.tag = "Selected";
    }


    public void DeselectPiece()
    {
        GameObject[] pieceTiles = GameObject.FindGameObjectsWithTag("Selected");

        foreach (GameObject pieceTile in pieceTiles)
        {
            setColor(pieceTile);
        }
    }

    //hilight hint
    public void HilightHint(int index)
    {
        GameObject pieceTile = isBoardFlipped ? GameObject.Find($"{63 - index}") : GameObject.Find($"{index}");
        pieceTile.GetComponent<SpriteRenderer>().color = new Color(149.0f / 255.0f, 130.0f / 255.0f, 0.0f, 1.0f);
        pieceTile.tag = "BestMove";
    }

    //deselect the hint
    public void DeselectHint()
    {
        GameObject[] pieceTiles = GameObject.FindGameObjectsWithTag("BestMove");

        foreach (GameObject pieceTile in pieceTiles)
        {
            setColor(pieceTile);
        }
    }

    //get square index 
    public bool TryGetSquareIndexFromCoords(Vector2 coords, out int squareIndex)
    {
        float xCords = coords.x;
        float yCords = coords.y;

        int rank = (int)Mathf.Round(Mathf.Abs(yCords - 3.5f));
        int file = (int)Mathf.Round(xCords + 3.5f);

        squareIndex = rank * 8 + file;

        if (isBoardFlipped)
        {
            squareIndex = 63 - squareIndex;
        }

        return (Mathf.Abs(xCords) < 4f && Mathf.Abs(yCords) < 4f);
    }

    // create the pieces sprites

    private void CreateGraphics()
    {
        int index;
        string name;
        //Iterate trough the chessboard and create the chess tiles and invisible hints
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                index = (rank * 8 + file);
                name = "" + index;
                if ((file + rank) % 2 == 0)
                {
                    GameObject tile = Instantiate(lightTile);
                    tile.transform.position = new Vector2(-3.5f + file, 3.5f - rank);
                    tile.name = name;
                }
                else
                {
                    GameObject tile = Instantiate(darkTile);
                    tile.transform.position = new Vector2(-3.5f + file, 3.5f - rank);
                    tile.name = name;
                }

                //Create the hint
                hints[index] = Instantiate(hint);
                hints[index].transform.position = new Vector2(-3.5f + file, 3.5f - rank);
                //Make hints transperent
                hints[index].GetComponent<SpriteRenderer>().color = new Color(0f,0f,0f,0f);
            }
        }


        //create GameObject for pieces sprites
        //Iterate trough the chessboard and create the chess pieces
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                index = (rank * 8 + file);
                chessPieces[index] = Instantiate(None);
                chessPieces[index].transform.position = new Vector3(-3.5f + file, 3.5f - rank, -1);
                chessPieces[index].AddComponent<SpriteRenderer>();
                piecesSprites[index] = chessPieces[index].GetComponent<SpriteRenderer>().sprite;
            }
        }
    }


    // set piece
    private void SetSpritePiece(SpriteRenderer chessPiece,Piece piece)
    {   

        switch (piece.type) 
        {
            case Piece.Type.None:
                chessPiece.sprite = null;
                break;
            case Piece.Type.Pawn:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = P;
                }
                else
                {
                    chessPiece.sprite = p;
                }
                break;
            case Piece.Type.Knight:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = N;
                }
                else
                {
                    chessPiece.sprite = n;
                }
                break;
            case Piece.Type.Bishop:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = B;
                }
                else
                {
                    chessPiece.sprite = b;
                }
                break;
            case Piece.Type.Rook:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = R;
                }
                else
                {
                    chessPiece.sprite = r;
                }
                break;
            case Piece.Type.Queen:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = Q;
                }
                else
                {
                    chessPiece.sprite = q;
                }
                break;
            case Piece.Type.King:
                if (piece.color == Piece.Color.White)
                {
                    chessPiece.sprite = K;
                }
                else
                {
                    chessPiece.sprite = k;
                }
                break;
        }
    }


    public void UpdateSprites()
    {
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0;file < 8; file++)
            {
                int index = rank * 8 + file;

                SpriteRenderer pieceSprite = chessPieces[index].GetComponent<SpriteRenderer>();
                Piece piece = isBoardFlipped ? board.GetPiece(63 - index) : board.GetPiece(index);
                SetSpritePiece(pieceSprite, piece);
            }
        }
    }

    public void SetHintMoves(List<Move> moves)
    {
        for (int i = 0; i < 64; i++)
        {
            //Make hints transperent
            hints[i].GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0f);
        }

        if (moves != null)
        {
            foreach (Move move in moves)
            {
                if (isBoardFlipped)
                {
                    hints[63 - move.squareTargetIndex].GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.5f);
                }
                else
                {
                    hints[move.squareTargetIndex].GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.5f);
                }
            }
        }
    }

    public void FlipBoard(bool flip)
    {
        isBoardFlipped = flip;
    }

    public bool IsBoardFlipped()
    {
        return isBoardFlipped;
    }


    public void AnimateMove(Move move, System.Action onAnimationCompleteCallback = null)
    {

        //Get the indicies of squares involved in move
        int squareSourceIndex = move.squareSourceIndex;
        int squareTargetIndex = move.squareTargetIndex;

        if (isBoardFlipped)
        {
            squareSourceIndex = 63 - squareSourceIndex;
            squareTargetIndex = 63 - squareTargetIndex;
        }

        //Get SpriteRenderer object of the pieces involved in the move
        SpriteRenderer pieceSourceSprite = chessPieces[squareSourceIndex].GetComponent<SpriteRenderer>();
        SpriteRenderer pieceTargetSprite = chessPieces[squareTargetIndex].GetComponent<SpriteRenderer>();

        //Create a twin of moving piece and make its original invisible 
        GameObject pieceMovement = Instantiate(chessPieces[squareSourceIndex]);
        pieceSourceSprite.sprite = null;

        //get the target object
        GameObject pieceTarget = chessPieces[squareTargetIndex];
        Vector3 targetPosition = pieceTarget.transform.position;

        
        StartCoroutine(AnimateTheMove(0.2f, pieceMovement, pieceTarget, () =>
        {
            // Execute the callback function if provided
            onAnimationCompleteCallback?.Invoke();
        }));

    }

    //make the same animation but backwards
    public void AnimateMoveBackward(Move move, System.Action onAnimationCompleteCallback = null)
    {
        //Get the indicies of squares involved in move
        int squareSourceIndex = move.squareTargetIndex;
        int squareTargetIndex = move.squareSourceIndex;

        if(isBoardFlipped)
        {
            squareSourceIndex = 63 - squareSourceIndex;
            squareTargetIndex = 63 - squareTargetIndex;
        }

        //Get SpriteRenderer object of the pieces involved in the move
        SpriteRenderer pieceSourceSprite = chessPieces[squareSourceIndex].GetComponent<SpriteRenderer>();
        SpriteRenderer pieceTargetSprite = chessPieces[squareTargetIndex].GetComponent<SpriteRenderer>();

        //Create a twin of moving piece and make its original invisible 
        GameObject pieceMovement = Instantiate(chessPieces[squareSourceIndex]);
        //pieceSourceSprite.sprite = null;
        SetSpritePiece(pieceSourceSprite, move.pieceTarget);

        //get the target object
        GameObject pieceTarget = chessPieces[squareTargetIndex];
        Vector3 targetPosition = pieceTarget.transform.position;


        StartCoroutine(AnimateTheMove(0.2f, pieceMovement, pieceTarget, () =>
        {
            // Execute the callback function if provided
            onAnimationCompleteCallback?.Invoke();
        }));
    }

    IEnumerator AnimateTheMove(float duration, GameObject pieceMovement, GameObject pieceTarget, System.Action ChangeSprite)
    {
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            pieceMovement.transform.position = Vector3.Lerp(
                pieceMovement.transform.position,
                pieceTarget.transform.position,
                t
            );

            timeElapsed += Time.deltaTime;
            //Change the sprite near the end of the animation for smoother transition
            if (t >= 0.9)
            {
                pieceTarget.GetComponent<SpriteRenderer>().sprite = pieceMovement.GetComponent<SpriteRenderer>().sprite;
            }
            yield return null;
        }

        //Ensure the piece ends up at the right position
        pieceMovement.transform.position = pieceTarget.transform.position;

        //make target piece same as piece movement
        Destroy(pieceMovement);


        ChangeSprite?.Invoke();

    }

    // Awake is called before Start()
    void Awake()
    {
        CreateGraphics();
    }

}