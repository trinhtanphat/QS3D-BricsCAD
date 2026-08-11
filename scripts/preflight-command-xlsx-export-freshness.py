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
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;")
        project = ed2.find(ed2_project_token)
        snapshot = ed2.find(ed2_snapshot_token)
        regenerate = ed2.find(ed2_regenerate_token)
        details = ed2.find(ed2_detail_token)
        summary = ed2.find("ProjectQuantityReportBuilder.Group(previewProject")
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);")
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);")
        if min(confirm, project, snapshot, regenerate, details, summary, live, export) < 0:
            errors.append("ED2 export missing save/read-only/snapshot/regenerate/build/live/export contract token")
        elif not confirm < project < snapshot < regenerate < details < summary < live < export:
            errors.append("ED2 must confirm Save before read-only project lookup, detached regeneration/report build, live-handle validation and export")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(doc)",
            "RegenerateProject(project)",
        ):
            if forbidden in ed2:
                errors.append("ED2 read-only export must not mutate/create live project state: " + forbidden)
        before_confirm = ed2[:confirm if confirm >= 0 else 0]
        for token in (ed2_project_token, ed2_snapshot_token, ed2_regenerate_token, ed2_detail_token):
            if token in before_confirm:
                errors.append("ED2 Cancel path executes project/report work before Save confirmation: " + token)

        bbs = text[bbs_start:regen_start]
        bbs_project_token = "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)"
        bbs_snapshot_token = "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);"
        bbs_regenerate_token = "RegenerateProject(previewProject);"
        bbs_build_token = "ProjectRebarScheduleBuilder.Build(previewProject)"
        confirm = bbs.find("if (dialog.ShowDialog() != true) return;")
        project = bbs.find(bbs_project_token)
        snapshot = bbs.find(bbs_snapshot_token)
        regenerate = bbs.find(bbs_regenerate_token)
        build = bbs.find(bbs_build_token)
        aggregate = bbs.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")')
        export = bbs.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
        if min(confirm, project, snapshot, regenerate, build, aggregate, export) < 0:
            errors.append("BBS XLSX export missing save/read-only/snapshot/regenerate/build/aggregate/export contract token")
        elif not confirm < project < snapshot < regenerate < build < aggregate < export:
            errors.append("BBS XLSX must confirm Save before read-only project lookup, detached regeneration/fresh build, aggregate validation and export")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(doc)",
            "RegenerateProject(project);",
            "ProjectRebarScheduleBuilder.Build(project)",
        ):
            if forbidden in bbs:
                errors.append("BBS read-only export must not mutate/create live project state: " + forbidden)
        before_confirm = bbs[:confirm if confirm >= 0 else 0]
        for token in (bbs_project_token, bbs_snapshot_token, bbs_regenerate_token, bbs_build_token):
            if token in before_confirm:
                errors.append("BBS XLSX Cancel path executes project/report work before Save confirmation: " + token)

print("QS3D command XLSX export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 and BBS confirm the destination before resolving existing project state, regenerate only detached snapshots, validate fresh reports, and leave Cancel/live semantic state side-effect free.")
