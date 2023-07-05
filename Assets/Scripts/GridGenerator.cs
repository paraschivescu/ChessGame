using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridGenerator))]
public class MyScriptEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GridGenerator gg = (GridGenerator)target;
        if (GUILayout.Button("Generate Grid")) {
            gg.GenerateGrid();
        }
        if (GUILayout.Button("Align Pieces To Grid"))
        {
            gg.AlignChessPiecesToGrid();
        }
    }
}

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int _width, _height;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _gridParent;
    [SerializeField] private GameController _gameController;

    void Start() 
    {
        AlignChessPiecesToGrid();
    }

    public void AlignChessPiecesToGrid() 
    { 
        Debug.Log("Aligning chess pieces to grid...");

        List<ChessPiece> _cpList = _gameController.GetListOfChessPieces();
        List<Tile> _tileList = new();

        // populate tile list
        for (int i = 0; i < _gridParent.transform.childCount; i++)
        {
            Tile t = _gridParent.transform.GetChild(i).GetComponent<Tile>();
            _tileList.Add(t);
        }

        // iterate all chess pieces, find nearest tile for each of them 
        foreach (ChessPiece cp in _cpList)
        {
            GameObject go = new("ClosestTile"); // needed to fix "You are trying to create a MonoBehaviour using the 'new' keyword. This is not allowed."
            Tile closestTile = go.AddComponent<Tile>();
            float lowestDistance = Mathf.Infinity;

            foreach (Tile t in _tileList) {
                float distance = Vector3.Distance(t.transform.position, cp.transform.position);
                if (distance < lowestDistance) {
                    lowestDistance = distance;
                    closestTile = t;
                }
            }
            // change the piece's transform & coords to match those of the tile
            cp.transform.position = closestTile.transform.position;
            cp.positionCoordsCurrent = closestTile._tileCoords;
            // set chess piece on this tile
            closestTile._chessPiece = cp;
            // set tile that the cp is on
            cp._currentTile = closestTile;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cp);
            DestroyImmediate(go);
        }        
    } 

    public void GenerateGrid()
    {
        Debug.Log("Generating grid...");
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y), Quaternion.identity, _gridParent);
                spawnedTile.name = $"Tile {x} {y}";
                spawnedTile._tileCoords = new Vector2Int(x, y);

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(isOffset);
            }
        }

        //_cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }

}