using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [SerializeField] private Vector2 _positionCoordsCurrent;
    [SerializeField] private Vector2 _positionCoordsStart;
    [SerializeField] private LevelGrid _levelGridReference;

    void Start()
    {
        _positionCoordsCurrent = _positionCoordsStart;
    }

    void MovePiece()
    {

              

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            // when pressing key, check if there is a tile at the destination coords
            Vector2 newCoords = new Vector2(_positionCoordsCurrent.x, _positionCoordsCurrent.y+1);
            if (checkIfTileAtCoords(newCoords)) 
            {
                // if yes, move piece there: 
                // 1. first, update _positionCoordsCurrent
                _positionCoordsCurrent = newCoords;
                Debug.Log(newCoords);
            } else {
                Debug.Log("No tile at new coords");
            }
        }
        // 2. then, move the actual prefab to the coords of the tile residing at the destination coords
        transform.position = new Vector3(_positionCoordsCurrent.x, _positionCoordsCurrent.y, transform.position.z);
    }

    bool checkIfTileAtCoords(Vector2 coords)
    {
        foreach (var kvp in _levelGridReference._tiles)
        {
            if (coords == kvp.Key) {
                return true;
            }
        }
        return false;
    }
    
}
