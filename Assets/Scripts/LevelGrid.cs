using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public Dictionary<Vector2Int, Tile> dictionaryCoordsTiles;
    public List<Tile> tiles;

    private void Start()
    {
        //AddTilesToDictionary    
        dictionaryCoordsTiles = new Dictionary<Vector2Int, Tile>();
        int tileCount = transform.childCount;
        for (int i = 0; i < tileCount; i++)
        {
            Transform tileTransform = transform.GetChild(i);
            Tile tile = tileTransform.GetComponent<Tile>();
            dictionaryCoordsTiles[new Vector2Int(tile._tileCoords.x, tile._tileCoords.y)] = tile;
            tiles.Add(tile);
        }

    }

    public Tile GetTileAtPosition(Vector2Int pos)
    {
        if (dictionaryCoordsTiles.TryGetValue(pos, out var tile)) return tile;
        return null;
    }

    public bool CheckIfTileAtCoords(Vector2Int coords)
    {
        foreach (var kvp in dictionaryCoordsTiles)
        {
            if (coords == kvp.Key)
            {
                return true;
            }
        }
        return false;
    }


}

