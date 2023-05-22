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

public class ChessPiece : MonoBehaviour
{
    [SerializeField] public Vector2Int _positionCoordsCurrent;
    [SerializeField] private Vector2Int _positionCoordsStart;
    public ChessPieceType type;

    void Start()
    {
        _positionCoordsCurrent = _positionCoordsStart;
    }

    public virtual List<Vector2Int> GetValidMoves()
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        validMoves.Add(new Vector2Int (0,1));
        return validMoves;
    }

}
