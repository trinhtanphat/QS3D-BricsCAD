#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing CurtainWallScheduleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
<<<<<<< HEAD
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append("QS3DCURTAINXLSX must require an existing QS3D project")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DCURTAINXLSX must not create/cache project state as an export side effect")
    if "ProjectStateSnapshot.CreateDetachedCopy(project)" not in text or "CurtainWallScheduleBuilder.Build(snapshot)" not in text:
        errors.append("Curtain XLSX must continue using the authoritative schedule builder")
=======
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "CurtainWallScheduleBuilder.Build(snapshot)",
    ):
        if token not in text:
            errors.append("QS3DCURTAINXLSX missing read-only detached-export token: " + token)
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "CurtainWallScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("QS3DCURTAINXLSX must not mutate/bind the live project during export: " + forbidden)
>>>>>>> origin/main

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Curtain XLSX requires existing project state, regenerates a detached snapshot and uses the authoritative schedule builder")
