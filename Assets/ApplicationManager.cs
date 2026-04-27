//The full program can be found at github.com/wyndar/GeoCalculator
using SFB;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using TMPro;
using System.Text;
// ReSharper disable InconsistentNaming

public class ApplicationManager : MonoBehaviour
{
    private readonly ExtensionFilter[] dataExtensions = new[] { new ExtensionFilter("CSVs", "csv") };
    private const string warningString = "The following parameters have not been set: ";
    private readonly string emptyDataWarningString = "There is no data in the file." + Environment.NewLine + 
                                                     "A blank template will be exported.";

    [SerializeField] private TMP_InputField BHTField, TmsField, TdField, DField, RiField, RmfField, RmField, HField, PSPField, SPField;
    [SerializeField] private TMP_InputField TfField, RwField, VshField;
    [SerializeField] private Button runButton, clearButton, switchButton, homeButton, helpButton;
    [SerializeField] private GameObject warningPanel, outputContentPanel, singleInputPanel, bulkInputPanel, homePanel, helpPanel;
    [SerializeField] private GameObject activePanel;
    [SerializeField] private GameObject dataPanelPrefab;
    [SerializeField] private TMP_Text warningText, switchButtonText;

    public string saveData;

    private List<CalculationInput> data = new();
    private readonly List<CalculationResult> answers = new();
    private CalculationInput singleCalculationInput;
    private CalculationResult singleCalculationResult;
    private bool hasSingleCalculation;

    private void Start() => Home();

    public void RunCalc()
    {
        string error = NullCheck();
        if (error != "")
        {
            error = error[..^2];
            warningPanel.SetActive(true);
            warningText.text = warningString + error;
            return;
        }
        try
        {
            var input = ReadInputFields();
            var result = Calculate(input);
            singleCalculationInput = input;
            singleCalculationResult = result;
            hasSingleCalculation = true;
            warningPanel.SetActive(false);
            TfField.text = FormatFloat(result.Tf);
            RwField.text = FormatFloat(result.Rw);
            VshField.text = FormatFloat(result.Vsh);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            hasSingleCalculation = false;
            ShowWarning("Input Error!" + Environment.NewLine + ex.Message);
        }
    }

