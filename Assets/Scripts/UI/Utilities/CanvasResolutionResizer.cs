using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasResolutionResizer : MonoBehaviour
{
    [SerializeField] private Vector2 newReferenceResolution = new(1920, 1080);

    private CanvasScaler canvasScaler;

    private void Awake() => canvasScaler = GetComponent<CanvasScaler>();

    [ContextMenu("Apply New Resolution")]
    public void ApplyNewResolution()
    {
        if (!canvasScaler)
            return;

        var oldResolution = canvasScaler.referenceResolution;
        canvasScaler.referenceResolution = newReferenceResolution;

        var ratioX = newReferenceResolution.x / oldResolution.x;
        var ratioY = newReferenceResolution.y / oldResolution.y;
        var textScaleRatio = Mathf.Min(ratioX, ratioY);

        foreach (RectTransform child in transform)
            ResizeChild(child, ratioX, ratioY, textScaleRatio);
    }

    private void ResizeChild(RectTransform rect, float ratioX, float ratioY, float textRatio)
    {
        rect.sizeDelta = new(rect.sizeDelta.x * ratioX, rect.sizeDelta.y * ratioY);
        rect.anchoredPosition = new(rect.anchoredPosition.x * ratioX, rect.anchoredPosition.y * ratioY);

        if (rect.TryGetComponent(out Text uiText))
            uiText.fontSize = Mathf.RoundToInt(uiText.fontSize * textRatio);

        if (rect.TryGetComponent(out TMP_Text tmpText))
            tmpText.fontSize *= textRatio;

        if (rect.TryGetComponent(out LayoutElement layout))
        {
            if (layout.preferredWidth > 0) layout.preferredWidth *= ratioX;
            if (layout.preferredHeight > 0) layout.preferredHeight *= ratioY;
            if (layout.minWidth > 0) layout.minWidth *= ratioX;
            if (layout.minHeight > 0) layout.minHeight *= ratioY;
        }

        foreach (RectTransform child in rect)
            ResizeChild(child, ratioX, ratioY, textRatio);
    }
}
