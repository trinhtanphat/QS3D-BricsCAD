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
    (
        "BBS CSV",
        ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
        "totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, \"BBS CSV total weight\");",
        "RebarCsvExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status);",
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
            errors.append(label + " must validate aggregates before dialog/export and finalize UI only after export")
        between_export_and_finalize = text[export_pos + len(export):finalize_pos]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append(label + " must not perform fallible UI work between persistent export and FinalizeUi")

    if "Cảnh báo UI sau export" not in text:
        errors.append(label + " missing best-effort post-export UI warning boundary")

if errors:
    print("QS3D export command side-effect preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: checked aggregates precede export side effects and post-export UI is best effort for Curtain, Door/Opening, Material, Room Finish, and BBS CSV commands.")
