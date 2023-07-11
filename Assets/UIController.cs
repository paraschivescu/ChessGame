using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public RectTransform uiElement;
    public RectTransform canvasRectTransform;
    public GameObject gameObj;

    private LevelGrid levelGrid;
    [SerializeField] private Transform debugLayerParent;    
    
    void Start()
    {
        EventManager.ToggleDebugOverlay += OnToggleDebugOverlay;
        levelGrid = FindObjectOfType<LevelGrid>();

        foreach (Tile t in levelGrid.tiles)
        {
            Vector3 tileScreenSpacePosition = Camera.main.WorldToScreenPoint(t.transform.position);
            RectTransform uiTileScore = Instantiate(uiElement, tileScreenSpacePosition, Quaternion.identity, debugLayerParent);
        }
    }

    public void DisplayPossibleMoveScores(int currentBoardScore, List<Move> moves)
    {
        Debug.Log(currentBoardScore);
    }

    void OnToggleDebugOverlay()
    {
        if (debugLayerParent.gameObject.activeSelf == false) debugLayerParent.gameObject.SetActive(true); else
        if (debugLayerParent.gameObject.activeSelf == true) debugLayerParent.gameObject.SetActive(false);
    }
}
