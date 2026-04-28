using System;
using System.Globalization;
using UnityEngine;

public static class CalculationService
{
    public static CalculationResult Calculate(CalculationInput input)
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
        float ssp;
        if (shortNormalCorrectedResistivity > 5 && input.H is > 3 and < 50)
        {
            float t = Mathf.Pow(4 * (shortNormalCorrectedResistivity + 2f), 1 / 3.65f) - 1.5f;
            float b = input.H - Mathf.Pow((shortNormalCorrectedResistivity + 11) / 0.65f, 1 / 6.05f) - 0.1f;
            float spCorrection = (t / b) + 0.95f;
            ssp = input.SP * spCorrection;
        }
        else
        {
            ssp = input.SP;
        }

        if (Mathf.Approximately(ssp, 0f))
            throw new InvalidOperationException("SP correction produces zero SSP.");

        float rwe = rmff * Mathf.Pow(10, ssp / (60 + (0.133f * tf)));
        float top = rwe + (0.131f * Mathf.Pow(10, 1 / Mathf.Log10(tf / 19.9f) - 2));
        float bottom = (-0.5f * rwe) + Mathf.Pow(10, 0.0426f / Mathf.Log10(tf / 50.8f));
        if (Mathf.Approximately(bottom, 0f))
            throw new InvalidOperationException("Calculated Rw denominator is zero.");

        float rw = top / bottom;
        float vsh = 1 - input.PSP / ssp;

        if (!IsFinite(tf) || !IsFinite(rw) || !IsFinite(vsh))
            throw new InvalidOperationException("Calculation produced a non-finite result.");

        return new CalculationResult(tf, rw, vsh);
    }

    public static float ParseInputFloat(string value)
    {
        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;

        if (float.TryParse(value, styles, CultureInfo.CurrentCulture, out float currentCultureValue))
            return currentCultureValue;

        return float.TryParse(value, styles, CultureInfo.InvariantCulture, out float invariantValue)
            ? invariantValue
            : throw new FormatException($"Invalid numeric value: {value}");
    }

    public static string FormatFloat(float value) => value.ToString("G", CultureInfo.InvariantCulture);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
