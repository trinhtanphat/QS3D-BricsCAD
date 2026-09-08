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
    "if (!IsCurrentAttachment(document, attachmentToken) ||",
    "!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;",
    "PaletteCoordinator.SetInspection(snapshots);",
    'SelectionSyncStatusPublisher.SetStatusForDocument(document, "Selection sync lỗi. Vui lòng thử lại.");',
)
for token in required:
    if token not in text:
        failures.append("selection refresh is missing affinity/redaction invariant: " + token)

for forbidden in (
    "PaletteCoordinator.SetInspection(EntitySnapshotReader.ReadImpliedSelection(document));",
    'PaletteCoordinator.SetStatus("Selection sync lỗi. Vui lòng thử lại.");',
    'PaletteCoordinator.SetStatus("Selection sync lỗi: " + ex.Message);',
):
    if forbidden in text:
        failures.append("selection refresh still has stale-affinity/detail-leak pattern: " + forbidden)

# Ordering is the core safety property: all potentially re-entrant palette creation must finish before
# snapshot capture; after capture, both the captured attachment generation and source-document activity
# must still be authoritative before inspection is applied.
try:
    ensure_index = text.index("PaletteCoordinator.EnsureCreated();")
    snapshot_index = text.index("var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);")
    attachment_index = text.index("if (!IsCurrentAttachment(document, attachmentToken) ||", snapshot_index)
    active_index = text.index("!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;", attachment_index)
    apply_index = text.index("PaletteCoordinator.SetInspection(snapshots);", active_index)
    if not (ensure_index < snapshot_index < attachment_index < active_index < apply_index):
        failures.append("selection inspection ordering does not preserve attachment-generation/document affinity")
except ValueError:
    failures.append("selection inspection ordering tokens are incomplete")

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 selection inspection attachment-generation/document-affinity preflight passed")
