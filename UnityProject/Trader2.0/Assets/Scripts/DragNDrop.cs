﻿using UnityEngine;
using UnityEngine.EventSystems;
public class DragNDrop : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform rectTransform;
    void Awake()
    {
        rectTransform = transform as RectTransform;
        Transform testCanvasTransform = transform.parent;
        do
        {
            canvas = testCanvasTransform.GetComponent<Canvas>();
            testCanvasTransform = testCanvasTransform.parent;
        } while (canvas == null);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
