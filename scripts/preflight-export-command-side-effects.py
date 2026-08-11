#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = (
    (
        "Curtain",
        ROOT / "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs",
        "CurtainWallScheduleBuilder.Build(project)",
        "panels = QuantityReportMath.AddCount(panels, row.PanelCount);",
        "CurtainWallXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Door/Opening",
        ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs",
        "DoorOpeningScheduleBuilder.Build(project)",
        "count = QuantityReportMath.AddCount(count, row.Count);",
        "DoorOpeningXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Material",
        ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs",
        "MaterialUsageScheduleBuilder.Build(project)",
        "elements = QuantityReportMath.AddCount(elements, row.ElementCount);",
        "MaterialUsageXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "Room finish",
        ROOT / "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs",
        "RoomFinishScheduleBuilder.Build(project)",
        "primary = QuantityReportMath.Add(primary, row.PrimaryQuantity, \"HT_Phòng export primary quantity\");",
        "RoomFinishXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
    ),
    (
        "BBS CSV",
        ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
        "ProjectRebarScheduleBuilder.Build(project)",
        "totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, \"BBS CSV total weight\");",
        "RebarCsvExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status);",
    ),
)

errors = []
for label, path, build, aggregate, export, finalize in CASES:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    dialog = "if (dialog.ShowDialog() != true) return;"
    project = "ProjectContextCoordinator.GetOrCreate(document)"
    regenerate = "RegenerateDirty(project)"
    positions = {
        dialog: text.find(dialog),
        project: text.find(project),
        regenerate: text.find(regenerate),
        build: text.find(build),
        aggregate: text.find(aggregate),
        export: text.find(export),
        finalize: text.find(finalize),
    }

    for token, pos in positions.items():
        if pos < 0:
            errors.append(label + " missing export-boundary token: " + token)

    if min(positions.values()) >= 0:
        if not (
            positions[dialog]
            < positions[project]
            < positions[regenerate]
            < positions[build]
            < positions[aggregate]
            < positions[export]
            < positions[finalize]
        ):
            errors.append(label + " must confirm destination before current-project regeneration/build, then validate aggregates, export, and finalize UI")

        before_dialog = text[:positions[dialog]]
        for forbidden in (project, regenerate, build):
            if forbidden in before_dialog:
                errors.append(label + " Cancel path must not touch project/regeneration/schedule state before save confirmation: " + forbidden)

        between_export_and_finalize = text[positions[export] + len(export):positions[finalize]]
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

print("PASS: schedule exporters confirm the destination before project/regeneration work, rebuild fresh rows, validate aggregates before writing, and isolate post-export UI.")
