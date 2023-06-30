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
    public Vector2Int _positionCoordsCurrent;
    public ChessPieceType type;
    public ChessPieceFaction faction;

    void Start()
    {

    }

   
    public virtual List<Tile> GetValidMoves(LevelGrid _levelGridReference)
    {
        List<Tile> AddValidMoves(List<Vector2Int> relativeValidMoves, LevelGrid _levelGridReference, bool continuousLineChessPiece)
        {
            List<Tile> moves = new List<Tile>();
            foreach (Vector2Int coords in relativeValidMoves) {
                Tile t = _levelGridReference.GetTileAtPosition(coords + _positionCoordsCurrent);
                if (t) {
                    moves.Add(t);
                }
                else {
                    if (continuousLineChessPiece) return moves;
                }
            }

            return moves;
        }

        List<Vector2Int> relativeValidMoves = new List<Vector2Int>();
        List<Tile> absoluteValidMoves = new List<Tile>();
        switch (type) {
            case(ChessPieceType.Pawn):
                relativeValidMoves.Add(new Vector2Int(0, 1));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMoves, _levelGridReference, false));
            break;

            case (ChessPieceType.Knight):
                relativeValidMoves.Add(new Vector2Int(1, 2));
                relativeValidMoves.Add(new Vector2Int(2, 1));
                relativeValidMoves.Add(new Vector2Int(2, -1));
                relativeValidMoves.Add(new Vector2Int(1, -2));
                relativeValidMoves.Add(new Vector2Int(-1, 2));
                relativeValidMoves.Add(new Vector2Int(-2, 1));
                relativeValidMoves.Add(new Vector2Int(-2, -1));
                relativeValidMoves.Add(new Vector2Int(-1, -2));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMoves, _levelGridReference, false));
            break;

            case (ChessPieceType.King):
                relativeValidMoves.Add(new Vector2Int(1,0));
                relativeValidMoves.Add(new Vector2Int(-1,0));
                relativeValidMoves.Add(new Vector2Int(0, 1));
                relativeValidMoves.Add(new Vector2Int(0, -1));
                relativeValidMoves.Add(new Vector2Int(-1, -1));
                relativeValidMoves.Add(new Vector2Int(1, 1));
                relativeValidMoves.Add(new Vector2Int(1, -1));
                relativeValidMoves.Add(new Vector2Int(-1, 1));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMoves, _levelGridReference, false));
            break;

            case (ChessPieceType.Rook):
                List<Vector2Int> relativeValidMovesLeft = new List<Vector2Int>(), relativeValidMovesRight = new List<Vector2Int>(), relativeValidMovesUp = new List<Vector2Int>(), relativeValidMovesDown = new List<Vector2Int>();
                for (int i = 1; i < 10; i++) {
                    relativeValidMovesUp.Add(new Vector2Int(0, i));
                    relativeValidMovesDown.Add(new Vector2Int(0, -i));
                    relativeValidMovesRight.Add(new Vector2Int(i, 0));
                    relativeValidMovesLeft.Add(new Vector2Int(-i, 0));
                }
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMovesUp, _levelGridReference, true));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMovesDown, _levelGridReference, true));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMovesLeft, _levelGridReference, true));
                absoluteValidMoves.AddRange(AddValidMoves(relativeValidMovesRight, _levelGridReference, true));
            break;

            default:
                Debug.Log("Trying to get valid moves of invalid chess piece type.");
            break;
        }

        // need to remove tiles across gaps for every cp that can move in a line, other than Knights: Queen, Rooks, Bishops

        return absoluteValidMoves;
    }


}
