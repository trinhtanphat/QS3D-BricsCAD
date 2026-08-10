#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
errors = []

if not WINDOW.is_file():
    errors.append("missing QuantitySummaryWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")
    forbidden = (
        "private readonly ProjectState _project;",
        "_project = ProjectContextCoordinator.GetOrCreate(_document);",
        "_project.Metadata[TemplateProfileStore.VisibleBqColumnsKey]",
        "_project.Touch();",
    )
    for token in forbidden:
        if token in text:
            errors.append("BQ window must not retain/mutate a stale cached ProjectState: " + token)

    required = (
        "using QS3D.Core.Persistence;",
        "var project = ProjectContextCoordinator.GetOrCreate(_document);",
        "ProjectStateSnapshot.Capture(project)",
        "project.Metadata[TemplateProfileStore.VisibleBqColumnsKey] = string.Join(\"|\", visible);",
        "rollback.Restore(project);",
        "new HashSet<string>(ColumnKeys, StringComparer.OrdinalIgnoreCase)",
        "LoadColumnPreferences();",
    )
    for token in required:
        if token not in text:
            errors.append("QuantitySummaryWindow missing reload-safe preference token: " + token)

    persist_pos = text.find("private void PersistColumnPreferences()")
    next_pos = text.find("private IEnumerable<CheckBox>", persist_pos)
    body = text[persist_pos:next_pos] if persist_pos >= 0 and next_pos > persist_pos else ""
    resolve_pos = body.find("ProjectContextCoordinator.GetOrCreate(_document)")
    snapshot_pos = body.find("ProjectStateSnapshot.Capture(project)")
    metadata_pos = body.find("project.Metadata[TemplateProfileStore.VisibleBqColumnsKey]")
    touch_pos = body.find("project.Touch();")
    if min(resolve_pos, snapshot_pos, metadata_pos, touch_pos) < 0 or not resolve_pos < snapshot_pos < metadata_pos < touch_pos:
        errors.append("BQ column preference write must resolve the current project, snapshot it, then mutate metadata/timestamp")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BQ column preferences rebind to the current DWG project after reload and persist through an explicit rollback boundary")
