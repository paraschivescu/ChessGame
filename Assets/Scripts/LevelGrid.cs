using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public Dictionary<Vector2, Tile> _tiles;

    private void Start()
    {
        //AddTilesToDictionary    
        _tiles = new Dictionary<Vector2, Tile>();
        int tileCount = transform.childCount;
        for (int i = 0; i < tileCount; i++)
        {
            Transform tileTransform = transform.GetChild(i);
            Tile tile = tileTransform.GetComponent<Tile>();
            _tiles[new Vector2(tile.coords.x, tile.coords.y)] = tile;
        }

    }

    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (_tiles.TryGetValue(pos, out var tile)) return tile;
        return null;
    }

    public bool checkIfTileAtCoords(Vector2 coords)
    {
        foreach (var kvp in _tiles)
        {
            if (coords == kvp.Key)
            {
                return true;
            }
        }
        return false;
    }


}

