using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private Color _baseColor, _offsetColor;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private GameObject _highlight;
    public Vector2Int coords;

    public void Init(bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;
    }

    public void HighlightTile(bool onOff) {
        if (onOff) {
            _highlight.SetActive(true);
        } else {
            _highlight.SetActive(false);
        }
    }

    void OnMouseEnter()
    {

    }

    void OnMouseExit()
    {

    }

    private void OnMouseDown()
    {
        EventManager.RaiseTileClickedEvent(this);
    }
}
