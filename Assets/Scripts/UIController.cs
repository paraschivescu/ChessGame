using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    void DestroyDebugStickers()
    {

        foreach (Transform transform in debugLayerParent)
        {
            UnityEngine.Object.Destroy(transform.gameObject);
        }
    }

    public void DisplayMoveScores(List<Move> moves)
    {
        DestroyDebugStickers();
        foreach (Move mv in moves)
        {
            Vector3 tileScreenSpacePosition = Camera.main.WorldToScreenPoint(mv.tileDestination.transform.position);
            var sticker = Instantiate(uiElement, tileScreenSpacePosition, Quaternion.identity, debugLayerParent);
            sticker.gameObject.GetComponent<TextMeshProUGUI>().SetText(mv.scoreOfMove.ToString());
        }
        
    }

    void OnToggleDebugOverlay()
    {
        if (debugLayerParent.gameObject.activeSelf == false) debugLayerParent.gameObject.SetActive(true); else
        if (debugLayerParent.gameObject.activeSelf == true) debugLayerParent.gameObject.SetActive(false);
    }
}
