using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming

public sealed class ApplicationInputController
{
    private const string WarningPrefix = "The following parameters have not been set: ";

    private readonly TMP_InputField BHTField;
    private readonly TMP_InputField TmsField;
    private readonly TMP_InputField TdField;
    private readonly TMP_InputField DField;
    private readonly TMP_InputField RiField;
    private readonly TMP_InputField RmfField;
    private readonly TMP_InputField RmField;
    private readonly TMP_InputField HField;
    private readonly TMP_InputField PSPField;
    private readonly TMP_InputField SPField;
    private readonly TMP_InputField TfField;
    private readonly TMP_InputField RwField;
    private readonly TMP_InputField VshField;
    private readonly GameObject outputContentPanel;
    private readonly GameObject dataPanelPrefab;

    private List<CalculationInput> data = new();
    private readonly List<CalculationResult> answers = new();
    private CalculationInput singleCalculationInput;
    private CalculationResult singleCalculationResult;
    private bool hasSingleCalculation;

    public ApplicationInputController(
        TMP_InputField BHTField,
        TMP_InputField TmsField,
        TMP_InputField TdField,
        TMP_InputField DField,
        TMP_InputField RiField,
        TMP_InputField RmfField,
        TMP_InputField RmField,
        TMP_InputField HField,
        TMP_InputField PSPField,
        TMP_InputField SPField,
        TMP_InputField TfField,
        TMP_InputField RwField,
        TMP_InputField VshField,
        GameObject outputContentPanel,
        GameObject dataPanelPrefab)
    {
        this.BHTField = BHTField;
        this.TmsField = TmsField;
        this.TdField = TdField;
        this.DField = DField;
        this.RiField = RiField;
        this.RmfField = RmfField;
        this.RmField = RmField;
        this.HField = HField;
        this.PSPField = PSPField;
        this.SPField = SPField;
        this.TfField = TfField;
        this.RwField = RwField;
        this.VshField = VshField;
        this.outputContentPanel = outputContentPanel;
        this.dataPanelPrefab = dataPanelPrefab;
    }

    public bool HasClearableData => HasAnySingleInputContent() || data.Count > 0 || answers.Count > 0;

    public void RunSingleCalculation(Action<string> showWarning, Action hideWarning)
    {
        var missingFields = GetMissingRequiredFields();
        if (!string.IsNullOrEmpty(missingFields))
        {
            showWarning(WarningPrefix + missingFields);
            return;
        }

        try
        {
            var input = ReadInputFields();
            var result = CalculationService.Calculate(input);
            singleCalculationInput = input;
            singleCalculationResult = result;
            hasSingleCalculation = true;
            hideWarning();
            ApplyOutputFields(result);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            hasSingleCalculation = false;
            showWarning("Input Error!" + Environment.NewLine + ex.Message);
        }
    }

    public void ReplaceBulkData(List<CalculationInput> importedData, Action<string> showWarning, Action hideWarning)
    {
        data = importedData ?? new List<CalculationInput>();
        RecalculateBulkData(showWarning, hideWarning);
    }

    public void SyncForSingleInputView()
    {
        if (data.Count == 0)
            return;

        ApplySingleInput(data[0], answers.Count > 0 ? answers[0] : null);
    }

    public void SyncForBulkInputView(Action<string> showWarning, Action hideWarning)
    {
        CarrySingleInputToBulkFirstEntry();
        if (data.Count > 0)
            RecalculateBulkData(showWarning, hideWarning);
        else
            ClearOutputRows();
    }

    public void ClearCurrentData(Action hideWarning)
    {
        ClearFields();
        ClearData();
        hideWarning();
    }

    public CalculationExportSnapshot CreateExportSnapshot()
    {
        return new CalculationExportSnapshot(
            data,
            answers,
            hasSingleCalculation ? singleCalculationInput : null,
            hasSingleCalculation ? singleCalculationResult : null);
    }

    private void RecalculateBulkData(Action<string> showWarning, Action hideWarning)
    {
        ClearOutputRows();
        answers.Clear();
        hasSingleCalculation = false;

        if (data.Count == 0)
        {
            showWarning("Input Error!" + Environment.NewLine + "Try exporting an empty csv to add your data.");
            return;
        }

        for (var i = 0; i < data.Count; i++)
        {
            try
            {
                var result = CalculationService.Calculate(data[i]);
                answers.Add(result);
                var row = UnityEngine.Object.Instantiate(dataPanelPrefab, outputContentPanel.transform);
                row.GetComponent<DataPanel>().SetText(data[i], result);
                ResizeOutputContent();
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
                answers.Clear();
                ClearOutputRows();
                showWarning($"Input Error on row {i + 2}!{Environment.NewLine}{ex.Message}");
                return;
            }
        }

        hideWarning();
    }

    private string GetMissingRequiredFields()
    {
        var missingFields = new List<string>();
        AddMissingField(missingFields, "BHT", BHTField.text);
        AddMissingField(missingFields, "Tms", TmsField.text);
        AddMissingField(missingFields, "Td", TdField.text);
        AddMissingField(missingFields, "D", DField.text);
        AddMissingField(missingFields, "Ri", RiField.text);
        AddMissingField(missingFields, "Rmf", RmfField.text);
        AddMissingField(missingFields, "Rm", RmField.text);
        AddMissingField(missingFields, "H", HField.text);
        AddMissingField(missingFields, "PSP", PSPField.text);
        AddMissingField(missingFields, "SP", SPField.text);
        return string.Join(", ", missingFields);
    }

