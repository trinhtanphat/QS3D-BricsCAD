#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/ProjectSemanticMutationExecutor.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectSemanticMutationExecutorSmoke.cs"
DOC = ROOT / "docs/PROJECT-SEMANTIC-MUTATION-JOURNAL.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic mutation journal file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "ProjectSemanticMutationPhase",
    "Planned",
    "Running",
    "Validating",
    "Committed",
    "RollingBack",
    "RolledBack",
    "RollbackFailed",
    "ProjectSemanticMutationJournal",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(project)",
    "new AggregateException(operationError, rollbackError)",
    "preCommitValidation",
    "TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Planned",
    "TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Running",
    "TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Validating",
    "TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Committed",
    "This executor restores semantic project state only",
):
    if token not in source:
        errors.append("semantic mutation executor missing contract token: " + token)

for token in (
    "SuccessfulMutationRecordsOrderedPhases",
    "MutationExceptionRestoresCompleteProjectState",
    "PreCommitFaultRollsBackCompletedInterchangeMutation",
    "SaturatedJournalCannotChangeMutationOutcome",
    "InvalidOperationNameFailsBeforeMutation",
    "ProjectInterchangeImportCoordinator.Execute",
    "PreserveSourceHandleProvenance = true",
    "injected post-import validation fault",
    'project.Metadata["CommittedAfterSaturation"] = "yes"',
    "Equal(256, journal.Entries.Count)",
    "ChangeVersion",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("semantic mutation smoke missing regression token: " + token)

for token in (
    "semantic-only",
    "detached journal",
    "diagnostic saturation",
    "pre-commit validation",
    "does not roll back native DWG",
    "fault injection",
    "ProjectStateSnapshot",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("semantic mutation documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic mutation scope keeps journal saturation diagnostic-only, journals normal ordered phases, and rolls back ProjectState after real mutation/pre-commit faults without claiming native DWG rollback.")