using SFB;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
// ReSharper disable InconsistentNaming

public static class DataFileSerializer
{
    private const string CsvHeader = "BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh";
    private const string DefaultExtension = ".csv";

    public static readonly ExtensionFilter[] OpenFilters =
    {
        new("Supported Data", "csv", "tsv", "json", "xlsx"),
        new("Delimited Text", "csv", "tsv"),
        new("JSON", "json"),
        new("Excel Workbook", "xlsx")
    };

    public static List<CalculationInput> Read(string path)
    {
        var normalizedPath = NormalizePath(path);
        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        var content = File.ReadAllText(normalizedPath);

        return extension switch
        {
            ".csv" => CSVReader.Read(content, ','),
            ".tsv" => CSVReader.Read(content, '\t'),
            ".json" => ReadJson(content),
            ".xlsx" => ReadXlsx(normalizedPath),
            _ => throw new FormatException($"Unsupported file type '{extension}'. Supported types: csv, tsv, json, xlsx.")
        };
    }

    public static void Write(string path, IReadOnlyList<CalculationInput> bulkInputs, 
        IReadOnlyList<CalculationResult> bulkResults, CalculationInput singleInput, CalculationResult singleResult)
    {
        var normalizedPath = NormalizePath(path);
        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();

        switch (extension)
        {
            case ".csv":
                File.WriteAllText(normalizedPath, BuildDelimitedContent(',', bulkInputs, bulkResults, 
                    singleInput, singleResult));
                return;
            case ".tsv":
                File.WriteAllText(normalizedPath, BuildDelimitedContent('\t', bulkInputs, bulkResults,
                    singleInput, singleResult));
                return;
            case ".json":
                File.WriteAllText(normalizedPath, BuildJsonContent(bulkInputs, bulkResults, singleInput, singleResult));
                return;
            case ".xlsx":
                WriteXlsx(normalizedPath, bulkInputs, bulkResults, singleInput, singleResult);
                return;
            default:
                throw new FormatException($"Unsupported file type '{extension}'. Supported types: csv, tsv, json, xlsx.");
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new FormatException("A file path is required.");

        return string.IsNullOrEmpty(Path.GetExtension(path))
            ? path + DefaultExtension
            : path;
    }

    private static string BuildDelimitedContent(char delimiter, IReadOnlyList<CalculationInput> bulkInputs, 
        IReadOnlyList<CalculationResult> bulkResults, CalculationInput singleInput, CalculationResult singleResult)
    {
        var sb = new StringBuilder(CsvHeader.Replace(',', delimiter));

        if (bulkInputs.Count > 0 && bulkResults.Count == bulkInputs.Count)
        {
            for (var i = 0; i < bulkInputs.Count; i++)
                AppendDelimitedRow(sb, delimiter, bulkInputs[i], bulkResults[i]);

            return sb.ToString();
        }

        if (singleInput == null || singleResult == null) return sb.ToString();
        AppendDelimitedRow(sb, delimiter, singleInput, singleResult);
        return sb.ToString();

    }

    private static void AppendDelimitedRow(StringBuilder sb, char delimiter, CalculationInput input, CalculationResult result)
    {
        sb.Append('\n').Append(FormatFloat(input.BHT)).Append(delimiter).Append(FormatFloat(input.Tms)).Append(delimiter)
            .Append(FormatFloat(input.Td)).Append(delimiter).Append(FormatFloat(input.D)).Append(delimiter)
            .Append(FormatFloat(input.Ri)).Append(delimiter).Append(FormatFloat(input.Rmf)).Append(delimiter)
            .Append(FormatFloat(input.Rm)).Append(delimiter).Append(FormatFloat(input.H)).Append(delimiter)
            .Append(FormatFloat(input.PSP)).Append(delimiter).Append(FormatFloat(input.SP)).Append(delimiter)
            .Append(FormatFloat(result.Tf)).Append(delimiter).Append(FormatFloat(result.Rw)).Append(delimiter)
            .Append(FormatFloat(result.Vsh));
    }

    private static string BuildJsonContent(IReadOnlyList<CalculationInput> bulkInputs, IReadOnlyList<CalculationResult> bulkResults,
        CalculationInput singleInput, CalculationResult singleResult)
    {
        var file = new CalculationJsonOutputFile
        {
            records = BuildJsonOutputRecords(bulkInputs, bulkResults, singleInput, singleResult)
        };

        return JsonUtility.ToJson(file, true);
    }

    private static List<CalculationInput> ReadXlsx(string path)
    {
        EnsureEpplusLicenseContext();

        using var package = new ExcelPackage(new FileInfo(path));
        var worksheet = package.Workbook.Worksheets.Count > 0 ? package.Workbook.Worksheets[0] : null;
        if (worksheet?.Dimension == null)
            return new List<CalculationInput>();

        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var column = worksheet.Dimension.Start.Column; column <= worksheet.Dimension.End.Column; column++)
        {
            var header = worksheet.Cells[1, column].Text?.Trim();
            if (!string.IsNullOrEmpty(header))
                headerLookup[header] = column;
        }

        ValidateRequiredHeaders(headerLookup);

        var records = new List<CalculationInput>();
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            if (IsWorksheetRowEmpty(worksheet, row, headerLookup))
                continue;

            records.Add(new CalculationInput(
                ReadWorksheetFloat(worksheet, row, headerLookup, "BHT"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "Tms"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "Td"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "D"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "Ri"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "Rmf"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "Rm"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "H"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "PSP"),
                ReadWorksheetFloat(worksheet, row, headerLookup, "SP")));
        }

