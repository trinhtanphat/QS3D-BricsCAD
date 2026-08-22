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
    start = text.find("private void OnExportClick")
    end = text.find("private void EnsureActive", start + 1)
    if start < 0 or end < 0:
        errors.append("RebarScheduleWindow missing export method boundary")
    else:
        body = text[start:end]
        confirm = body.find("if (dialog.ShowDialog(this) != true) return;")
        project = body.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)")
        snapshot = body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        regenerate = body.find("RegenerateDirty(snapshot)")
        build = body.find("_rows = ProjectRebarScheduleBuilder.Build(snapshot);")
        bind = body.find("BindRows();", build + 1)
        export = body.find("XlsxRebarScheduleExporter.Export(dialog.FileName, _rows)")
        if min(confirm, project, snapshot, regenerate, build, bind, export) < 0:
            errors.append("BBS review export missing save/read-only-project/detached-regenerate/build/rebind/export contract token")
        elif not confirm < project < snapshot < regenerate < build < bind < export:
            errors.append("BBS review XLSX must confirm Save before existing-project lookup, detached regeneration, fresh build, UI rebind, and export")

        before_confirm = body[:confirm if confirm >= 0 else 0]
        for forbidden in (
            "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
            "ProjectStateSnapshot.CreateDetachedCopy(project)",
            "RegenerateDirty(snapshot)",
            "ProjectRebarScheduleBuilder.Build(snapshot)",
        ):
            if forbidden in before_confirm:
                errors.append("BBS review Cancel path must not execute before Save confirmation: " + forbidden)
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate(_document)",
            "ExistingProjectMutationContext",
            "RegenerateDirty(project)",
            "ProjectRebarScheduleBuilder.Build(project)",
        ):
            if forbidden in body:
                errors.append("BBS review export must not create/bind/regenerate live project state: " + forbidden)

    for token in (
        "private IReadOnlyList<RebarScheduleRow> _rows;",
        "private void BindRows()",
        "Grid.ItemsSource = null;",
    ):
        if token not in text:
            errors.append("BBS review freshness support missing token: " + token)

print("QS3D BBS review export freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BBS review export confirms Save, re-resolves existing state read-only, regenerates a detached snapshot, refreshes visible totals, and exports fresh rows.")
