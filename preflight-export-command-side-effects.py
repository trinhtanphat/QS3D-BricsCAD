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

# Template export has no semantic regeneration, but Cancel must still be side-effect free: opening
# the dialog must not create/cache a project. Once the template file is committed, UI reporting is
# best effort so a Palette/Editor failure cannot turn a successful export into a reported failure.
template_path = ROOT / "src/QS3D.BricsCAD.V25/TemplateCommands.cs"
if not template_path.is_file():
    errors.append("missing src/QS3D.BricsCAD.V25/TemplateCommands.cs")
else:
    text = template_path.read_text(encoding="utf-8")
    dialog = "if (dialog.ShowDialog() != true) return;"
    project = "ProjectContextCoordinator.GetOrCreate(doc)"
    build = "store.ExportProject(project,"
    export = "store.Save(profile, dialog.FileName);"
    finalize = "FinalizeExportUi(doc,"
    positions = {
        dialog: text.find(dialog),
        project: text.find(project),
        build: text.find(build),
        export: text.find(export),
        finalize: text.find(finalize),
    }
    for token, pos in positions.items():
        if pos < 0:
            errors.append("Template missing export-boundary token: " + token)
    if min(positions.values()) >= 0:
        if not (positions[dialog] < positions[project] < positions[build] < positions[export] < positions[finalize]):
            errors.append("Template must confirm destination before project creation/profile build, commit the file, then finalize UI")
        before_dialog = text[:positions[dialog]]
        for forbidden in (project, build):
            if forbidden in before_dialog:
                errors.append("Template Cancel path must not create/read project export state before save confirmation: " + forbidden)
        between_export_and_finalize = text[positions[export] + len(export):positions[finalize]]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append("Template must not perform fallible UI work between persistent export and FinalizeExportUi")
    if "Cảnh báo UI sau export template" not in text:
        errors.append("Template missing best-effort post-export UI warning boundary")

if errors:
    print("QS3D export command side-effect preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: schedule/template exporters confirm destination before project/regeneration work, validate/build before writing, and isolate post-export UI from persistent success.")
