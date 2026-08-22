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
        regenerate = ed2.find("var regenerated = RegenerateProject(project);")
        details = ed2.find("ProjectQuantityReportBuilder.Detail(project)")
        summary = ed2.find("ProjectQuantityReportBuilder.Group(project)")
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);")
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);")
        if min(confirm, regenerate, details, summary, live, export) < 0:
            errors.append("ED2 export missing save/regenerate/build/live/export contract token")
        elif not confirm < regenerate < details < summary < live < export:
            errors.append("ED2 must confirm Save before regeneration/report build/live-handle validation/export")
        if "RegenerateProject(project)" in ed2[:confirm if confirm >= 0 else 0]:
            errors.append("ED2 Cancel path must not regenerate project state")

        bbs = text[bbs_start:regen_start]
        confirm = bbs.find("if (dialog.ShowDialog() != true) return;")
        project = bbs.find("ProjectContextCoordinator.GetOrCreate(doc)")
        regenerate = bbs.find("RegenerateProject(project);")
        build = bbs.find("ProjectRebarScheduleBuilder.Build(project)")
        aggregate = bbs.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")')
        export = bbs.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
        if min(confirm, project, regenerate, build, aggregate, export) < 0:
            errors.append("BBS XLSX export missing save/project/regenerate/build/aggregate/export contract token")
        elif not confirm < project < regenerate < build < aggregate < export:
            errors.append("BBS XLSX must confirm Save before project lookup/regeneration/fresh build/aggregate/export")
        for forbidden in ("ProjectContextCoordinator.GetOrCreate(doc)", "RegenerateProject(project);", "ProjectRebarScheduleBuilder.Build(project)"):
            if forbidden in bbs[:confirm if confirm >= 0 else 0]:
                errors.append("BBS XLSX Cancel path executes before Save confirmation: " + forbidden)

print("QS3D command XLSX export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 and BBS XLSX confirm the destination before regeneration/fresh report work.")
