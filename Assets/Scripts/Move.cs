using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public ChessPiece chessPieceToMove;
    public Tile tileDestination;
    public int scoreOfMove;
    public int scoreOverwatch;
    public int scoreCapture;
    public int scoreThreat;
    public bool ownMove = true;
}
