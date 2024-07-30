using SFB;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Text;
using UnityEngine.Networking;

public class ApplicationManager : MonoBehaviour

    //note F = Formation Factor

    //F = c ÷ ( por ^ m )

    //c = cementation factor
    //m = cementation exponent
    //F = 1 ÷ ( por ^ 2 ) (for limestones)
    //F = 0.81 ÷ ( por ^ 2 ) or 0.62 ÷ ( por ^ 2.15 ) (for sands)
    //Sw = Water Saturation
    //Sw = ( F * Rw ÷ Rt ) ^ 1/n
    //n = saturation exponent
    //Sw = [ ( c * Rw ) ÷ ( por^m * Rt ) ] ^ 1/nSw = Water Saturation
    //Sw = ( F * Rw ÷ Rt ) ^ 1/n
    //n = saturation exponent
    //Sw = [ ( c * Rw ) ÷ ( por^m * Rt ) ] ^ 1/n
{
    private readonly ExtensionFilter[] dataExtensions = new[] { new ExtensionFilter("CSVs", "csv") };
    private readonly string warningString = "The following parameters have not been set: ";
    [SerializeField] private TMP_InputField BHTField, TmsField, TdField, DField, RiField, RmfField, RmField, HField, PSPField, SPField;
    //optional parameter porosity
    [SerializeField] private TMP_InputField PField;
    [SerializeField] private TMP_InputField TfField, RwField, VshField, SwField, ShField;
    [SerializeField] private Button runButton, clearButton;
    [SerializeField] private GameObject warningPanel, outputContentPanel, singleInputPanel, bulkInputPanel;
    [SerializeField] private GameObject dataPanelPrefab;
    [SerializeField] private TMP_Text warningText, switchButtonText;


    private List<Dictionary<string, float>> data = new();
    private List<Dictionary<string, float>> answers;
    //calc params
    private float BHT, Tms, Td, D, Ri, Rmf, Rm, H, PSP, SP;
    //calc answers
    private float Tf, Rw, Vsh, Sw, Sh;
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

    public void WarningClear() => warningPanel.SetActive(false);
    public void GetDataFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Title", "", dataExtensions, false);
        if (paths.Length > 0)
        {
            StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        }
    }
    private IEnumerator OutputRoutine(string url)
    {
        var loader = new WWW (url);
        yield return loader;
        data = CSVReader.Read(loader.text);
        SetAndCalculateValuesFromData();
    }

    public void SaveDataFile()
    {
        string saveData = ToCSV();
        var path = StandaloneFileBrowser.SaveFilePanel("CSV", "", "data", "csv");
        if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, saveData);
    }

    public void SwitchInput()
    {
        if(bulkInputPanel.activeInHierarchy)
        {
            bulkInputPanel.SetActive(false);
            singleInputPanel.SetActive(true);
            switchButtonText.text = "Bulk Input";
            return;
        }
        bulkInputPanel.SetActive(true);
        singleInputPanel.SetActive(false);
        switchButtonText.text = "Single Input";
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
        //float F = 1;
        Vsh = 1 - PSP / SSP;
        //Sw = Mathf.Pow((F * Rw) / Rt, 0.5f);
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
        answers = new();
        for (var i = 0; i < data.Count; i++)
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
