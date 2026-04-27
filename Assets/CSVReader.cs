using System;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public class CSVReader
{
    private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private static readonly char[] TRIM_CHARS = { '\"' };
    private static readonly string[] RequiredHeaders = { "BHT", "Tms", "Td", "D", "Ri", "Rmf", "Rm", "H", "PSP", "SP" };

    public static List<CalculationInput> Read(string file)
    {
        var list = new List<CalculationInput>();
        var lines = Regex.Split(file, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        var header = Regex.Split(lines[0], SPLIT_RE);
        var headerLookup = CreateHeaderLookup(header);

        ValidateHeaders(headerLookup);

        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            list.Add(new CalculationInput(
                ParseRequiredValue(values, headerLookup, "BHT", i + 1),
                ParseRequiredValue(values, headerLookup, "Tms", i + 1),
                ParseRequiredValue(values, headerLookup, "Td", i + 1),
                ParseRequiredValue(values, headerLookup, "D", i + 1),
                ParseRequiredValue(values, headerLookup, "Ri", i + 1),
                ParseRequiredValue(values, headerLookup, "Rmf", i + 1),
                ParseRequiredValue(values, headerLookup, "Rm", i + 1),
                ParseRequiredValue(values, headerLookup, "H", i + 1),
                ParseRequiredValue(values, headerLookup, "PSP", i + 1),
                ParseRequiredValue(values, headerLookup, "SP", i + 1)));
        }
        return list;
    }

    private static Dictionary<string, int> CreateHeaderLookup(string[] header)
    {
        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Length; i++)
        {
            var normalizedHeader = header[i].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Trim();
            if (!string.IsNullOrEmpty(normalizedHeader))
                headerLookup[normalizedHeader] = i;
        }

        return headerLookup;
    }

    private static void ValidateHeaders(Dictionary<string, int> headerLookup)
    {
        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headerLookup.ContainsKey(requiredHeader))
                throw new FormatException($"Missing required column '{requiredHeader}'.");
        }
    }

    private static float ParseRequiredValue(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headerLookup, string columnName, int rowNumber)
    {
        var columnIndex = headerLookup[columnName];
        if (columnIndex >= values.Count)
            throw new FormatException($"Missing value for column '{columnName}' on row {rowNumber + 1}.");

        var value = values[columnIndex].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            return intValue;

        if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
            return floatValue;

        throw new FormatException($"Invalid numeric value '{value}' in column '{columnName}' on row {rowNumber + 1}.");
    }
}
