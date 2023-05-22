using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private LevelGrid _levelGridReference;

    private void Start()
    {
        EventManager.TileClicked += OnTileClicked;
    }

    private void OnTileClicked(Tile tile)
    {
        Debug.Log("Tile clicked: " + tile.name);

        // get tiles that are valid destinations based on the chess piece's list of relative destinations (cp.GetValidMoves())

        // get the cp's current tile's coords
        Vector2Int cpCurrentCoords = _chessPiece._positionCoordsCurrent;
        List<Vector2Int> cpRelativeValidMoves = _chessPiece.GetValidMoves();
        List<Tile> cpAbsoluteValidMoves = new List<Tile>();
        foreach (Vector2Int coords in cpRelativeValidMoves) {
            cpAbsoluteValidMoves.Add(_levelGridReference.GetTileAtPosition(coords + cpCurrentCoords));
        }

        if (cpAbsoluteValidMoves.Contains(tile)) {
            _chessPiece._positionCoordsCurrent = tile.coords;
            Debug.Log(tile.coords);
            _chessPiece.transform.position = new Vector3(_chessPiece._positionCoordsCurrent.x, _chessPiece._positionCoordsCurrent.y, transform.position.z);
        }

    }

}