        return records;
    }

    private static void WriteXlsx(string path, IReadOnlyList<CalculationInput> bulkInputs, IReadOnlyList<CalculationResult> bulkResults,
        CalculationInput singleInput, CalculationResult singleResult)
    {
        EnsureEpplusLicenseContext();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("GeoCalculator");
        WriteWorksheetHeaders(worksheet);

        if (bulkInputs.Count > 0 && bulkResults.Count == bulkInputs.Count)
        {
            for (var i = 0; i < bulkInputs.Count; i++)
                WriteWorksheetRow(worksheet, i + 2, bulkInputs[i], bulkResults[i]);
        }
        else if (singleInput != null && singleResult != null)
        {
            WriteWorksheetRow(worksheet, 2, singleInput, singleResult);
        }

        package.SaveAs(new FileInfo(path));
    }

    private static CalculationJsonOutputRecord[] BuildJsonOutputRecords(IReadOnlyList<CalculationInput> bulkInputs, 
        IReadOnlyList<CalculationResult> bulkResults, CalculationInput singleInput, CalculationResult singleResult)
    {
        if (bulkInputs.Count <= 0 || bulkResults.Count != bulkInputs.Count)
            return singleInput != null && singleResult != null
                ? new[] { new CalculationJsonOutputRecord(singleInput, singleResult) }
                : Array.Empty<CalculationJsonOutputRecord>();

        var records = new CalculationJsonOutputRecord[bulkInputs.Count];
        for (var i = 0; i < bulkInputs.Count; i++)
            records[i] = new CalculationJsonOutputRecord(bulkInputs[i], bulkResults[i]);

        return records;
    }

    private static List<CalculationInput> ReadJson(string content)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return new List<CalculationInput>();

        if (trimmed.StartsWith("[", StringComparison.Ordinal))
            trimmed = "{\"records\":" + trimmed + "}";

        if (trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.Contains("\"records\"",
                StringComparison.Ordinal))
            trimmed = "{\"records\":[" + trimmed + "]}";

        ValidateJsonFieldNames(trimmed);

        var file = JsonUtility.FromJson<CalculationJsonInputFile>(trimmed);
        if (file?.records == null)
            throw new FormatException("Invalid JSON format. Expected a record object, a records array," +
                                      " or an object containing 'records'.");

        var inputs = new List<CalculationInput>(file.records.Length);
        foreach (var record in file.records)
            inputs.Add(record.ToInput());

        return inputs;
    }

    private static void ValidateJsonFieldNames(string json)
    {
        foreach (var requiredHeader in CSVReader.RequiredHeaders)
            if (!json.Contains($"\"{requiredHeader}\"", StringComparison.Ordinal))
                throw new FormatException($"Missing required field '{requiredHeader}' in JSON data.");
    }

    private static string FormatFloat(float value) => value.ToString("G", CultureInfo.InvariantCulture);

    private static void EnsureEpplusLicenseContext()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private static void ValidateRequiredHeaders(IReadOnlyDictionary<string, int> headerLookup)
    {
        foreach (var requiredHeader in CSVReader.RequiredHeaders)
        {
            if (!headerLookup.ContainsKey(requiredHeader))
                throw new FormatException($"Missing required column '{requiredHeader}'.");
        }
    }

    private static bool IsWorksheetRowEmpty(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerLookup)
    {
        foreach (var requiredHeader in CSVReader.RequiredHeaders)
        {
            if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, headerLookup[requiredHeader]].Text))
                return false;
        }

        return true;
    }

    private static float ReadWorksheetFloat(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerLookup, string columnName)
    {
        var cell = worksheet.Cells[row, headerLookup[columnName]];
        var value = cell.Value;

        if (value == null)
            throw new FormatException($"Missing value for column '{columnName}' on row {row}.");

        switch (value)
        {
            case double doubleValue:
                return (float)doubleValue;
            case float floatValue:
                return floatValue;
            case int intValue:
                return intValue;
            case decimal decimalValue:
                return (float)decimalValue;
            case long longValue:
                return longValue;
        }

        var text = cell.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new FormatException($"Missing value for column '{columnName}' on row {row}.");

        if (float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariantValue))
            return invariantValue;

        if (float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var cultureValue))
            return cultureValue;

        throw new FormatException($"Invalid numeric value '{text}' in column '{columnName}' on row {row}.");
    }

    private static void WriteWorksheetHeaders(ExcelWorksheet worksheet)
    {
        var headers = CsvHeader.Split(',');
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cells[1, i + 1].Value = headers[i];
    }

    private static void WriteWorksheetRow(ExcelWorksheet worksheet, int row, CalculationInput input, CalculationResult result)
    {
        worksheet.Cells[row, 1].Value = input.BHT;
        worksheet.Cells[row, 2].Value = input.Tms;
        worksheet.Cells[row, 3].Value = input.Td;
        worksheet.Cells[row, 4].Value = input.D;
        worksheet.Cells[row, 5].Value = input.Ri;
        worksheet.Cells[row, 6].Value = input.Rmf;
        worksheet.Cells[row, 7].Value = input.Rm;
        worksheet.Cells[row, 8].Value = input.H;
        worksheet.Cells[row, 9].Value = input.PSP;
        worksheet.Cells[row, 10].Value = input.SP;
        worksheet.Cells[row, 11].Value = result.Tf;
        worksheet.Cells[row, 12].Value = result.Rw;
        worksheet.Cells[row, 13].Value = result.Vsh;
    }

    [Serializable]
    private sealed class CalculationJsonInputFile
    {
        public CalculationJsonInputRecord[] records;
    }

    [Serializable]
    private sealed class CalculationJsonOutputFile
    {
        // ReSharper disable once NotAccessedField.Local
        public CalculationJsonOutputRecord[] records;
    }

    [Serializable]
    private sealed class CalculationJsonInputRecord
    {
        public float BHT;
        public float Tms;
        public float Td;
        public float D;
        public float Ri;
        public float Rmf;
        public float Rm;
        public float H;
        public float PSP;
        public float SP;

        public CalculationInput ToInput() => new(BHT, Tms, Td, D, Ri, Rmf, Rm, H, PSP, SP);
    }

    [Serializable]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private sealed class CalculationJsonOutputRecord
    {
        public float BHT;
        public float Tms;
        public float Td;
        public float D;
        public float Ri;
        public float Rmf;
        public float Rm;
        public float H;
        public float PSP;
        public float SP;
        public float Tf;
        public float Rw;
        public float Vsh;

        public CalculationJsonOutputRecord()
        {
        }

        public CalculationJsonOutputRecord(CalculationInput input, CalculationResult result)
        {
            BHT = input.BHT;
            Tms = input.Tms;
            Td = input.Td;
            D = input.D;
            Ri = input.Ri;
            Rmf = input.Rmf;
            Rm = input.Rm;
            H = input.H;
            PSP = input.PSP;
            SP = input.SP;
            Tf = result.Tf;
            Rw = result.Rw;
            Vsh = result.Vsh;
        }
    }
}
