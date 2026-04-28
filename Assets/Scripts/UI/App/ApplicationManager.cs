//The full program can be found at github.com/wyndar/GeoCalculator
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

public class ApplicationManager : MonoBehaviour
{
    public enum ViewMode
    {
        Info,
        Help,
        SingleInput,
        BulkInput
    }

    [SerializeField] private Color futaPurple = new(0.78431374f, 0f, 1f, 1f);
    [SerializeField] private TMP_InputField BHTField, TmsField, TdField, DField, RiField, RmfField, RmField, HField, PSPField, SPField;
    [SerializeField] private TMP_InputField TfField, RwField, VshField;
    [SerializeField] private Button runButton, clearButton;
    [SerializeField] private GameObject warningPanel, outputContentPanel, singleInputPanel, bulkInputPanel, homePanel, helpPanel;
    [SerializeField] private GameObject saveAsPanel;
    [SerializeField] private GameObject activePanel;
    [SerializeField] private GameObject dataPanelPrefab;
    [SerializeField] private TMP_Text warningText;

    private ApplicationOverlayController overlayController;
    private ApplicationInputController inputController;
    private ApplicationFileWorkflow fileWorkflow;
    private ViewMode currentView = ViewMode.Info;

    public Color FutaPurple => futaPurple;
    public ViewMode CurrentView => currentView;
    public bool RequiresInterruptOverlay => overlayController != null && overlayController.RequiresInterruptOverlay;

    private void Awake()
    {
        overlayController = new ApplicationOverlayController(this, warningPanel, warningText, saveAsPanel);
        inputController = new ApplicationInputController(
            BHTField,
            TmsField,
            TdField,
            DField,
            RiField,
            RmfField,
            RmField,
            HField,
            PSPField,
            SPField,
            TfField,
            RwField,
            VshField,
            outputContentPanel,
            dataPanelPrefab);
        fileWorkflow = new ApplicationFileWorkflow(overlayController, inputController.CreateExportSnapshot, HandleImportedData);

        overlayController.Initialize();
    }

    private void Start() => ShowInfoScreen();

    public void RunCalc() => inputController.RunSingleCalculation(overlayController.ShowWarning, overlayController.HideWarning);

    public void WarningClear() => overlayController.AcknowledgeWarning();

    public void GetDataFile() => fileWorkflow.OpenDataFile();

    public void SaveDataFile() => fileWorkflow.SaveDataFile();

    public void SaveDataFileAs() => fileWorkflow.SaveDataFileAs();

    public bool HasClearableData() => inputController.HasClearableData;

    public void ClearCurrentData() => inputController.ClearCurrentData(overlayController.HideWarning);

    public void ShowInfoScreen()
    {
        SetActivePanel(homePanel);
        currentView = ViewMode.Info;
    }

    public void ShowHelpScreen()
    {
        SetActivePanel(helpPanel);
        currentView = ViewMode.Help;
    }

    public void ShowSingleInput()
    {
        SetActivePanel(singleInputPanel);
        currentView = ViewMode.SingleInput;
    }

    public void ShowBulkInput()
    {
        SetActivePanel(bulkInputPanel);
        currentView = ViewMode.BulkInput;
    }

    public void SwitchToSingleInput()
    {
        inputController.SyncForSingleInputView();
        ShowSingleInput();
    }

    public void SwitchToBulkInput()
    {
        ShowBulkInput();
        inputController.SyncForBulkInputView(overlayController.ShowWarning, overlayController.HideWarning);
    }

    public void Home() => ShowInfoScreen();

    public void Help() => ShowHelpScreen();

    public void SwitchInput()
    {
        if (activePanel == singleInputPanel)
            SwitchToBulkInput();
        else
            SwitchToSingleInput();
    }

    private void SetActivePanel(GameObject panel)
    {
        homePanel.SetActive(false);
        helpPanel.SetActive(false);
        singleInputPanel.SetActive(false);
        bulkInputPanel.SetActive(false);

        panel.SetActive(true);
        activePanel = panel;
    }

    private void HandleImportedData(System.Collections.Generic.List<CalculationInput> importedData, string path)
    {
        ShowBulkInput();
        inputController.ReplaceBulkData(importedData, overlayController.ShowWarning, overlayController.HideWarning);
    }
}
