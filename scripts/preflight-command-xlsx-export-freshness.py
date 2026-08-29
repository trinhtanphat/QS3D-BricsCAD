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
        ed2_project_token = "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)"
        ed2_snapshot_token = "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);"
        ed2_regenerate_token = "var regenerated = RegenerateProject(previewProject);"
        ed2_detail_token = "ProjectQuantityReportBuilder.Detail(previewProject"
        project = ed2.find(ed2_project_token)
        snapshot = ed2.find(ed2_snapshot_token)
        regenerate = ed2.find(ed2_regenerate_token)
        details = ed2.find(ed2_detail_token)
        summary = ed2.find("ProjectQuantityReportBuilder.Group(previewProject")
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);")
        dialog = ed2.find("var dialog = new SaveFileDialog")
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);", confirm + 1)
        if min(project, snapshot, regenerate, details, summary, live, dialog, confirm, export) < 0:
            errors.append("ED2 export missing read-only/snapshot/regenerate/build/live/save/export contract token")
        elif not project < snapshot < regenerate < details < summary < live < dialog < confirm < export:
            errors.append("ED2 must validate existing detached report/live-handle exportability before SaveFileDialog, then export only after Save confirmation")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(doc)",
            "RegenerateProject(project)",
        ):
            if forbidden in ed2:
                errors.append("ED2 read-only export must not mutate/create live project state: " + forbidden)
        before_confirm = ed2[:confirm if confirm >= 0 else 0]
        if "XlsxQuantityExporter.ExportEd2(" in before_confirm:
            errors.append("ED2 must not write XLSX before Save confirmation")

        bbs = text[bbs_start:regen_start]
        bbs_project_token = "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)"
        bbs_snapshot_token = "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);"
        bbs_regenerate_token = "RegenerateProject(previewProject);"
        bbs_build_token = "ProjectRebarScheduleBuilder.Build(previewProject)"
        project = bbs.find(bbs_project_token)
        snapshot = bbs.find(bbs_snapshot_token)
        regenerate = bbs.find(bbs_regenerate_token)
        build = bbs.find(bbs_build_token)
        aggregate = bbs.find('var totals = RebarScheduleBuilder.CalculateTotals(rows);')
        dialog = bbs.find("var dialog = new SaveFileDialog")
        confirm = bbs.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
        export = bbs.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);", confirm + 1)
        if min(project, snapshot, regenerate, build, aggregate, dialog, confirm, export) < 0:
            errors.append("BBS XLSX export missing read-only/snapshot/regenerate/build/aggregate/save/export contract token")
        elif not project < snapshot < regenerate < build < aggregate < dialog < confirm < export:
            errors.append("BBS XLSX must validate fresh detached rows and canonical finite aggregate before SaveFileDialog, then export only after Save confirmation")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(doc)",
            "RegenerateProject(project);",
            "ProjectRebarScheduleBuilder.Build(project)",
            'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")',
        ):
            if forbidden in bbs:
                errors.append("BBS read-only export must not mutate/create live project state or restore pairwise status aggregation: " + forbidden)
        before_confirm = bbs[:confirm if confirm >= 0 else 0]
        if "XlsxRebarScheduleExporter.Export(" in before_confirm:
            errors.append("BBS XLSX must not write the export before Save confirmation")

print("QS3D command XLSX export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 and BBS validate existing detached exportability before SaveFileDialog, BBS uses canonical compensated totals, writes occur only after confirmation, and live semantic state stays read-only.")
