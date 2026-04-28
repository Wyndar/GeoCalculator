using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DropdownMenu : MonoBehaviour
{
    public void Initialize(Color highlightColor, System.Action<string> clickHandler, System.Func<string, bool> canSelectRoute)
    {
        foreach (Transform child in transform)
        {
            var label = child.GetComponentInChildren<TMP_Text>(true);
            var background = child.GetComponent<Image>();
            if (label == null || background == null)
                continue;

            var item = child.GetComponent<DropdownMenuItem>();
            if (item == null)
                item = child.gameObject.AddComponent<DropdownMenuItem>();

            item.Initialize(child.name, label, background, highlightColor, clickHandler);
            item.SetInteractable(canSelectRoute(child.name));
        }
    }

    public void Open(System.Func<string, bool> canSelectRoute)
    {
        RefreshInteractableState(canSelectRoute);
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void RefreshInteractableState(System.Func<string, bool> canSelectRoute)
    {
        foreach (Transform child in transform)
        {
            var item = child.GetComponent<DropdownMenuItem>();
            if (item == null)
                continue;

            item.SetInteractable(canSelectRoute(child.name));
        }
    }
}