    private static void AddMissingField(ICollection<string> missingFields, string fieldName, string fieldValue)
    {
        if (string.IsNullOrEmpty(fieldValue))
            missingFields.Add(fieldName);
    }

    private CalculationInput ReadInputFields()
    {
        return new CalculationInput(
            CalculationService.ParseInputFloat(BHTField.text),
            CalculationService.ParseInputFloat(TmsField.text),
            CalculationService.ParseInputFloat(TdField.text),
            CalculationService.ParseInputFloat(DField.text),
            CalculationService.ParseInputFloat(RiField.text),
            CalculationService.ParseInputFloat(RmfField.text),
            CalculationService.ParseInputFloat(RmField.text),
            CalculationService.ParseInputFloat(HField.text),
            CalculationService.ParseInputFloat(PSPField.text),
            CalculationService.ParseInputFloat(SPField.text));
    }

    private void ApplyOutputFields(CalculationResult result)
    {
        TfField.text = CalculationService.FormatFloat(result.Tf);
        RwField.text = CalculationService.FormatFloat(result.Rw);
        VshField.text = CalculationService.FormatFloat(result.Vsh);
    }

    private void ClearFields()
    {
        BHTField.text = "";
        TmsField.text = "";
        TdField.text = "";
        DField.text = "";
        RmfField.text = "";
        RiField.text = "";
        RmField.text = "";
        HField.text = "";
        PSPField.text = "";
        SPField.text = "";
        TfField.text = "";
        RwField.text = "";
        VshField.text = "";
        hasSingleCalculation = false;
    }

    private void ClearData()
    {
        data.Clear();
        answers.Clear();
        hasSingleCalculation = false;
        ClearOutputRows();
    }

    private void ClearOutputRows()
    {
        if (outputContentPanel.transform.childCount > 0)
        {
            for (var i = outputContentPanel.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(outputContentPanel.transform.GetChild(i).gameObject);
        }

        outputContentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(
            outputContentPanel.GetComponent<RectTransform>().sizeDelta.x,
            0);
    }

    private void ResizeOutputContent()
    {
        outputContentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(
            outputContentPanel.GetComponent<RectTransform>().sizeDelta.x,
            outputContentPanel.transform.childCount * 40);
    }

    private bool HasAnySingleInputContent()
    {
        return !string.IsNullOrWhiteSpace(BHTField.text) ||
               !string.IsNullOrWhiteSpace(TmsField.text) ||
               !string.IsNullOrWhiteSpace(TdField.text) ||
               !string.IsNullOrWhiteSpace(DField.text) ||
               !string.IsNullOrWhiteSpace(RiField.text) ||
               !string.IsNullOrWhiteSpace(RmfField.text) ||
               !string.IsNullOrWhiteSpace(RmField.text) ||
               !string.IsNullOrWhiteSpace(HField.text) ||
               !string.IsNullOrWhiteSpace(PSPField.text) ||
               !string.IsNullOrWhiteSpace(SPField.text) ||
               !string.IsNullOrWhiteSpace(TfField.text) ||
               !string.IsNullOrWhiteSpace(RwField.text) ||
               !string.IsNullOrWhiteSpace(VshField.text);
    }

    private void CarrySingleInputToBulkFirstEntry()
    {
        if (!TryReadSingleInput(out var input))
            return;

        if (data.Count == 0)
            data.Add(input);
        else
            data[0] = input;
    }

    private bool TryReadSingleInput(out CalculationInput input)
    {
        input = null;
        if (!HasAnyPrimaryInputValues())
            return false;

        if (!string.IsNullOrWhiteSpace(GetMissingRequiredFields()))
            return false;

        try
        {
            input = ReadInputFields();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool HasAnyPrimaryInputValues()
    {
        return !string.IsNullOrWhiteSpace(BHTField.text) ||
               !string.IsNullOrWhiteSpace(TmsField.text) ||
               !string.IsNullOrWhiteSpace(TdField.text) ||
               !string.IsNullOrWhiteSpace(DField.text) ||
               !string.IsNullOrWhiteSpace(RiField.text) ||
               !string.IsNullOrWhiteSpace(RmfField.text) ||
               !string.IsNullOrWhiteSpace(RmField.text) ||
               !string.IsNullOrWhiteSpace(HField.text) ||
               !string.IsNullOrWhiteSpace(PSPField.text) ||
               !string.IsNullOrWhiteSpace(SPField.text);
    }

    private void ApplySingleInput(CalculationInput input, CalculationResult result)
    {
        BHTField.text = CalculationService.FormatFloat(input.BHT);
        TmsField.text = CalculationService.FormatFloat(input.Tms);
        TdField.text = CalculationService.FormatFloat(input.Td);
        DField.text = CalculationService.FormatFloat(input.D);
        RiField.text = CalculationService.FormatFloat(input.Ri);
        RmfField.text = CalculationService.FormatFloat(input.Rmf);
        RmField.text = CalculationService.FormatFloat(input.Rm);
        HField.text = CalculationService.FormatFloat(input.H);
        PSPField.text = CalculationService.FormatFloat(input.PSP);
        SPField.text = CalculationService.FormatFloat(input.SP);

        if (result == null)
        {
            TfField.text = "";
            RwField.text = "";
            VshField.text = "";
            hasSingleCalculation = false;
            return;
        }

        ApplyOutputFields(result);
        singleCalculationInput = input;
        singleCalculationResult = result;
        hasSingleCalculation = true;
    }
}
