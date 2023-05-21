using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private Color _baseColor, _offsetColor;
    [SerializeField] private SpriteRenderer _renderer;
    public Vector2 coords;

    public void Init(bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;
    }

    void OnMouseEnter()
    {
        Color tileColor = GetComponent<SpriteRenderer>().color;
        tileColor.a = 0.5f; 
        GetComponent<SpriteRenderer>().color = tileColor;
    }

    void OnMouseExit()
    {
        Color tileColor = GetComponent<SpriteRenderer>().color;
        tileColor.a = 1f;
        GetComponent<SpriteRenderer>().color = tileColor;
    }
}
