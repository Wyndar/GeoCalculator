# GeoCalculator

GeoCalculator is a standalone Unity application for SP-log based petrophysical calculations. It is designed to speed up repetitive formation-evaluation work by replacing manual chart-based workflows with direct numeric calculation and CSV import/export.

In its current form, the application calculates:
- `Tf` - formation temperature at depth
- `Rw` - formation water resistivity
- `Vsh` - volume of shale

The application supports both:
- single-value input through the UI
- bulk calculation from CSV files

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

- Standalone desktop Unity application
- Single-record calculation through a form-based UI
- Bulk import from CSV
- Bulk export to CSV
- Cross-platform file picking through StandaloneFileBrowser
- Input validation and clearer row-level CSV error reporting

## Required Inputs

For each calculation, the application expects these inputs:

- `BHT`
- `Tms`
- `Td`
- `D`
- `Ri`
- `Rmf`
- `Rm`
- `H`
- `PSP`
- `SP`

## CSV Format

The importer expects a header row with these columns:

```csv
BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP
```

The exported output includes both inputs and computed values:

```csv
BHT,Tms,Td,D,Ri,Rmf,Rm,H,PSP,SP,Tf,Rw,Vsh
```

## Project Status

This repository has been updated to Unity 6 and lightly modernized from its earlier implementation. Recent cleanup work includes:

- Unity 6 project upgrade
- normalized TextMesh Pro assets and project hygiene
- safer numeric parsing
- typed CSV import models instead of string-keyed dictionaries
- direct file loading instead of deprecated `WWW`
- clearer bulk-import error handling

## Development Notes

- Engine: Unity `6000.3.10f1`
- Main scene: [Assets/Scenes/Well Log Calculator.unity](Assets/Scenes/Well%20Log%20Calculator.unity)
- Core scripts:
  - [Assets/ApplicationManager.cs](Assets/ApplicationManager.cs)
  - [Assets/CSVReader.cs](Assets/CSVReader.cs)
  - [Assets/DataPanel.cs](Assets/DataPanel.cs)
  - [Assets/CalculationModels.cs](Assets/CalculationModels.cs)

## Citation

If you reference the software or its research context, cite the paper:

Benson Akinbode Olisa, Adedamola Bill Folaranmi, `GEOCALCULATOR, AN APPLICATION SOFTWARE FOR THE DETERMINATION OF VOLUME OF SHALE AND FORMATION WATER RESISTIVITY FROM SP LOGS`, Scientific Research Journal (SCIRJ), Volume XIII, Issue IV, April 2025, pp. 1-18. DOI: `10.31364/SCIRJ/v13.i04.2025.P04251017`
