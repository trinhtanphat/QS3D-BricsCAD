#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = (
    (
        "Curtain",
        ROOT / "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs",
        "CurtainWallScheduleBuilder.Build(snapshot)",
        "panels = QuantityReportMath.AddCount(panels, row.PanelCount);",
        "CurtainWallXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
        False,
    ),
    (
        "Door/Opening",
        ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs",
        "DoorOpeningScheduleBuilder.Build(snapshot)",
        "count = QuantityReportMath.AddCount(count, row.Count);",
        "DoorOpeningXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
        False,
    ),
    (
        "Material",
        ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs",
        "MaterialUsageScheduleBuilder.Build(snapshot)",
        "elements = QuantityReportMath.AddCount(elements, row.ElementCount);",
        "MaterialUsageXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
        False,
    ),
    (
        "Room finish",
        ROOT / "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs",
        "RoomFinishScheduleBuilder.Build(snapshot)",
        "var primary = primaryAccumulator.Value(\"HT_Phòng export primary quantity\");",
        "RoomFinishXlsxExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status, dialog.FileName);",
        False,
    ),
    (
        "BBS CSV",
        ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
        "totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, \"BBS CSV total weight\");",
        "RebarCsvExporter.Export(dialog.FileName, rows);",
        "FinalizeUi(document, status);",
        True,
    ),
)

errors = []
for label, path, build, aggregate, export, finalize, validate_before_dialog in CASES:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    confirm = "if (dialog.ShowDialog() != true) return;"
    project = "ProjectContextCoordinator.TryGetReadOnly(document, out var project)"
    snapshot = "ProjectStateSnapshot.CreateDetachedCopy(project)"
    regenerate = "RegenerateDirty(snapshot)"
    positions = {
        confirm: text.find(confirm),
        project: text.find(project),
        snapshot: text.find(snapshot),
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
        if validate_before_dialog:
            if not (
                positions[project]
                < positions[snapshot]
                < positions[regenerate]
                < positions[build]
                < positions[aggregate]
                < positions[confirm]
                < positions[export]
                < positions[finalize]
            ):
                errors.append(label + " must resolve existing project read-only, regenerate/build detached state, validate aggregates, confirm destination, export, then finalize UI")
            before_confirm = text[:positions[confirm]]
            if export in before_confirm:
                errors.append(label + " must not write the persistent export before save confirmation")
        else:
            if not (
                positions[confirm]
                < positions[project]
                < positions[snapshot]
                < positions[regenerate]
                < positions[build]
                < positions[aggregate]
                < positions[export]
                < positions[finalize]
            ):
                errors.append(label + " must confirm destination, resolve existing project read-only, regenerate/build detached state, validate aggregates, export, then finalize UI")

            before_confirm = text[:positions[confirm]]
            for forbidden in (project, snapshot, regenerate, build):
                if forbidden in before_confirm:
                    errors.append(label + " Cancel path must not touch project/regeneration/schedule state before save confirmation: " + forbidden)

        between_export_and_finalize = text[positions[export] + len(export):positions[finalize]]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append(label + " must not perform fallible UI work between persistent export and FinalizeUi")

    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(label + " read-only export must not create a replacement project")
    if "ExistingProjectMutationContext" in text:
        errors.append(label + " read-only export must not bind a mutation context")
    if "RegenerateDirty(project)" in text:
        errors.append(label + " read-only export must not regenerate live project state")
    if "Cảnh báo UI sau export" not in text:
        errors.append(label + " missing best-effort post-export UI warning boundary")

# Template export has no semantic regeneration. Scope checks to the export command only because
# TemplateCommands.cs also contains an intentionally create-capable import/bootstrap command.
template_path = ROOT / "src/QS3D.BricsCAD.V25/TemplateCommands.cs"
if not template_path.is_file():
    errors.append("missing src/QS3D.BricsCAD.V25/TemplateCommands.cs")
else:
    text = template_path.read_text(encoding="utf-8")
    export_start = text.find('[CommandMethod("QS3DTEMPLATEEXPORT"')
    import_start = text.find('[CommandMethod("QS3DTEMPLATEIMPORT"', export_start + 1) if export_start >= 0 else -1
    if export_start < 0 or import_start <= export_start:
        errors.append("Template cannot isolate QS3DTEMPLATEEXPORT from QS3DTEMPLATEIMPORT")
        export_text = ""
    else:
        export_text = text[export_start:import_start]

    dialog = "if (dialog.ShowDialog() != true) return;"
    project = "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)"
    build = "store.ExportProject(project,"
    export = "store.Save(profile, dialog.FileName);"
    finalize = "FinalizeExportUi(doc,"
    positions = {
        dialog: export_text.find(dialog),
        project: export_text.find(project),
        build: export_text.find(build),
        export: export_text.find(export),
        finalize: export_text.find(finalize),
    }
    for token, pos in positions.items():
        if pos < 0:
            errors.append("Template missing export-boundary token: " + token)
    if min(positions.values()) >= 0:
        if not (positions[dialog] < positions[project] < positions[build] < positions[export] < positions[finalize]):
            errors.append("Template must confirm destination before existing-project lookup/profile build, commit the file, then finalize UI")
        before_dialog = export_text[:positions[dialog]]
        for forbidden in (project, build):
            if forbidden in before_dialog:
                errors.append("Template Cancel path must not read project export state before save confirmation: " + forbidden)
        between_export_and_finalize = export_text[positions[export] + len(export):positions[finalize]]
        if "PaletteCoordinator." in between_export_and_finalize or "Editor.WriteMessage" in between_export_and_finalize:
            errors.append("Template must not perform fallible UI work between persistent export and FinalizeExportUi")
    if "ProjectContextCoordinator.GetOrCreate(doc)" in export_text:
        errors.append("Template read-only export must not create/cache replacement project state")
    if "ExistingProjectMutationContext" in export_text:
        errors.append("Template read-only export must not bind a mutation context")
    if "Cảnh báo UI sau export template" not in text:
        errors.append("Template missing best-effort post-export UI warning boundary")

if errors:
    print("QS3D export command side-effect preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: schedule exporters stay read-only on detached state, BBS CSV validates exportability before destination UI while the other schedule/template lanes preserve their current confirmation order, and all persistent writes remain confirmation-gated with post-export UI isolation.")
