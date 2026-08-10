#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
HEALTH_ALL = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
errors = []

for path in (SERVICE, GEOMETRY, COMMANDS, HEALTH_ALL):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    required = (
        'private const string HandleKey = "GeneratedSolidHandle";',
        'OpenMode.ForRead',
        'GeneratedGeometryService.HasMatchingOwnership(entity, project, element)',
        '"GENERATED_SOLID_OWNERSHIP_MISMATCH"',
        'HealthSeverity.Error',
    )
    for token in required:
        if token not in text:
            errors.append("runtime ownership service missing token: " + token)
    forbidden = (
        "OpenMode.ForWrite",
        ".Erase(",
        ".UpgradeOpen(",
        "MarkGenerated(",
        "PrepareReplacement(",
        "CommitReplacement(",
    )
    for token in forbidden:
        if token in text:
            errors.append("runtime ownership health must remain read-only; found: " + token)

if GEOMETRY.is_file():
    text = GEOMETRY.read_text(encoding="utf-8")
    if "public static bool HasMatchingOwnership(Entity entity, ProjectState project, ProjectElement element)" not in text:
        errors.append("GeneratedGeometryService must expose the existing ownership parser through a read-only overload")
    if 'private const string RegAppName = "QS3D";' not in text:
        errors.append("GeneratedGeometryService ownership RegApp contract changed unexpectedly")

for path, caller in ((COMMANDS, "QS3DHEALTH"), (HEALTH_ALL, "QS3DHEALTHALL")):
    if path.is_file():
        text = path.read_text(encoding="utf-8")
        if "GeneratedSolidRuntimeHealthService.Inspect" not in text:
            errors.append(caller + " does not include runtime GeneratedSolidHandle XData ownership diagnostics")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] host GeneratedSolidHandle runtime ownership is checked read-only in QS3DHEALTH and QS3DHEALTHALL")
