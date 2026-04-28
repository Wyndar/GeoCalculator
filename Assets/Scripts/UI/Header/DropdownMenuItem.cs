using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
// ReSharper disable InconsistentNaming

public sealed class DropdownMenuItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color DefaultTextColor = Color.black;
    private static readonly Color DisabledTextColor = Color.grey;
    private static readonly Color ActiveTextColor = Color.white;
    private static readonly Color Transparent = Color.clear;

    private string routeName;
    private TMP_Text label;
    private UnityEngine.UI.Image background;
    private Color highlightColor;
    private Action<string> clickHandler;
    private bool isInteractable = true;

    public void Initialize(string itemRoute, TMP_Text itemLabel, UnityEngine.UI.Image itemBackground, Color color,
        Action<string> onClick)
    {
        routeName = itemRoute;
        label = itemLabel;
        background = itemBackground;
        highlightColor = color;
        clickHandler = onClick;
        label.raycastTarget = false;
        background.raycastTarget = true;
        SetHovered(false);
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        SetHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable || eventData.button != PointerEventData.InputButton.Left)
            return;

        clickHandler?.Invoke(routeName);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable)
            return;

        SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

    private void SetHovered(bool isHovered)
    {
        if (label != null)
            label.color = !isInteractable ? DisabledTextColor : isHovered ? ActiveTextColor : DefaultTextColor;

        if (background != null)
            background.color = isInteractable && isHovered ? highlightColor : Transparent;
    }
}
