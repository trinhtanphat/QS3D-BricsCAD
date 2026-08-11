#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = ROOT / "src/QS3D.BricsCAD.V25"
HELPER = SOURCE_ROOT / "ExistingProjectMutationContext.cs"
errors = []

if not HELPER.is_file():
    errors.append("missing ExistingProjectMutationContext.cs")
else:
    helper = HELPER.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var existing)",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "existing.ProjectId",
        "canonical.ProjectId",
        "ProjectContextCoordinator.Forget(document)",
        "project = canonical;",
    ):
        if token not in helper:
            errors.append("canonical existing-project mutation helper missing token: " + token)

DIRECT_TRY = {
    "AutoHostLinkCommands.cs": "ExistingProjectMutationContext.TryGet(document, out var project)",
    "BbsCsvCommands.cs": "ExistingProjectMutationContext.TryGet(document, out var project)",
}
for filename, token in DIRECT_TRY.items():
    path = SOURCE_ROOT / filename
    if not path.is_file():
        errors.append("missing mutation command: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    if token not in text:
        errors.append(filename + ": missing canonical existing-project mutation lookup")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" in text:
        errors.append(filename + ": must not mutate a detached read-only project instance")

TABLE_COMMANDS = (
    "BqNativeTableCommands.cs",
    "BbsNativeTableCommands.cs",
    "DoorOpeningNativeTableCommands.cs",
    "RoomFinishNativeTableCommands.cs",
    "MaterialUsageNativeTableCommands.cs",
    "SemanticElementTableCommands.cs",
)
for filename in TABLE_COMMANDS:
    path = SOURCE_ROOT / filename
    if not path.is_file():
        errors.append("missing native table mutation command: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    if "return ExistingProjectMutationContext.Require(document, operation);" not in text:
        errors.append(filename + ": RequireExistingProject must return canonical mutation context")
    health_index = text.find("TABLEHEALTH")
    if health_index >= 0:
        health_tail = text[health_index:]
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in health_tail:
            errors.append(filename + ": health inspection must retain true read-only lookup")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: existing-project mutations use canonical cached state while health/read-only inspection remains detached-safe.")