    public void WarningClear()
    {
        warningPanel.SetActive(false);
        if (!warningText.text.Contains(emptyDataWarningString)) return;
        SaveDataToFile();
    }
    public void GetDataFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Import Data", "", dataExtensions, false);
        if (paths.Length <= 0) return;
        LoadDataFile(paths[0]);
    }

    public void SaveDataFile()
    {
        saveData = ToCSV();
        if (!string.IsNullOrEmpty(saveData))
        {
            SaveDataToFile();
            return;
        }
        warningPanel.SetActive(true);
        warningText.text = emptyDataWarningString;
    }

    private void SaveDataToFile()
    {
        var path = StandaloneFileBrowser.SaveFilePanel("Save Data", "", "data", "csv");
        if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, saveData);
    }

    private void SetActivePanel(GameObject panel, string text)
    {
        if (activePanel != null)
            activePanel.SetActive(false);
        panel.SetActive(true);
        activePanel = panel;
        switchButtonText.text = text;
    }

    public void Home()
    {
        SetActivePanel(homePanel, "Start");
        homeButton.gameObject.SetActive(false);
        helpButton.gameObject.SetActive(true);
    }
    public void Help()
    {
        SetActivePanel(helpPanel, "Start");
        helpButton.gameObject.SetActive(false);
        homeButton.gameObject.SetActive(true);
    }
    public void SwitchInput()
    {
        helpButton.gameObject.SetActive(true);
        homeButton.gameObject.SetActive(true);
        if (activePanel == singleInputPanel)
            SetActivePanel(bulkInputPanel, "Single Input");
        else
            SetActivePanel(singleInputPanel, "Bulk Input");  
    }
    private string NullCheck()
    {
        string error = "";
        if (string.IsNullOrEmpty(BHTField.text))
            error += "BHT, ";
        if (string.IsNullOrEmpty(TmsField.text))
            error += "Tms, ";
        if (string.IsNullOrEmpty(TdField.text))
            error += "Td, ";
        if (string.IsNullOrEmpty(DField.text))
            error += "D, ";
        if (string.IsNullOrEmpty(RiField.text))
            error += "Ri, ";
        if (string.IsNullOrEmpty(RmfField.text))
            error += "Rmf, ";
        if (string.IsNullOrEmpty(RmField.text))
            error += "Rm, ";
        if (string.IsNullOrEmpty(HField.text))
            error += "H, ";
        if (string.IsNullOrEmpty(PSPField.text))
            error += "PSP, ";
        if (string.IsNullOrEmpty(SPField.text))
            error += "SP, ";
        return error;
    }

    private static CalculationResult Calculate(CalculationInput input)
    {
        if (Mathf.Approximately(input.Td, 0f))
            throw new InvalidOperationException("Td cannot be zero.");

        float gD = (input.BHT - input.Tms) / input.Td * 100;
        float tf = input.Tms + (gD * (input.D / 100));
        float tempCorrection = (input.BHT + 6.77f) / (tf + 6.77f);
        float rmm = input.Rm * tempCorrection;
        float rmff = input.Rmf * tempCorrection;
        if (Mathf.Approximately(rmm, 0f))
            throw new InvalidOperationException("Rm produces a zero resistivity correction.");

        float shortNormalCorrectedResistivity = input.Ri / rmm;
        float SSP;
        if (shortNormalCorrectedResistivity > 5 && input.H is > 3 and < 50)
        {
            float t = Mathf.Pow(4 * (shortNormalCorrectedResistivity + 2f), 1 / 3.65f) - 1.5f;
            float b = input.H - Mathf.Pow((shortNormalCorrectedResistivity + 11) / 0.65f, 1 / 6.05f) - 0.1f;
            float SPcor = (t / b) + 0.95f;
            SSP = input.SP * SPcor;
        }
        else
        {
            SSP = input.SP;
        }

        if (Mathf.Approximately(SSP, 0f))
            throw new InvalidOperationException("SP correction produces zero SSP.");

        float rwe = rmff * Mathf.Pow(10, SSP / (60 + (0.133f * tf)));
        float top = rwe + (0.131f * Mathf.Pow(10, 1 / Mathf.Log10(tf / 19.9f) - 2));
        float bottom = (-0.5f * rwe) + Mathf.Pow(10, 0.0426f / Mathf.Log10(tf / 50.8f));
        if (Mathf.Approximately(bottom, 0f))
            throw new InvalidOperationException("Calculated Rw denominator is zero.");

        float rw = top / bottom;
        float vsh = 1 - input.PSP / SSP;

        if (!IsFinite(tf) || !IsFinite(rw) || !IsFinite(vsh))
            throw new InvalidOperationException("Calculation produced a non-finite result.");

        return new CalculationResult(tf, rw, vsh);
    }

    private void SetAndCalculateValuesFromData()
    {
        ClearOutputRows();
        answers.Clear();
        hasSingleCalculation = false;
        if (data.Count == 0)
        {
            warningPanel.SetActive(true);
            warningText.text = "Input Error!" + Environment.NewLine + "Try exporting an empty csv to add your data.";
            return;
        }
        for (var i = 0; i < data.Count; i++)
        {
            try
            {
                var result = Calculate(data[i]);
                answers.Add(result);
                GameObject x = Instantiate(dataPanelPrefab, outputContentPanel.transform);
                x.GetComponent<DataPanel>().SetText(data[i], result);
                ResizeOutputContent();
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
                answers.Clear();
                ClearOutputRows();
                ShowWarning($"Input Error on row {i + 2}!{Environment.NewLine}{ex.Message}");
                return;
            }
        }

        warningPanel.SetActive(false);
    }

    public void ClearData()
    {
        data.Clear();
        answers.Clear();
        hasSingleCalculation = false;
        ClearOutputRows();
    }

    private string ToCSV()
    {
        if (data.Count > 0 && answers.Count == data.Count)
        {
            var sb = new StringBuilder("BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh");
            for (var i = 0; i < data.Count; i++)
            {
                sb.Append('\n').Append(FormatFloat(data[i].BHT)).Append(',').Append(FormatFloat(data[i].Tms)).Append(',').Append(FormatFloat(data[i].Td))
                    .Append(',').Append(FormatFloat(data[i].D)).Append(',').Append(FormatFloat(data[i].Ri)).Append(',').Append(FormatFloat(data[i].Rmf))
                    .Append(',').Append(FormatFloat(data[i].Rm)).Append(',').Append(FormatFloat(data[i].H)).Append(',').Append(FormatFloat(data[i].PSP))
                    .Append(',').Append(FormatFloat(data[i].SP)).Append(',').Append(FormatFloat(answers[i].Tf)).Append(',').Append(FormatFloat(answers[i].Rw))
                    .Append(',').Append(FormatFloat(answers[i].Vsh));
            }
            return sb.ToString();
        }

        if (!hasSingleCalculation) return string.Empty;
        {
            var sb = new StringBuilder("BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh");
            sb.Append('\n').Append(FormatFloat(singleCalculationInput.BHT)).Append(',').Append(FormatFloat(singleCalculationInput.Tms)).Append(',').Append(FormatFloat(singleCalculationInput.Td))
                .Append(',').Append(FormatFloat(singleCalculationInput.D)).Append(',').Append(FormatFloat(singleCalculationInput.Ri)).Append(',').Append(FormatFloat(singleCalculationInput.Rmf))
                .Append(',').Append(FormatFloat(singleCalculationInput.Rm)).Append(',').Append(FormatFloat(singleCalculationInput.H)).Append(',').Append(FormatFloat(singleCalculationInput.PSP))
                .Append(',').Append(FormatFloat(singleCalculationInput.SP)).Append(',').Append(FormatFloat(singleCalculationResult.Tf)).Append(',').Append(FormatFloat(singleCalculationResult.Rw))
                .Append(',').Append(FormatFloat(singleCalculationResult.Vsh));
            return sb.ToString();
        }

    }

    private CalculationInput ReadInputFields()
    {
        return new CalculationInput(
            ParseInputFloat(BHTField.text),
            ParseInputFloat(TmsField.text),
            ParseInputFloat(TdField.text),
            ParseInputFloat(DField.text),
            ParseInputFloat(RiField.text),
            ParseInputFloat(RmfField.text),
            ParseInputFloat(RmField.text),
            ParseInputFloat(HField.text),
            ParseInputFloat(PSPField.text),
            ParseInputFloat(SPField.text));
    }

    public void Clear()
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
        warningPanel.SetActive(false);
        hasSingleCalculation = false;
    }

    private void ClearOutputRows()
    {
        if (outputContentPanel.transform.childCount > 0)
        {
            for (int i = outputContentPanel.transform.childCount - 1; i >= 0; i--)
                Destroy(outputContentPanel.transform.GetChild(i).gameObject);
        }

        outputContentPanel.GetComponent<RectTransform>().sizeDelta = new(
            outputContentPanel.GetComponent<RectTransform>().sizeDelta.x,
            0);
    }

    private void LoadDataFile(string path)
    {
        try
        {
            data.Clear();
            data = CSVReader.Read(File.ReadAllText(path));
            SetAndCalculateValuesFromData();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            data.Clear();
            answers.Clear();
            ClearOutputRows();
            ShowWarning("Input Error!" + Environment.NewLine + ex.Message);
        }
    }

    private void ResizeOutputContent() =>
        outputContentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(
            outputContentPanel.GetComponent<RectTransform>().sizeDelta.x,
            outputContentPanel.transform.childCount * 40);

    private void ShowWarning(string message)
    {
        warningPanel.SetActive(true);
        warningText.text = message;
    }

    private static string FormatFloat(float value) => value.ToString("G", CultureInfo.InvariantCulture);

    private static float ParseInputFloat(string value)
    {
        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;

        if (float.TryParse(value, styles, CultureInfo.CurrentCulture, out float currentCultureValue))
            return currentCultureValue;

        return float.TryParse(value, styles, CultureInfo.InvariantCulture, out float invariantValue) 
            ? invariantValue 
            : throw new FormatException($"Invalid numeric value: {value}");
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
