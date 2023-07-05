using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public Transform chessPiecesParent;
    public Dictionary<Vector2Int, Tile> dictionaryCoordsTiles;
    public List<Tile> tiles;
    public List<ChessPiece> chessPiecesInPlay;

    private void Start()
    {
        // Add Chess Pieces in play
        int cpCount = chessPiecesParent.childCount;
        for (int i = 0; i < cpCount; i++)
        {
            Transform cpTransform = chessPiecesParent.GetChild(i);
            ChessPiece cp = cpTransform.GetComponent<ChessPiece>();
            chessPiecesInPlay.Add(cp);
        }

        //AddTilesToDictionary    
        dictionaryCoordsTiles = new Dictionary<Vector2Int, Tile>();
        int tileCount = transform.childCount;
        for (int i = 0; i < tileCount; i++)
        {
            Transform tileTransform = transform.GetChild(i);
            Tile tile = tileTransform.GetComponent<Tile>();
            dictionaryCoordsTiles[new Vector2Int(tile._tileCoords.x, tile._tileCoords.y)] = tile;
            tiles.Add(tile);

/*            
            if (tile._chessPiece) {
                chessPiecesInPlay.Add(tile._chessPiece);
            }*/
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

