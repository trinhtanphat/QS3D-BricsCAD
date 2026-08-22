#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ASSERT = ROOT / "tests/QS3D.Core.SmokeTests/ProjectRollbackAssert.cs"
MATRIX = ROOT / "tests/QS3D.Core.SmokeTests/ProjectRollbackFailureMatrixSmoke.cs"
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
EXECUTOR = ROOT / "src/QS3D.Core/Services/ProjectSemanticMutationExecutor.cs"
DOC = ROOT / "docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing rollback failure-matrix file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


assertion = read(ASSERT)
matrix = read(MATRIX)
snapshot = read(SNAPSHOT)
executor = read(EXECUTOR)
doc = read(DOC)

for token in (
    "ProjectStateSnapshot.CreateDetachedCopy",
    ".SchemaVersion",
    ".ProjectId",
    ".Name",
    ".DrawingPath",
    ".DrawingFingerprint",
    ".ActiveZoneId",
    ".ActiveFloorId",
    ".UpdatedUtc",
    ".ChangeVersion",
    ".Zones",
    ".Floors",
    ".Families",
    ".Elements",
    ".SourceHandles",
    ".DependsOn",
    ".Properties",
    ".Quantities",
    ".Dirty",
    ".QuantityRules",
    ".AuditEvents",
    ".Metadata",
):
    if token not in assertion:
        errors.append("ProjectRollbackAssert missing whole-project comparison token: " + token)

for token in (
    "MutationStagesRestoreWholeProjectState",
    "ValidationFailureRestoresWholeProjectState",
    "AssertionHarnessDetectsDrift",
    "InjectionStage.Catalog",
    "InjectionStage.Element",
    "InjectionStage.RulesAuditMetadata",
    "InjectionStage.Validation",
    "ProjectSemanticMutationExecutor.Execute",
    "ProjectRollbackAssert.Equivalent",
    "ProjectSemanticMutationPhase.RollingBack",
    "ProjectSemanticMutationPhase.RolledBack",
    "ProjectSemanticMutationPhase.Committed",
    "ModuleInitializer",
):
    if token not in matrix:
        errors.append("rollback failure matrix smoke missing staged regression token: " + token)

if "CreateDetachedCopy" not in snapshot or "Restore(ProjectState project)" not in snapshot:
    errors.append("rollback matrix must remain backed by the canonical ProjectStateSnapshot clone/restore contract")

for token in (
    "ProjectStateSnapshot.Capture(project)",
    "rollback.Restore(project)",
    "ProjectSemanticMutationPhase.RollingBack",
    "ProjectSemanticMutationPhase.RolledBack",
):
    if token not in executor:
        errors.append("semantic mutation executor rollback contract drifted: " + token)

for forbidden in (
    "FaultInjection",
    "InjectFailure",
    "FailureStage",
    "TestHook",
):
    if forbidden in snapshot or forbidden in executor:
        errors.append("production rollback code must not gain test fault switches: " + forbidden)

for token in (
    "test-only",
    "whole-project",
    "pre-commit validation",
    "no production fault switch",
    "ProjectStateSnapshot",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("rollback failure-matrix handoff missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: test-only staged rollback matrix asserts whole-project restoration without adding production fault switches.")
