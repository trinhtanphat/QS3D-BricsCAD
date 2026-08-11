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
        tokens = (
            "if (dialog.ShowDialog(this) != true) return;",
            "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
            "ProjectStateSnapshot.CreateDetachedCopy(project)",
            "RegenerateDirty(snapshot)",
            "_rows = ProjectRebarScheduleBuilder.Build(snapshot);",
            "BindRows();",
            "XlsxRebarScheduleExporter.Export(dialog.FileName, _rows)",
        )
        positions = [body.find(token) for token in tokens]
        if min(positions) < 0 or positions != sorted(positions):
            errors.append("BBS review export must confirm Save -> read-only lookup -> detached copy -> regenerate -> build -> rebind -> export")
        for forbidden in (
            "ExistingProjectMutationContext",
            "ProjectContextCoordinator.GetOrCreate(_document)",
            "RegenerateDirty(project)",
            "ProjectRebarScheduleBuilder.Build(project)",
        ):
            if forbidden in body:
                errors.append("BBS review export must not mutate/bind live project state: " + forbidden)

    for token in (
        "using QS3D.Core.Persistence;",
        "private IReadOnlyList<RebarScheduleRow> _rows;",
        "private void BindRows()",
        "Grid.ItemsSource = null;",
    ):
        if token not in text:
            errors.append("BBS review freshness support missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: BBS modeless review export regenerates a detached snapshot, refreshes visible totals, and leaves live project state untouched.")
