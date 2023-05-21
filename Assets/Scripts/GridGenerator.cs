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

        GridGenerator myScript = (GridGenerator)target;

        if (GUILayout.Button("My Button"))
        {
            myScript.GenerateGridClicked();
        }
    }
}

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int _width, _height;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Transform _cam;
    public Transform GridParent;

    public void GenerateGridClicked()
    {
        Debug.Log("Generating Grid...");
        GenerateGrid();
    }

    void GenerateGrid()
    {

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y), Quaternion.identity, GridParent);
                spawnedTile.name = $"Tile {x} {y}";
                spawnedTile.coords = new Vector2(x, y);

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(isOffset);
            }
        }

        _cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }

}