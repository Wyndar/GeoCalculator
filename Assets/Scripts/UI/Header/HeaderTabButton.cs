using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HeaderTabButton : MonoBehaviour, IPointerClickHandler
{
    public enum HeaderButtonKind
    {
        File,
        Edit,
        Help,
        Info
    }

    private TMP_Text label;
    private Image underbar;
    private HeaderButtonKind kind;
    private Action<HeaderButtonKind> clickHandler;
    private Color activeColor;
    private static readonly Color DefaultTextColor = Color.black;
    private static readonly Color Transparent = new(1f, 1f, 1f, 0f);

    public HeaderButtonKind Kind => kind;

    public void Initialize(HeaderButtonKind buttonKind, Color highlightColor, Action<HeaderButtonKind> onClick)
    {
        kind = buttonKind;
        activeColor = highlightColor;
        clickHandler = onClick;
        label ??= GetComponentInChildren<TMP_Text>(true);
        underbar ??= transform.Find("Underbar")?.GetComponent<Image>();

        var button = GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;

        SetActiveVisual(false);
    }

    public void SetActiveVisual(bool isActive)
    {
        if (label != null)
            label.color = isActive ? activeColor : DefaultTextColor;

        if (underbar != null)
            underbar.color = isActive ? activeColor : Transparent;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        clickHandler?.Invoke(kind);
    }
}
