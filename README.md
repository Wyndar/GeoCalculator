# GeoCalculator

GeoCalculator is a standalone Unity desktop application for SP-log based petrophysical calculations. It is built to reduce repetitive manual chart work by turning the workflow into direct numeric calculation with both single-entry and bulk file-based input.

The current application outputs:

- `Tf` - formation temperature at depth
- `Rw` - formation water resistivity
- `Vsh` - volume of shale

## Downloads

Packaged downloads are distributed through GitHub Releases.

Available now:

- `GeoCalculator-Windows.zip`
- `GeoCalculator-Linux.zip`
- `GeoCalculator-macOS.zip`
- `GeoCalculator-sample-data.zip`
- `SHA256SUMS.txt`

The sample data archive is produced from the tracked [sample-data](sample-data/) folder.

## Background

This software is associated with the paper:

[GEOCALCULATOR, AN APPLICATION SOFTWARE FOR THE DETERMINATION OF VOLUME OF SHALE AND FORMATION WATER RESISTIVITY FROM SP LOGS](https://www.scirj.org/apr-2025-paper.php?rp=P04251017)

Authors:

- Benson Akinbode Olisa
- Adedamola Bill Folaranmi

Publication:

- Scientific Research Journal (SCIRJ), Volume XIII, Issue IV, April 2025
- ISSN `2201-2796`
- DOI `10.31364/SCIRJ/v13.i04.2025.P04251017`

The paper describes the motivation for the software: reducing ambiguity, turnaround time, and manual error in SP-log interpretation workflows that depend on multiple petrophysical corrections and chart lookups.

Note: the paper discusses the broader formation-evaluation workflow, including hydrocarbon saturation derived from SP-log analysis. The current application in this repository directly outputs `Tf`, `Rw`, and `Vsh`.

## Features

- Single-record calculation through a form-based UI
- Bulk import from `CSV`, `TSV`, `JSON`, and `XLSX`
- Bulk export to `CSV`, `TSV`, `JSON`, and `XLSX`
- Save to the current opened or previously saved file path
- Save As with explicit output-type selection
- Keyboard shortcuts for common file and view actions
- Cross-platform file picking through StandaloneFileBrowser

## Required Inputs

For each calculation, the application expects these inputs:

- `BHT` - Bottom Hole Temperature
- `Tms` - Total Surface Temperature
- `Td` - Total Depth
- `D` - Depth to the clean sand
- `Ri` - Resistivity of the Short Normal
- `Rmf` - Resistivity of the mud filtrate
- `Rm` - Resistivity of the mud
- `H` - Thickness of the clean sand
- `PSP` - Pseudo Static Potential / Shale SP
- `SP` - SP of clean sand

Default units:

- Temperature in Fahrenheit
- Distance in feet
- Resistivity in ohm-metre
- SP values in millivolts

## File Formats

Delimited-text import expects a header row with these columns:

```csv
BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP
```

Delimited-text export includes both inputs and computed values:

```csv
BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh
```

Supported formats:

- `CSV` for spreadsheet and general interchange
- `TSV` for tab-delimited text workflows
- `JSON` for structured interchange and automation
- `XLSX` for native spreadsheet workflows

JSON import accepts either:

- a single record object
- an array of record objects
- an object containing a `records` array

## App Workflow

- `Single Input` is used for one calculation entered directly in the UI
- `Bulk Input` is used to open a supported data file and calculate multiple rows at once
- `Open` imports data from `CSV`, `TSV`, `JSON`, or `XLSX`
- `Save` writes back to the current file path when one exists
- `Save As` always prompts for an output type and destination

Keyboard shortcuts:

- `Ctrl+O` Open
- `Ctrl+S` Save
- `Ctrl+Shift+S` Save As
- `Shift+S` Single Input
- `Shift+B` Bulk Input
- `Shift+C` Clear

## Sample Data

Tracked sample files live under [sample-data](sample-data/):

- [sample-data/csv](sample-data/csv/)
- [sample-data/tsv](sample-data/tsv/)
- [sample-data/json](sample-data/json/)
- [sample-data/xlsx](sample-data/xlsx/)

## Development Notes

- Engine: Unity `6000.3.10f1`
- Main scene: [Assets/Scenes/Well Log Calculator.unity](Assets/Scenes/Well%20Log%20Calculator.unity)

Core code locations:

- [Assets/Scripts/Core](Assets/Scripts/Core/)
- [Assets/Scripts/Data](Assets/Scripts/Data/)
- [Assets/Scripts/UI](Assets/Scripts/UI/)

Primary application flow:

- [Assets/Scripts/UI/App/ApplicationManager.cs](Assets/Scripts/UI/App/ApplicationManager.cs)
- [Assets/Scripts/UI/App/ApplicationFileWorkflow.cs](Assets/Scripts/UI/App/ApplicationFileWorkflow.cs)
- [Assets/Scripts/UI/App/ApplicationOverlayController.cs](Assets/Scripts/UI/App/ApplicationOverlayController.cs)
- [Assets/Scripts/UI/App/ApplicationInputController.cs](Assets/Scripts/UI/App/ApplicationInputController.cs)


## Citation

If you reference the software or its research context, cite the paper:

Benson Akinbode Olisa, Adedamola Bill Folaranmi, `GEOCALCULATOR, AN APPLICATION SOFTWARE FOR THE DETERMINATION OF VOLUME OF SHALE AND FORMATION WATER RESISTIVITY FROM SP LOGS`, Scientific Research Journal (SCIRJ), Volume XIII, Issue IV, April 2025, pp. 1-18. DOI: `10.31364/SCIRJ/v13.i04.2025.P04251017`
