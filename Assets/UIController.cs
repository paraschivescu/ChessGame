using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public Camera cam;
    public RectTransform uiElement;
    public RectTransform canvasRectTransform;
    public GameObject gameObj;

    // Start is called before the first frame update
    void Start()
    {
        EventManager.ActivatedDebugOverlay += OnActivatedDebugOverlay;

    }

    void OnActivatedDebugOverlay()
    {
        Debug.Log("OnActivateDebugOverlay");

        Transform gameObjTransform = gameObj.transform;
        Vector2 scale = new(
            canvasRectTransform.rect.width / Screen.width,
            canvasRectTransform.rect.height / Screen.height
        );
        Debug.Log(scale);

        // Convert the position of the game object to screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(gameObjTransform.position);

        // Set the position of the UI element based on the game object's position in screen space
        uiElement.anchoredPosition = new Vector2(
            screenPos.x * scale.x,
            screenPos.y * scale.y
        );

    }

}
