using System.Collections.Generic;

public sealed class CalculationExportSnapshot
{
    public CalculationExportSnapshot(
        IReadOnlyList<CalculationInput> bulkInputs,
        IReadOnlyList<CalculationResult> bulkResults,
        CalculationInput singleInput,
        CalculationResult singleResult)
    {
        BulkInputs = bulkInputs;
        BulkResults = bulkResults;
        SingleInput = singleInput;
        SingleResult = singleResult;
    }

    public IReadOnlyList<CalculationInput> BulkInputs { get; }
    public IReadOnlyList<CalculationResult> BulkResults { get; }
    public CalculationInput SingleInput { get; }
    public CalculationResult SingleResult { get; }

    public bool HasExportableData =>
        BulkInputs.Count > 0 && BulkResults.Count == BulkInputs.Count ||
        SingleInput != null && SingleResult != null;
}
