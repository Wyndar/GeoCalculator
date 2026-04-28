using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

public sealed class HeaderController : MonoBehaviour
{
    private const string FileDropdownName = "File Drop Panel";
    private const string EditDropdownName = "Edit Drop Panel";
    private const string InterruptPanelName = "Interrupt Panel";

    private enum DropdownKind
    {
        None,
        File,
        Edit
    }

    private ApplicationManager applicationManager;
    private HeaderTabButton fileButton;
    private HeaderTabButton editButton;
    private HeaderTabButton helpButton;
    private HeaderTabButton infoButton;
    private DropdownMenu fileDropdown;
    private DropdownMenu editDropdown;
    private Button closeOverlay;
    private DropdownKind openDropdown = DropdownKind.None;

    private void Awake()
    {
        applicationManager = FindFirstObjectByType<ApplicationManager>();
        if (applicationManager == null)
            throw new InvalidOperationException("HeaderController requires an ApplicationManager in the scene.");

        fileButton = ConfigureHeaderButton("File", HeaderTabButton.HeaderButtonKind.File);
        editButton = ConfigureHeaderButton("Edit", HeaderTabButton.HeaderButtonKind.Edit);
        helpButton = ConfigureHeaderButton("Help", HeaderTabButton.HeaderButtonKind.Help);
        infoButton = ConfigureHeaderButton("Info", HeaderTabButton.HeaderButtonKind.Info);

        fileDropdown = ConfigureDropdown(FileDropdownName);
        editDropdown = ConfigureDropdown(EditDropdownName);
        closeOverlay = FindInterruptPanel();
    }

    private void Start()
    {
        CloseAllDropdowns();
        applicationManager.ShowInfoScreen();
        RefreshHeaderVisualState();
    }

    private void Update()
    {
        HandleKeyboardShortcuts();
    }

    private HeaderTabButton ConfigureHeaderButton(string childName, HeaderTabButton.HeaderButtonKind kind)
    {
        var child = transform.Find(childName);
        if (child == null)
            return null;

        var tab = child.GetComponent<HeaderTabButton>();
        if (tab == null)
            tab = child.gameObject.AddComponent<HeaderTabButton>();

        tab.Initialize(kind, GetFutaPurple(), HandleHeaderClick);
        return tab;
    }

    private DropdownMenu ConfigureDropdown(string objectName)
    {
        var dropdownTransform = FindDropdownTransform(objectName);
        if (dropdownTransform == null)
            return null;

        var dropdown = dropdownTransform.GetComponent<DropdownMenu>();
        if (dropdown == null)
            dropdown = dropdownTransform.gameObject.AddComponent<DropdownMenu>();

        dropdown.Initialize(GetFutaPurple(), HandleDropdownItemClick, CanSelectRoute);
        dropdown.Close();
        return dropdown;
    }

    private Transform FindDropdownTransform(string objectName)
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;
        
        var allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        foreach (var child in allTransforms)
            if (child.name == objectName)
                return child;

