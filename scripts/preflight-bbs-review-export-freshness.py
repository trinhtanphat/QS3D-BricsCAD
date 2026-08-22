#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")

    export_start = text.find("private void OnExportClick")
    export_end = text.find("private RebarScheduleRow ResolveCurrentRow", export_start + 1)
    if export_start < 0 or export_end < 0:
        errors.append("RebarScheduleWindow missing export method boundary")
    else:
        body = text[export_start:export_end]
        confirm = body.find("if (dialog.ShowDialog(this) != true) return;")
        active = body.find("EnsureActive(", confirm + 1)
        build = body.find("_rows = BuildCurrentRows();")
        bind = body.find("BindRows();", build + 1)
        nonempty = body.find("if (_rows.Count == 0)", bind + 1)
        export = body.find("XlsxRebarScheduleExporter.Export(dialog.FileName, _rows)", nonempty + 1)
        if min(confirm, active, build, bind, nonempty, export) < 0:
            errors.append("BBS review export missing save/active-DWG/live-build/rebind/nonempty/export contract token")
        elif not confirm < active < build < bind < nonempty < export:
            errors.append("BBS review XLSX must confirm Save before active-DWG recheck, live detached build, UI rebind, nonempty validation, and export")

        before_confirm = body[:confirm if confirm >= 0 else 0]
        for forbidden in (
            "BuildCurrentRows()",
            "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
            "ProjectStateSnapshot.CreateDetachedCopy(project)",
            "RegenerateDirty(snapshot)",
            "ProjectRebarScheduleBuilder.Build(snapshot)",
        ):
            if forbidden in before_confirm:
                errors.append("BBS review Cancel path must not resolve/regenerate report state before Save confirmation: " + forbidden)

    helper_start = text.find("private IReadOnlyList<RebarScheduleRow> BuildCurrentRows()")
    helper_end = text.find("private static bool SameRow", helper_start + 1)
    if helper_start < 0 or helper_end < 0:
        errors.append("RebarScheduleWindow missing BuildCurrentRows helper boundary")
    else:
        helper = text[helper_start:helper_end]
        project = helper.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)")
        snapshot = helper.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        regenerate = helper.find("RegenerateDirty(snapshot)")
        build = helper.find("ProjectRebarScheduleBuilder.Build(snapshot)")
        if min(project, snapshot, regenerate, build) < 0:
            errors.append("BBS live-row helper missing existing-project/detached-regenerate/build contract token")
        elif not project < snapshot < regenerate < build:
            errors.append("BBS live-row helper must resolve existing project read-only, clone detached state, regenerate detached state, then build canonical rows")
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(_document)",
            "ExistingProjectMutationContext",
            "RegenerateDirty(project)",
            "ProjectRebarScheduleBuilder.Build(project)",
        ):
            if forbidden in helper:
                errors.append("BBS live-row helper must not create/bind/regenerate live project state: " + forbidden)

    for token in (
        "private IReadOnlyList<RebarScheduleRow> _rows;",
        "private void BindRows()",
        "Grid.ItemsSource = null;",
        "private RebarScheduleRow ResolveCurrentRow(RebarScheduleRow displayedRow)",
    ):
        if token not in text:
            errors.append("BBS review freshness support missing token: " + token)

print("QS3D BBS review export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BBS review export confirms Save before rebuilding authoritative rows through a read-only detached helper, refreshes visible totals, and exports fresh rows.")
