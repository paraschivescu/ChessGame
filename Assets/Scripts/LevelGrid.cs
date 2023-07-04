using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public Dictionary<Vector2Int, Tile> _tiles;

    private void Start()
    {
        //AddTilesToDictionary    
        _tiles = new Dictionary<Vector2Int, Tile>();
        int tileCount = transform.childCount;
        for (int i = 0; i < tileCount; i++)
        {
            Transform tileTransform = transform.GetChild(i);
            Tile tile = tileTransform.GetComponent<Tile>();
            _tiles[new Vector2Int(tile._tileCoords.x, tile._tileCoords.y)] = tile;
        }

    }

    public Tile GetTileAtPosition(Vector2Int pos)
    {
        if (_tiles.TryGetValue(pos, out var tile)) return tile;
        return null;
    }

    public bool CheckIfTileAtCoords(Vector2Int coords)
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

