using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public Dictionary<Vector2, Tile> _tiles;

    private void Start()
    {
        _tiles = new Dictionary<Vector2, Tile>();
        IterateTiles();
    }

    private void IterateTiles()
    {
        int tileCount = transform.childCount;

        for (int i = 0; i < tileCount; i++)
        {
            Transform tileTransform = transform.GetChild(i);
            Tile tile = tileTransform.GetComponent<Tile>();
            _tiles[new Vector2(tile.coords.x, tile.coords.y)] = tile;

        }

/*        foreach (var kvp in _tiles)
        {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
        }*/

    }



}

/*
_tiles[new Vector2(x, y)] = spawnedTile;

public Tile GetTileAtPosition(Vector2 pos)
{
    if (_tiles.TryGetValue(pos, out var tile)) return tile;
    return null;
}
*/