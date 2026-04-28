using TMPro;
using System.Globalization;
using UnityEngine;
// ReSharper disable InconsistentNaming

public class DataPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text BHTText,
        TmsText,
        TdText,
        DText,
        RiText,
        RmfText,
        RmText,
        HText,
        PSPText,
        SPText,
        TfText,
        RwText,
        VshText;

    public void SetText(CalculationInput input, CalculationResult result)
    {
        BHTText.text = FormatFloat(input.BHT);
        TmsText.text = FormatFloat(input.Tms);
        TdText.text = FormatFloat(input.Td);
        DText.text = FormatFloat(input.D);
        RiText.text = FormatFloat(input.Ri);
        RmfText.text = FormatFloat(input.Rmf);
        RmText.text = FormatFloat(input.Rm);
        HText.text = FormatFloat(input.H);
        PSPText.text = FormatFloat(input.PSP);
        SPText.text = FormatFloat(input.SP);
        TfText.text = FormatFloat(result.Tf);
        RwText.text = FormatFloat(result.Rw);
        VshText.text = FormatFloat(result.Vsh);
    }

    private static string FormatFloat(float value) => value.ToString("G", CultureInfo.InvariantCulture);
}
