#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")

    ed2_start = text.find('[CommandMethod("QS3DED2"')
    bbs_start = text.find('[CommandMethod("QS3DBBS"', ed2_start + 1)
    regen_start = text.find('[CommandMethod("QS3DREGEN"', bbs_start + 1)
    if min(ed2_start, bbs_start, regen_start) < 0:
        errors.append("Commands.cs missing ED2/BBS/REGEN method boundaries")
    else:
        ed2 = text[ed2_start:bbs_start]
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;")
        project = ed2.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        snapshot = ed2.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        regenerate = ed2.find("var regenerated = RegenerateProject(previewProject);")
        details = ed2.find("ProjectQuantityReportBuilder.Detail(previewProject)")
        summary = ed2.find("ProjectQuantityReportBuilder.Group(previewProject)")
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);")
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);")
        if min(confirm, project, snapshot, regenerate, details, summary, live, export) < 0:
            errors.append("ED2 export missing save/regenerate/build/live/export contract token")
        elif not confirm < project < snapshot < regenerate < details < summary < live < export:
            errors.append("ED2 must confirm Save before read-only project lookup/detached regeneration/report build/live-handle validation/export")
        for forbidden in ("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)", "ProjectStateSnapshot.CreateDetachedCopy(project)", "RegenerateProject(previewProject)"):
            if forbidden in ed2[:confirm if confirm >= 0 else 0]:
                errors.append("ED2 Cancel path executes before Save confirmation: " + forbidden)

        bbs = text[bbs_start:regen_start]
        confirm = bbs.find("if (dialog.ShowDialog() != true) return;")
        project = bbs.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        snapshot = bbs.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        regenerate = bbs.find("RegenerateProject(previewProject);")
        build = bbs.find("ProjectRebarScheduleBuilder.Build(previewProject)")
        aggregate = bbs.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")')
        export = bbs.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
        if min(confirm, project, snapshot, regenerate, build, aggregate, export) < 0:
            errors.append("BBS XLSX export missing save/project/regenerate/build/aggregate/export contract token")
        elif not confirm < project < snapshot < regenerate < build < aggregate < export:
            errors.append("BBS XLSX must confirm Save before read-only project lookup/detached regeneration/fresh build/aggregate/export")
        for forbidden in ("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)", "ProjectStateSnapshot.CreateDetachedCopy(project)", "RegenerateProject(previewProject);", "ProjectRebarScheduleBuilder.Build(previewProject)"):
            if forbidden in bbs[:confirm if confirm >= 0 else 0]:
                errors.append("BBS XLSX Cancel path executes before Save confirmation: " + forbidden)

        if "ProjectContextCoordinator.GetOrCreate(doc)" in ed2 or "ProjectContextCoordinator.GetOrCreate(doc)" in bbs:
            errors.append("ED2/BBS read-only exports must not create replacement project state")
        if "RegenerateProject(project)" in ed2 or "RegenerateProject(project)" in bbs:
            errors.append("ED2/BBS exports must not regenerate live project state")

print("QS3D command XLSX export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 and BBS XLSX confirm the destination before regeneration/fresh report work.")
