#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipSafetySmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing generated ownership safety file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


policy = read(POLICY)
smoke = read(SMOKE)

for token in (
    "EnsureValidElementSet(project);",
    "Project contains a null semantic element entry; generated CAD ownership cannot be resolved safely.",
    "Project contains duplicate element id:",
    "CollectOwnerHandles(ProjectState project)",
    "TryFindOwner(ProjectState project",
):
    if token not in policy:
        errors.append("generated ownership policy missing fail-closed token: " + token)

if policy.count("EnsureValidElementSet(project);") < 2:
    errors.append("both CollectOwnerHandles and TryFindOwner must validate the complete semantic element set")

for forbidden in (
    ".Where(x => x != null)",
    "if (element == null) continue;",
):
    if forbidden in policy:
        errors.append("generated ownership scan must not silently skip corrupt null semantic elements: " + forbidden)

for token in (
    "ValidOwnershipStillResolvesDeterministically",
    "NullElementFailsClosed",
    "DuplicateElementIdsFailClosed",
    "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
    "GeneratedHandleOwnershipPolicy.TryFindOwner(project, \"UNOWNED\"",
    "ModuleInitializer",
):
    if token not in smoke:
        errors.append("generated ownership smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generated CAD ownership scans fail closed on corrupt semantic element sets before reporting handles as owned or unowned.")
