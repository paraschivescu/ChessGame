using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public ChessPiece chessPieceToMove;
    public Tile tileDestination;
    public int scoreOfMove = 0;
    public bool ownMove = true;
    public List<Move> subsequentMoves;
}
