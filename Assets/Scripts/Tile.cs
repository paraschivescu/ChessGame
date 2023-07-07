using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private Color _baseColor, _offsetColor;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private GameObject _highlight;
    public Vector2Int _tileCoords;
    public int _overwatchScore;


    public void Init(bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;
        _overwatchScore = 1;
    }

    public void HighlightTile(bool onOff) {
        if (onOff) {
            _highlight.SetActive(true);
        } else {
            _highlight.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        EventManager.RaiseTileClickedEvent(this);
    }
}
