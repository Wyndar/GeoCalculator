using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

public sealed class ApplicationOverlayController
{
    private const string InterruptPanelName = "Interrupt Panel";
    private const string WarningTitleObjectName = "Header";
    private const string WarningDialogTitle = "WARNING!";
    private const string SaveAsRadioObjectName = "Save As Radio";
    private const string SaveButtonObjectName = "Save Button";
    private const string CancelButtonObjectName = "Cancel Button";

    private readonly MonoBehaviour owner;
    private readonly GameObject warningPanel;
    private readonly TMP_Text warningText;
    private readonly GameObject saveAsPanel;

    private Button interruptPanelButton;
    private TMP_Text warningTitleText;
    private Button warningActionButton;
    private TMP_Text warningActionButtonText;
    private ToggleGroup saveAsToggleGroup;
    private Button saveAsSaveButton;
    private Button saveAsCancelButton;
    private Action warningConfirmAction;
    private Action<string> saveAsConfirmAction;
    private Action saveAsCancelAction;

    public ApplicationOverlayController(MonoBehaviour owner, GameObject warningPanel, TMP_Text warningText, GameObject saveAsPanel)
    {
        this.owner = owner;
        this.warningPanel = warningPanel;
        this.warningText = warningText;
        this.saveAsPanel = saveAsPanel;
    }

    private bool IsWarningVisible => warningPanel != null && warningPanel.activeSelf;
    private bool IsSaveAsVisible => saveAsPanel != null && saveAsPanel.activeSelf;
    public bool RequiresInterruptOverlay => IsWarningVisible || IsSaveAsVisible;

    public void Initialize()
    {
        interruptPanelButton = FindInterruptPanel();
        CacheWarningUi();
        ConfigureSaveAsUi();
        DismissWarning();
        HideSaveAsPopup(false);
    }

