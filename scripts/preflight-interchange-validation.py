#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeValidationCommands.cs"
EXPORT = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeValidationSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-JSON.md"
errors = []

for path in (CORE, COMMAND, EXPORT, SMOKE, REGISTRATION, DOC):
    if not path.is_file():
        errors.append("missing interchange validation dependency: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    required = (
        "ProjectInterchangeJsonExporter.FormatName",
        "ProjectInterchangeJsonExporter.FormatVersion",
        'RequireUnit(units.Length, "m", "length", issues)',
        'RequireUnit(units.Area, "m2", "area", issues)',
        'RequireUnit(units.Volume, "m3", "volume", issues)',
        'RequireUnit(units.Mass, "kg", "mass", issues)',
        '"drawing-local"',
        '"GENERATED_RUNTIME_PROPERTY"',
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
        'key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)',
        'key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        '"DEPENDENCY_REF_MISSING"',
        '"DEPENDENCY_SELF"',
        '"DEPENDENCY_CYCLE"',
        '"FAMILY_CATEGORY_MISMATCH"',
        '"SOURCE_SCOPE"',
        "MaxFileBytes",
        "MaxElements",
        "MaxIssues",
        "var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)",
        "var dependents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)",
        "var ready = new SortedSet<string>",
        "while (ready.Count > 0)",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectInterchangeJsonValidator.cs missing validation contract: " + token)
    forbidden = (
        "ProjectContextCoordinator",
        "ProjectStateSnapshot",
        "QsdbProjectStore",
        "RegenerationEngine",
        "ProjectState ",
        "VisitDependencies(",
    )
    for token in forbidden:
        if token in text:
            errors.append("Core interchange validator must remain detached/read-only and dependency validation must stay iterative; forbidden token: " + token)

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DINTERCHANGEVALIDATE", CommandFlags.Modal)]',
        "new OpenFileDialog",
        "ProjectInterchangeJsonValidator.ValidateFile(dialog.FileName)",
        "READ-ONLY / NOT IMPORTED",
        "Nothing was imported or changed",
    ):
        if token not in text:
            errors.append("ProjectInterchangeValidationCommands.cs missing read-only command contract: " + token)
    for token in (
        "ProjectContextCoordinator",
        "GetOrCreate(",
        "ProjectStateSnapshot",
        "RegenerationEngine",
        "QsdbProjectStore",
        "TransactionManager",
        "StartTransaction",
        "SaveFileDialog",
    ):
        if token in text:
            errors.append("QS3DINTERCHANGEVALIDATE must not mutate/load/replace project or DWG state; forbidden token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExportedValidSnapshotPasses",
        "WrongUnitsFailClosed",
        "GeneratedOwnershipSmugglingFailsClosed",
        "BrokenDependencyFailsClosed",
        "DependencyCycleFailsClosed",
        '"GENERATED_RUNTIME_PROPERTY"',
        '"DEPENDENCY_REF_MISSING"',
        '"DEPENDENCY_CYCLE"',
    ):
        if token not in text:
            errors.append("ProjectInterchangeValidationSmoke.cs missing validator smoke: " + token)

if REGISTRATION.is_file() and "ProjectInterchangeValidationSmoke.Run();" not in REGISTRATION.read_text(encoding="utf-8"):
    errors.append("ProjectInterchangeValidationSmoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DINTERCHANGEVALIDATE",
        "READ-ONLY / NOT IMPORTED",
        "does **not** claim",
        "ID collision resolution",
        "current-DWG source-handle rebinding",
        "preflight-interchange-validation.py",
    ):
        if token not in text:
            errors.append("INTERCHANGE-JSON.md missing validator/import boundary: " + token)

print("QS3D semantic interchange validation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DINTERCHANGEVALIDATE remains a bounded read-only semantic snapshot validator tied to the v1 exporter contract, uses iterative dependency-cycle validation and cannot be mistaken for project/DWG import or generated ownership reconstruction.")
