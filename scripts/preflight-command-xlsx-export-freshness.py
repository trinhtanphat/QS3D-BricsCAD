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
        detached = "QS3D.Core.Persistence.ProjectStateSnapshot.CreateDetachedCopy(project)"

        ed2 = text[ed2_start:bbs_start]
        project = ed2.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        confirm = ed2.find("if (dialog.ShowDialog() != true) return;")
        snapshot = ed2.find(detached)
        regenerate = ed2.find("var regenerated = RegenerateProject(snapshot);")
        details = ed2.find("ProjectQuantityReportBuilder.Detail(snapshot)")
        summary = ed2.find("ProjectQuantityReportBuilder.Group(snapshot)")
        live = ed2.find("EnsureEd2HandlesAreLive(doc, details);")
        export = ed2.find("XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);")
        if min(project, confirm, snapshot, regenerate, details, summary, live, export) < 0:
            errors.append("ED2 export missing existing-project/detached-regeneration/export contract token")
        elif not project < confirm < snapshot < regenerate < details < summary < live < export:
            errors.append("ED2 must resolve existing project read-only, confirm Save, regenerate/build detached state, validate live handles, then export")
        if "ProjectContextCoordinator.GetOrCreate(doc)" in ed2:
            errors.append("ED2 export must not create/cache a project")
        if "RegenerateProject(project)" in ed2:
            errors.append("ED2 export must not regenerate the live project")
        if "ProjectStateSnapshot.CreateDetachedCopy(project)" in ed2[:confirm if confirm >= 0 else 0]:
            errors.append("ED2 Cancel path must not allocate/regenerate detached export state before Save confirmation")

        bbs = text[bbs_start:regen_start]
        confirm = bbs.find("if (dialog.ShowDialog() != true) return;")
        project = bbs.find("ProjectContextCoordinator.TryGetReadOnly(doc, out var project)")
        snapshot = bbs.find(detached)
        regenerate = bbs.find("RegenerateProject(snapshot);")
        build = bbs.find("ProjectRebarScheduleBuilder.Build(snapshot)")
        aggregate = bbs.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")')
        export = bbs.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
        if min(confirm, project, snapshot, regenerate, build, aggregate, export) < 0:
            errors.append("BBS XLSX export missing save/read-only/detached-regeneration/build/aggregate/export contract token")
        elif not confirm < project < snapshot < regenerate < build < aggregate < export:
            errors.append("BBS XLSX must confirm Save before existing-project lookup, detached regeneration/fresh build, aggregate, and export")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(doc)",
            "RegenerateProject(project);",
            "ProjectRebarScheduleBuilder.Build(project)",
        ):
            if forbidden in bbs:
                errors.append("BBS XLSX must not mutate/create live project state: " + forbidden)
        for forbidden in (
            "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
            detached,
            "RegenerateProject(snapshot);",
            "ProjectRebarScheduleBuilder.Build(snapshot)",
        ):
            if forbidden in bbs[:confirm if confirm >= 0 else 0]:
                errors.append("BBS XLSX Cancel path executes export-state work before Save confirmation: " + forbidden)

print("QS3D command XLSX export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ED2 and BBS XLSX use existing read-only projects and regenerate detached snapshots only after export confirmation.")