        return null;
    }

    private Button FindInterruptPanel()
    {
        var panelTransform = FindDropdownTransform(InterruptPanelName);
        if (panelTransform == null)
            return null;

        var button = panelTransform.GetComponent<Button>();
        if (button == null)
            return null;

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveListener(CloseAllDropdowns);
        button.onClick.AddListener(CloseAllDropdowns);
        panelTransform.gameObject.SetActive(false);
        return button;
    }

    private void HandleHeaderClick(HeaderTabButton.HeaderButtonKind kind)
    {
        switch (kind)
        {
            case HeaderTabButton.HeaderButtonKind.Info:
                CloseAllDropdowns();
                applicationManager.ShowInfoScreen();
                RefreshHeaderVisualState();
                break;
            case HeaderTabButton.HeaderButtonKind.Help:
                CloseAllDropdowns();
                applicationManager.ShowHelpScreen();
                RefreshHeaderVisualState();
                break;
            case HeaderTabButton.HeaderButtonKind.File:
                ToggleDropdown(fileDropdown, DropdownKind.File);
                break;
            case HeaderTabButton.HeaderButtonKind.Edit:
                ToggleDropdown(editDropdown, DropdownKind.Edit);
                break;
        }
    }

    private void HandleDropdownItemClick(string routeName)
    {
        if (!CanSelectRoute(routeName))
            return;

        CloseAllDropdowns();
        ExecuteRoute(routeName);
        RefreshHeaderVisualState();
    }

    private void ToggleDropdown(DropdownMenu dropdown, DropdownKind dropdownKind)
    {
        if (dropdown == null)
            return;

        var shouldOpen = !dropdown.gameObject.activeSelf;
        CloseAllDropdowns();
        if (!shouldOpen)
            return;

        openDropdown = dropdownKind;
        dropdown.Open(CanSelectRoute);
        if (closeOverlay == null) return;
        closeOverlay.gameObject.SetActive(true);
        RefreshHeaderVisualState();
    }

    private void CloseAllDropdowns()
    {
        openDropdown = DropdownKind.None;
        fileDropdown?.Close();
        editDropdown?.Close();
        if (closeOverlay != null && (applicationManager == null || !applicationManager.RequiresInterruptOverlay))
            closeOverlay.gameObject.SetActive(false);
        RefreshHeaderVisualState();
    }

    private void RefreshHeaderVisualState()
    {
        var currentView = applicationManager.CurrentView;
        var infoActive = openDropdown == DropdownKind.None && currentView == ApplicationManager.ViewMode.Info;
        var helpActive = openDropdown == DropdownKind.None && currentView == ApplicationManager.ViewMode.Help;
        var fileActive = openDropdown == DropdownKind.File;
        var editActive = openDropdown == DropdownKind.Edit;

        infoButton?.SetActiveVisual(infoActive);
        helpButton?.SetActiveVisual(helpActive);
        fileButton?.SetActiveVisual(fileActive);
        editButton?.SetActiveVisual(editActive);
    }

    private bool CanSelectRoute(string routeName)
    {
        switch (routeName)
        {
            case "Single Input":
                return applicationManager.CurrentView != ApplicationManager.ViewMode.SingleInput;
            case "Bulk Input":
                return applicationManager.CurrentView != ApplicationManager.ViewMode.BulkInput;
            case "Clear":
                return applicationManager.HasClearableData();
            default:
                return true;
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (applicationManager == null || applicationManager.RequiresInterruptOverlay || IsTextInputFocused())
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        var ctrlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        var shiftPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        if (ctrlPressed && shiftPressed && keyboard.sKey.wasPressedThisFrame)
        {
            TryExecuteShortcut("Save As");
            return;
        }

        if (ctrlPressed && keyboard.oKey.wasPressedThisFrame)
        {
            TryExecuteShortcut("Open");
            return;
        }

        if (ctrlPressed && keyboard.sKey.wasPressedThisFrame)
        {
            TryExecuteShortcut("Save");
            return;
        }

        if (shiftPressed && keyboard.sKey.wasPressedThisFrame)
        {
            TryExecuteShortcut("Single Input");
            return;
        }

        if (shiftPressed && keyboard.bKey.wasPressedThisFrame)
        {
            TryExecuteShortcut("Bulk Input");
            return;
        }

        if (shiftPressed && keyboard.cKey.wasPressedThisFrame)
            TryExecuteShortcut("Clear");
    }

    private void TryExecuteShortcut(string routeName)
    {
        if (!CanSelectRoute(routeName))
            return;

        CloseAllDropdowns();
        ExecuteRoute(routeName);
        RefreshHeaderVisualState();
    }

    private void ExecuteRoute(string routeName)
    {
        switch (routeName)
        {
            case "Open":
                applicationManager.GetDataFile();
                break;
            case "Save":
                applicationManager.SaveDataFile();
                break;
            case "Save As":
                applicationManager.SaveDataFileAs();
                break;
            case "Single Input":
                applicationManager.SwitchToSingleInput();
                break;
            case "Bulk Input":
                applicationManager.SwitchToBulkInput();
                break;
            case "Clear":
                applicationManager.ClearCurrentData();
                break;
        }
    }

    private static bool IsTextInputFocused()
    {
        var selectedObject = EventSystem.current?.currentSelectedGameObject;
        if (selectedObject == null)
            return false;

        return selectedObject.GetComponentInParent<TMP_InputField>() != null;
    }

    private Color GetFutaPurple() => applicationManager.FutaPurple;
}
