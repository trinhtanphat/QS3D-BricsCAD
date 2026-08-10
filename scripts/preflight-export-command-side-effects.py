#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = (
    (
        "Curtain",
        ROOT / "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs",
        "panels = QuantityReportMath.AddCount(panels, row.PanelCount);",
        "CurtainWallXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Door/Opening",
        ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs",
        "count = QuantityReportMath.AddCount(count, row.Count);",
        "DoorOpeningXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Material",
        ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs",
        "elements = QuantityReportMath.AddCount(elements, row.ElementCount);",
        "MaterialUsageXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Room finish",
        ROOT / "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs",
        "primary = QuantityReportMath.Add(primary, row.PrimaryQuantity, \"HT_Phòng export primary quantity\");",
        "RoomFinishXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
)

errors = []
for label, path, aggregate, export, finalize in CASES:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    dialog = "if (dialog.ShowDialog() != true) return;"
    aggregate_pos = text.find(aggregate)
    dialog_pos = text.find(dialog)
    export_pos = text.find(export)
    finalize_pos = text.find(finalize)

    for token, pos in ((aggregate, aggregate_pos), (dialog, dialog_pos), (export, export_pos), (finalize, finalize_pos)):
        if pos < 0:
            errors.append(label + " missing export-boundary token: " + token)

    if min(aggregate_pos, dialog_pos, export_pos, finalize_pos) >= 0:
        if not aggregate_pos < dialog_pos < export_pos < finalize_pos:
            errors.append(label + " must validate read-only aggregates before dialog/export and finalize UI only after export")
        between_export_and_finalize = text[export_pos + len(export):finalize_pos]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append(label + " must not perform fallible UI work between persistent export and FinalizeUi")

    if "Cảnh báo UI sau export" not in text:
        errors.append(label + " missing best-effort post-export UI warning boundary")

bbs_path = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
if not bbs_path.is_file():
    errors.append("missing " + str(bbs_path.relative_to(ROOT)))
else:
    text = bbs_path.read_text(encoding="utf-8")
    dialog = "if (dialog.ShowDialog() != true) return;"
    regenerate = "new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);"
    aggregate = "totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, \"BBS CSV total weight\");"
    export = "RebarCsvExporter.Export(dialog.FileName, rows);"
    finalize = "FinalizeUi(document, status);"
    positions = {
        dialog: text.find(dialog),
        regenerate: text.find(regenerate),
        aggregate: text.find(aggregate),
        export: text.find(export),
        finalize: text.find(finalize),
    }
    for token, pos in positions.items():
        if pos < 0:
            errors.append("BBS CSV missing export-boundary token: " + token)
    if min(positions.values()) >= 0:
        if not positions[dialog] < positions[regenerate] < positions[aggregate] < positions[export] < positions[finalize]:
            errors.append("BBS CSV must confirm export first, then regenerate/validate, export, and finalize UI")
        before_dialog = text[:positions[dialog]]
        if "RegenerateDirty(project)" in before_dialog:
            errors.append("BBS CSV Cancel path must not regenerate semantic state before the save dialog is confirmed")
        between_export_and_finalize = text[positions[export] + len(export):positions[finalize]]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append("BBS CSV must not perform fallible UI work between persistent export and FinalizeUi")
    if "Cảnh báo UI sau export" not in text:
        errors.append("BBS CSV missing best-effort post-export UI warning boundary")

if errors:
    print("QS3D export command side-effect preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: read-only exporters validate aggregates before prompting; BBS CSV confirms export before semantic regeneration; all exporters finalize UI only after persistent export.")
