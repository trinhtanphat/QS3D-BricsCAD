#!/usr/bin/env python3
"""Guard delayed V25 selection inspection against cross-document/project binding."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

required = (
    "PaletteCoordinator.EnsureCreated();",
    "var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);",
    "if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;",
    "PaletteCoordinator.SetInspection(snapshots);",
    'PaletteCoordinator.SetStatus("Selection sync lỗi. Vui lòng thử lại.");',
    "finally { Refreshing.Remove(document); }",
)
for token in required:
    if token not in text:
        failures.append("selection refresh is missing affinity/redaction invariant: " + token)

for forbidden in (
    "PaletteCoordinator.SetInspection(EntitySnapshotReader.ReadImpliedSelection(document));",
    'PaletteCoordinator.SetStatus("Selection sync lỗi: " + ex.Message);',
):
    if forbidden in text:
        failures.append("selection refresh still has stale-affinity/detail-leak pattern: " + forbidden)

# Ordering is the core safety property: all potentially re-entrant palette creation must finish before
# snapshot capture; after capture, the source document must still be active before inspection is applied.
try:
    ensure_index = text.index("PaletteCoordinator.EnsureCreated();")
    snapshot_index = text.index("var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);")
    revalidate_index = text.index("if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;", snapshot_index)
    apply_index = text.index("PaletteCoordinator.SetInspection(snapshots);", revalidate_index)
    if not (ensure_index < snapshot_index < revalidate_index < apply_index):
        failures.append("selection inspection ordering does not preserve document affinity")

    between_snapshot_and_apply = text[snapshot_index:apply_index]
    for async_handoff in ("await ", "BeginInvoke", "InvokeAsync", "Task.Run"):
        if async_handoff in between_snapshot_and_apply:
            failures.append("selection inspection introduces an async/re-entrant handoff after snapshot capture: " + async_handoff)
except ValueError:
    pass

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 selection inspection document-affinity preflight passed")
