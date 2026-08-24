# Excel company/user template export

Command: `QS3DEXCELTEMPLATE`

Lane: #3686 (`issue-3686`)

## Purpose

`QS3DEXCELTEMPLATE` writes QS3D's canonical quantity report rows into an existing `.xlsx` workbook template without introducing another quantity engine or an Office Interop dependency. The workbook renderer is `QS3D.Core.Export.QsWorkbookTemplateExporter`.

## Workflow

1. Choose scope: `Selection`, `Floor`, `Zone`, or `All`.
2. Choose report row mode: `Detail` or `Group`.
3. Choose mapping mode: `Default` or `Custom`.
4. Choose the source `.xlsx` template.
5. For `Custom`, choose a bounded JSON mapping file.
6. Choose a different `.xlsx` output path.
7. QS3D rechecks project identity/revision, regenerates a detached project snapshot, builds canonical quantity rows, validates every source CAD Handle is still live, then calls the Core template exporter.

Cancellation during any prompt/file dialog returns before `DrawingUnitWorkflow.EnsureResolved` and before any output replacement. The Core exporter writes through a temporary file and only replaces the requested destination after the generated XLSX passes package validation.

## Default mapping

The built-in safe/default contract targets worksheet `CHI_TIET`, starts at row `2`, reserves rows `2..5001`, and maps all `QsWorkbookTemplateField` values in enum order to columns `A..AC`. A company template whose data block or footer differs should use `Custom` mapping instead of relying on this default.

## Custom mapping JSON

The JSON file is limited to 64 KiB and may map each template field/Excel column at most once. Example: `samples/excel-template-mapping.example.json`.

```json
{
  "worksheet": "CHI_TIET",
  "firstDataRow": 2,
  "reservedDataRows": 5000,
  "mappings": [
    { "field": "Index", "column": "A" },
    { "field": "NetConcreteM3", "column": "I" },
    { "field": "ElementIds", "column": "L" },
    { "field": "SourceHandles", "column": "M" },
    { "field": "DrawingFingerprint", "column": "N" },
    { "field": "TraceKey", "column": "O" }
  ]
}
```

Valid `field` values are the public `QsWorkbookTemplateField` enum values. Column values use Excel `A..XFD` notation. `worksheet`, row bounds, duplicate fields/columns and mapped formula/merged-cell conflicts are validated fail-closed by the adapter/Core exporter.

## Provenance and safety

When mapped, `ElementIds`, `SourceHandles`, `DrawingFingerprint`, and `TraceKey` come from the canonical quantity row provenance. Before export, the adapter normalizes and resolves the complete source Handle set against the active DWG; any stale/missing source refuses the export.

The command never overwrites the source template in place. Unrelated workbook content outside mapped data cells is preserved by the Core renderer. No proprietary BLT template, binary, Office Interop assembly, license material, or customer workbook is committed to the repository.

## Runtime boundary

Source/static CI validates registration, command ordering, parser boundaries, canonical quantity calls, provenance/live-Handle checks, UI/Ribbon wiring, Core exporter use and V25/V26 shared-source parity. Interactive Windows file dialogs and licensed BricsCAD V25/V26 template rendering remain `LOCAL_ONLY` under #72.
