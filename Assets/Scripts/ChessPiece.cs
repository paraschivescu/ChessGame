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
}
