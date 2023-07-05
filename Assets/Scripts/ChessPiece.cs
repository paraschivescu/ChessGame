using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

public enum ChessPieceFaction
{
    White,
    Zombie
}

public class ChessPiece : MonoBehaviour
{
    public Vector2Int positionCoordsCurrent;
    public ChessPieceType type;
    public ChessPieceFaction faction;
    public int _captureScore;
    public Tile _currentTile;

    void Start()
    {
        switch (type)
        {   case (ChessPieceType.Pawn):
                _captureScore = 10;
            break;
            case (ChessPieceType.Knight):
                _captureScore = 30;
            break;
            case (ChessPieceType.Bishop):
                _captureScore = 30;
            break;
            case (ChessPieceType.Rook):
                _captureScore = 50;
            break;
            case (ChessPieceType.Queen):
                _captureScore = 90;
            break;
            case (ChessPieceType.King):
                _captureScore = 1000;
            break;                
        }
    }

}

