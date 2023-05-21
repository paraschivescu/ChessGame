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
        MovePieceTo(_chessPiece, tile.coords);
    }

    void MovePieceTo(ChessPiece cp, Vector2 newCoords)
    {
        if (_levelGridReference.checkIfTileAtCoords(newCoords))
        {
            cp._positionCoordsCurrent = newCoords;
            Debug.Log(newCoords);
        }
        else
        {
            Debug.Log("No tile at new coords");
        }
        cp.transform.position = new Vector3(cp._positionCoordsCurrent.x, cp._positionCoordsCurrent.y, transform.position.z);
    }


}
