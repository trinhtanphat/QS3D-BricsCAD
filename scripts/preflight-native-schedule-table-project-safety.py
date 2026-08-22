#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = (
    "src/QS3D.BricsCAD.V25/BbsNativeTableCommands.cs",
    "src/QS3D.BricsCAD.V25/MaterialUsageNativeTableCommands.cs",
    "src/QS3D.BricsCAD.V25/DoorOpeningNativeTableCommands.cs",
    "src/QS3D.BricsCAD.V25/RoomFinishNativeTableCommands.cs",
)
errors = []

for relative in FILES:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing native schedule table command file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    if "private static QS3D.Core.Domain.ProjectState RequireExistingProject(Document document, string operation)" not in text:
        errors.append(relative + " missing shared existing-project guard")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(relative + " must resolve existing project state read-only")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(relative + " must not create/cache semantic project state from native schedule-table Build/Refresh/Remove")
    if "HEALTH" not in text.upper() or "Inspect(document, project)" not in text:
        errors.append(relative + " must retain native table health inspection")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BBS/Material/Door/Room native schedule tables require existing semantic project state and retain read-only health")
