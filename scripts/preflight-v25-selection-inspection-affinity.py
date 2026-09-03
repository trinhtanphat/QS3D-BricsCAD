#!/usr/bin/env python3
"""Guard delayed V25 selection inspection against cross-document/project binding."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SELECTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
selection = SELECTION.read_text(encoding="utf-8")
palette = PALETTE.read_text(encoding="utf-8")

failures = []

required_selection = (
    "PaletteCoordinator.SetInspection(document, EntitySnapshotReader.ReadImpliedSelection(document));",
    'PaletteCoordinator.SetStatus("Selection sync lỗi. Vui lòng thử lại.");',
)
for required in required_selection:
    if required not in selection:
        failures.append("selection refresh is missing document-bound/redacted behavior: " + required)

for forbidden in (
    "PaletteCoordinator.SetInspection(EntitySnapshotReader.ReadImpliedSelection(document));",
    'PaletteCoordinator.SetStatus("Selection sync lỗi: " + ex.Message);',
):
    if forbidden in selection:
        failures.append("selection refresh still has stale-affinity/detail-leak pattern: " + forbidden)

required_palette = (
    "public static void SetInspection(Document document, IReadOnlyList<EntitySnapshot> snapshots)",
    "if (!ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;",
    "if (ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))",
)
for required in required_palette:
    if required not in palette:
        failures.append("inspection application is missing active-document affinity guard: " + required)

for forbidden in (
    "public static void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)",
    "var document = Application.DocumentManager.MdiActiveDocument;",
):
    if forbidden in palette:
        failures.append("inspection application can independently rebind to a different active document: " + forbidden)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 selection inspection document-affinity preflight passed")
