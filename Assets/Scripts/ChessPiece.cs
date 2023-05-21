using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    [SerializeField] public Vector2 _positionCoordsCurrent;
    [SerializeField] private Vector2 _positionCoordsStart;
    [SerializeField] private LevelGrid _levelGridReference;

    void Start()
    {
        _positionCoordsCurrent = _positionCoordsStart;
    }
    
}