    public void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null)
            return;

        ConfigureWarningDialog(message, null);
    }

    public void ShowEmptyExportWarning(string message, Action onAccepted)
    {
        if (warningPanel == null || warningText == null)
            return;

        ConfigureWarningDialog(message, onAccepted);
    }

    public void HideWarning() => DismissWarning();

    public void AcknowledgeWarning()
    {
        var confirmAction = warningConfirmAction;
        DismissWarning();
        confirmAction?.Invoke();
    }

    public void ShowSaveAsPopup(string defaultExtension, Action<string> onConfirmed, Action onCancelled)
    {
        if (saveAsPanel == null)
        {
            ShowWarning("Save Error!" + Environment.NewLine + "Save As popup is not configured.");
            onCancelled?.Invoke();
            return;
        }

        saveAsConfirmAction = onConfirmed;
        saveAsCancelAction = onCancelled;
        DismissWarning();
        ApplyDefaultSaveAsSelection(defaultExtension);
        saveAsPanel.SetActive(true);
        UpdateInterruptPanelState();
    }

    private void CancelSaveAsPopup() => CancelSaveAsPopup(true);

    private void ConfigureWarningDialog(string message, Action onConfirmed)
    {
        EnsureWarningUi();
        warningConfirmAction = onConfirmed;

        if (warningTitleText != null)
            warningTitleText.text = WarningDialogTitle;

        warningText.text = message;
        ConfigureDefaultWarningActionButton(true, "OK");
        warningPanel.SetActive(true);
        UpdateInterruptPanelState();
    }

    private void DismissWarning()
    {
        warningConfirmAction = null;

        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (warningText != null)
            warningText.text = string.Empty;

        if (warningTitleText != null)
            warningTitleText.text = WarningDialogTitle;

        ConfigureDefaultWarningActionButton(true, "OK");
        UpdateInterruptPanelState();
    }

    private void CacheWarningUi()
    {
        if (warningPanel == null)
            return;

        warningTitleText = warningPanel.transform.Find(WarningTitleObjectName)?.GetComponent<TMP_Text>();
        warningActionButton = warningPanel.GetComponentInChildren<Button>(true);
        warningActionButtonText = warningActionButton != null
            ? warningActionButton.GetComponentInChildren<TMP_Text>(true)
            : null;
    }

    private void EnsureWarningUi()
    {
        if (warningActionButton == null || warningText == null)
            CacheWarningUi();
    }

    private void ConfigureSaveAsUi()
    {
        CacheSaveAsUi();

        if (saveAsSaveButton != null)
        {
            saveAsSaveButton.onClick.RemoveListener(ConfirmSaveAsSelection);
            saveAsSaveButton.onClick.AddListener(ConfirmSaveAsSelection);
        }

        if (saveAsCancelButton != null)
        {
            saveAsCancelButton.onClick.RemoveListener(CancelSaveAsPopup);
            saveAsCancelButton.onClick.AddListener(CancelSaveAsPopup);
        }
    }

    private void CacheSaveAsUi()
    {
        if (saveAsPanel == null)
            return;

        var radioTransform = saveAsPanel.transform.Find(SaveAsRadioObjectName);
        saveAsToggleGroup = radioTransform != null ? radioTransform.GetComponent<ToggleGroup>() : null;

        var saveButtonTransform = saveAsPanel.transform.Find(SaveButtonObjectName);
        saveAsSaveButton = saveButtonTransform != null ? saveButtonTransform.GetComponent<Button>() : null;

        var cancelButtonTransform = saveAsPanel.transform.Find(CancelButtonObjectName);
        saveAsCancelButton = cancelButtonTransform != null ? cancelButtonTransform.GetComponent<Button>() : null;
    }

    private void ConfirmSaveAsSelection()
    {
        var extension = GetSelectedSaveExtension();
        if (string.IsNullOrWhiteSpace(extension))
        {
            ShowWarning("Save Error!" + Environment.NewLine + "Select a save type before continuing.");
            return;
        }

        var confirmAction = saveAsConfirmAction;
        HideSaveAsPopup(false);
        ClearSaveAsCallbacks();
        confirmAction?.Invoke(extension);
    }

    private void CancelSaveAsPopup(bool notifyCancellation)
    {
        var cancelAction = notifyCancellation ? saveAsCancelAction : null;
        HideSaveAsPopup(true);
        cancelAction?.Invoke();
    }

    private void HideSaveAsPopup(bool clearCallbacks)
    {
        if (saveAsPanel != null)
            saveAsPanel.SetActive(false);

        if (clearCallbacks)
            ClearSaveAsCallbacks();

        UpdateInterruptPanelState();
    }

    private void ClearSaveAsCallbacks()
    {
        saveAsConfirmAction = null;
        saveAsCancelAction = null;
    }

    private void ApplyDefaultSaveAsSelection(string extension)
    {
        SetSaveAsSelection(string.IsNullOrWhiteSpace(extension)
            ? DataFileSerializer.DefaultExtension.TrimStart('.')
            : extension.Trim().TrimStart('.').ToLowerInvariant());
    }

    private string GetSelectedSaveExtension()
    {
        if (saveAsToggleGroup == null)
            return null;

        foreach (var toggle in saveAsToggleGroup.ActiveToggles())
        {
            var label = toggle.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                continue;

            var extension = label.text.Trim().TrimStart('.').ToLowerInvariant();
            if (extension is "csv" or "tsv" or "json" or "xlsx")
                return extension;
        }

        return null;
    }

    private void SetSaveAsSelection(string extension)
    {
        if (saveAsToggleGroup == null || string.IsNullOrWhiteSpace(extension))
            return;

        var normalizedExtension = extension.Trim().TrimStart('.').ToLowerInvariant();
        foreach (var toggle in saveAsToggleGroup.GetComponentsInChildren<Toggle>(true))
        {
            var label = toggle.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                continue;

            toggle.isOn = string.Equals(
                label.text.Trim().TrimStart('.'),
                normalizedExtension,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ConfigureDefaultWarningActionButton(bool isVisible, string label)
    {
        if (warningActionButton == null)
            return;

        warningActionButton.gameObject.SetActive(isVisible);
        if (warningActionButtonText != null && !string.IsNullOrEmpty(label))
            warningActionButtonText.text = label;
    }

    private Button FindInterruptPanel()
    {
        var canvas = owner.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != InterruptPanelName)
                continue;

            var button = child.GetComponent<Button>();
            if (button == null)
                return null;

            button.onClick.RemoveListener(DismissTransientOverlays);
            button.onClick.AddListener(DismissTransientOverlays);
            child.gameObject.SetActive(false);
            return button;
        }

        return null;
    }

    private void DismissTransientOverlays()
    {
        DismissWarning();
        CancelSaveAsPopup(true);
    }

    private void UpdateInterruptPanelState()
    {
        if (interruptPanelButton == null)
            return;

        interruptPanelButton.gameObject.SetActive(RequiresInterruptOverlay);
    }
}
