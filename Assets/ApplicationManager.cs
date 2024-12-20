//The full program can be found at github.com/wyndar/GeoCalculator
using SFB;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Text;

public class ApplicationManager : MonoBehaviour
{
    private readonly ExtensionFilter[] dataExtensions = new[] { new ExtensionFilter("CSVs", "csv") };
    private readonly string warningString = "The following parameters have not been set: ";
    private readonly string emptyDataWarningString = "There is no data in the file." + System.Environment.NewLine + 
        "A blank template will be exported.";

    [SerializeField] private TMP_InputField BHTField, TmsField, TdField, DField, RiField, RmfField, RmField, HField, PSPField, SPField;
    [SerializeField] private TMP_InputField TfField, RwField, VshField;
    [SerializeField] private Button runButton, clearButton, switchButton, homeButton, helpButton;
    [SerializeField] private GameObject warningPanel, outputContentPanel, singleInputPanel, bulkInputPanel, homePanel, helpPanel;
    [SerializeField] private GameObject activePanel;
    [SerializeField] private GameObject dataPanelPrefab;
    [SerializeField] private TMP_Text warningText, switchButtonText;

    public string saveData;

    private List<Dictionary<string, float>> data = new();
    private List<Dictionary<string, float>> answers = new();
    //calc params
    private float BHT, Tms, Td, D, Ri, Rmf, Rm, H, PSP, SP;
    //calc answers
    private float Tf, Rw, Vsh;

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
        SetValues();
        Calc();
        TfField.text = Tf.ToSafeString();
        RwField.text = Rw.ToSafeString();
        VshField.text= Vsh.ToSafeString();
    }

    public void WarningClear()
    {
        warningPanel.SetActive(false);
        if (warningText.text.Contains(emptyDataWarningString))
            SaveDataToFile();
    }
    public void GetDataFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Import Data", "", dataExtensions, false);
        if (paths.Length > 0)
        {
            StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        }
    }
    private IEnumerator OutputRoutine(string url)
    {
        var loader = new WWW (url);
        yield return loader;
        data.Clear();
        data = CSVReader.Read(loader.text);
        SetAndCalculateValuesFromData();
    }

    public void SaveDataFile()
    {
        saveData = ToCSV();
        if (answers.Count > 0)
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

    private void Calc()
    {
        warningPanel.SetActive(false);
        Tf = 0;
        Rw = 0;
        Vsh = 0;
        float gD = (BHT - Tms) / Td * 100;
        Tf = Tms + (gD * (D / 100));
        float tempCorrection = (BHT + 6.77f) / (Tf + 6.77f);
        float rmm = Rm * tempCorrection;
        float rmff = Rmf * tempCorrection;
        float shortNormalCorrectedResistivity = Ri / rmm;
        float SSP;
        if (shortNormalCorrectedResistivity > 5 && H > 3 && H < 50)
        {
            float t = Mathf.Pow(4 * (shortNormalCorrectedResistivity + 2f), 1 / 3.65f) - 1.5f;
            float b = H - Mathf.Pow((shortNormalCorrectedResistivity + 11) / 0.65f, 1 / 6.05f) - 0.1f;
            float SPcor = (t / b) + 0.95f;
            SSP = SP * SPcor;
        }
        else
            SSP = SP;
        float Rwe = rmff * Mathf.Pow(10, SSP / (60 + (0.133f * Tf)));
        float top = Rwe + (0.131f * Mathf.Pow(10, 1 / Mathf.Log10(Tf / 19.9f) - 2));
        float bottom = (-0.5f * Rwe) + Mathf.Pow(10, 0.0426f / Mathf.Log10(Tf / 50.8f));
        Rw = top / bottom;
        Vsh = 1 - PSP / SSP;
        var entry = new Dictionary<string, float>
        {
            ["Tf"] = Tf,
            ["Rw"] = Rw,
            ["Vsh"] = Vsh
        };
        answers.Add(entry);
    }
    private void SetAndCalculateValuesFromData()
    {
        answers.Clear();
        if (data.Count == 0)
        {
            warningPanel.SetActive(true);
            warningText.text = "Input Error!" + System.Environment.NewLine + "Try exporting an empty csv to add your data.";
        }
        for (var i = 0; i < data.Count; i++)
        {
            try
            {
                BHT = data[i]["BHT"];
                Tms = data[i]["Tms"];
                Td = data[i]["Td"];
                D = data[i]["D"];
                Rmf = data[i]["Rmf"];
                Ri = data[i]["Ri"];
                Rm = data[i]["Rm"];
                H = data[i]["H"];
                PSP = data[i]["PSP"];
                SP = data[i]["SP"];
                Calc();
                GameObject x = Instantiate(dataPanelPrefab, outputContentPanel.transform);
                x.GetComponent<DataPanel>().SetText(BHT, Tms, Td, D, Ri, Rmf, Rm, H, PSP, SP, Tf, Rw, Vsh);
                outputContentPanel.GetComponent<RectTransform>().sizeDelta = new(outputContentPanel.GetComponent<RectTransform>().sizeDelta.x,
                    outputContentPanel.transform.childCount * 40);
            }
            catch
            {
                warningPanel.SetActive(true);
                warningText.text = "Input Error!" + System.Environment.NewLine + "Try exporting an empty csv to add your data.";
            }
        }
    }

    public void ClearData()
    {
        data.Clear();
        answers.Clear();
        if (outputContentPanel.transform.childCount == 0)
            return;
        for (int i = outputContentPanel.transform.childCount - 1; i >= 0; i--)
            Destroy(outputContentPanel.transform.GetChild(i).gameObject);
    }

    private string ToCSV()
    {
        var sb = new StringBuilder("BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh");
        for (var i = 0; i < data.Count; i++)
        {
            sb.Append('\n').Append(data[i]["BHT"].ToString()).Append(',').Append(data[i]["Tms"].ToString()).Append(',').Append(data[i]["Td"].ToString())
                .Append(',').Append(data[i]["D"].ToString()).Append(',').Append(data[i]["Rmf"].ToString()).Append(',').Append(data[i]["Ri"].ToString())
                .Append(',').Append(data[i]["Rm"].ToString()).Append(',').Append(data[i]["H"].ToString()).Append(',').Append(data[i]["PSP"].ToString())
                .Append(',').Append(data[i]["SP"].ToString()).Append(',').Append(answers[i]["Tf"].ToString()).Append(',').Append(answers[i]["Rw"].ToString())
                .Append(',').Append(answers[i]["Vsh"].ToString());
        }
        return sb.ToString();
    }
    private void SetValues()
    {
        BHT = float.Parse(BHTField.text);
        Tms = float.Parse(TmsField.text);
        Td = float.Parse(TdField.text);
        D = float.Parse(DField.text);
        Rmf = float.Parse(RmfField.text);
        Ri = float.Parse(RiField.text);
        Rm = float.Parse(RmField.text);
        H = float.Parse(HField.text);
        PSP = float.Parse(PSPField.text);
        SP = float.Parse(SPField.text);
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
    }
}
