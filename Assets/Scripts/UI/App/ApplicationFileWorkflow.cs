using SFB;
using System;
using System.Collections.Generic;
using System.IO;
// ReSharper disable InconsistentNaming

public sealed class ApplicationFileWorkflow
{
    private enum SaveRequestKind
    {
        None,
        Save,
        SaveAs
    }

    private readonly ApplicationOverlayController overlayController;
    private readonly Func<CalculationExportSnapshot> exportSnapshotProvider;
    private readonly Action<List<CalculationInput>, string> importedDataHandler;
    private readonly string emptyDataWarningMessage =
        "There is no data in the file." + Environment.NewLine + "A blank template will be exported.";

    private SaveRequestKind pendingSaveRequest = SaveRequestKind.None;
    private string currentDataFilePath;

    public ApplicationFileWorkflow(
        ApplicationOverlayController overlayController,
        Func<CalculationExportSnapshot> exportSnapshotProvider,
        Action<List<CalculationInput>, string> importedDataHandler)
    {
        this.overlayController = overlayController;
        this.exportSnapshotProvider = exportSnapshotProvider;
        this.importedDataHandler = importedDataHandler;
    }

    public void OpenDataFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Import Data", "", DataFileSerializer.OpenFilters, false);
        if (paths.Length <= 0)
            return;

        try
        {
            var importedData = DataFileSerializer.Read(paths[0]);
            currentDataFilePath = paths[0];
            importedDataHandler(importedData, paths[0]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            overlayController.ShowWarning("Input Error!" + Environment.NewLine + ex.Message);
        }
    }

    public void SaveDataFile()
    {
        pendingSaveRequest = SaveRequestKind.Save;
        if (exportSnapshotProvider().HasExportableData)
        {
            ContinuePendingSaveRequest();
            return;
        }

        overlayController.ShowEmptyExportWarning(emptyDataWarningMessage, ContinuePendingSaveRequest);
    }

    public void SaveDataFileAs()
    {
        pendingSaveRequest = SaveRequestKind.SaveAs;
        if (exportSnapshotProvider().HasExportableData)
        {
            ContinuePendingSaveRequest();
            return;
        }

        overlayController.ShowEmptyExportWarning(emptyDataWarningMessage, ContinuePendingSaveRequest);
    }

    private void ContinuePendingSaveRequest()
    {
        switch (pendingSaveRequest)
        {
            case SaveRequestKind.Save when !string.IsNullOrWhiteSpace(currentDataFilePath):
                SaveDataToPath(currentDataFilePath, null, exportSnapshotProvider());
                return;
            case SaveRequestKind.Save:
            case SaveRequestKind.SaveAs:
                overlayController.ShowSaveAsPopup(
                    GetDefaultSaveExtension(),
                    ConfirmSaveAsSelection,
                    CancelPendingSaveRequest);
                return;
            default:
                return;
        }
    }

    private void ConfirmSaveAsSelection(string extension)
    {
        var path = PromptForSavePath(extension);
        if (string.IsNullOrEmpty(path))
        {
            CancelPendingSaveRequest();
            return;
        }

        SaveDataToPath(path, extension, exportSnapshotProvider());
    }

    private string PromptForSavePath(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("A save extension is required.", nameof(extension));

        var initialDirectory = string.IsNullOrWhiteSpace(currentDataFilePath)
            ? ""
            : Path.GetDirectoryName(currentDataFilePath) ?? "";
        var defaultName = string.IsNullOrWhiteSpace(currentDataFilePath)
            ? "data"
            : Path.GetFileNameWithoutExtension(currentDataFilePath);

        return StandaloneFileBrowser.SaveFilePanel("Save Data", initialDirectory, defaultName, extension.TrimStart('.'));
    }

    private void SaveDataToPath(string path, string forcedExtension, CalculationExportSnapshot snapshot)
    {
        try
        {
            var normalizedPath = NormalizeSavePath(path, forcedExtension);
            DataFileSerializer.Write(normalizedPath, snapshot.BulkInputs, snapshot.BulkResults, snapshot.SingleInput, snapshot.SingleResult);
            currentDataFilePath = normalizedPath;
            pendingSaveRequest = SaveRequestKind.None;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            overlayController.ShowWarning("Save Error!" + Environment.NewLine + ex.Message);
        }
    }

    private string GetDefaultSaveExtension()
    {
        return string.IsNullOrWhiteSpace(currentDataFilePath)
            ? DataFileSerializer.DefaultExtension.TrimStart('.')
            : Path.GetExtension(currentDataFilePath).TrimStart('.').ToLowerInvariant();
    }

    private static string NormalizeSavePath(string path, string forcedExtension = null)
    {
        if (string.IsNullOrWhiteSpace(forcedExtension))
            return string.IsNullOrEmpty(Path.GetExtension(path))
                ? path + DataFileSerializer.DefaultExtension
                : path;

        var normalizedExtension = forcedExtension.StartsWith(".") ? forcedExtension : "." + forcedExtension;
        return string.IsNullOrEmpty(Path.GetExtension(path))
            ? path + normalizedExtension
            : Path.ChangeExtension(path, normalizedExtension);
    }

    private void CancelPendingSaveRequest() => pendingSaveRequest = SaveRequestKind.None;
}
