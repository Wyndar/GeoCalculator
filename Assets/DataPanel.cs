using TMPro;
using UnityEngine;

public class DataPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text BHTText, TmsText, TdText, DText, RiText, RmfText, RmText, HText, PSPText, SPText, TfText, RwText, VshText;
    public void SetText(float BHT, float Tms, float Td, float D, float Ri, float Rmf, float Rm, float H, float PSP, float SP, float Tf, float Rw, float Vsh)
    {
        BHTText.text = BHT.ToString();
        TmsText.text = Tms.ToString();
        TdText.text = Td.ToString();
        DText.text = D.ToString();
        RiText.text = Ri.ToString();
        RmfText.text = Rmf.ToString();
        RmText.text = Rm.ToString();
        HText.text = H.ToString();
        PSPText.text = PSP.ToString();
        SPText.text = SP.ToString();
        TfText.text = Tf.ToString();
        RwText.text = Rw.ToString();
        VshText.text = Vsh.ToString();
    }
}
