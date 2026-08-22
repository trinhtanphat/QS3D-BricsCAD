#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
CORE = ROOT / "src/QS3D.Core"
TESTS = ROOT / "tests/QS3D.Core.SmokeTests"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing lifecycle file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


lifecycle = read(ADAPTER / "DocumentLifecycleCoordinator.cs")
context = read(ADAPTER / "ProjectContextCoordinator.cs")
state = read(CORE / "Domain/ProjectState.cs")
snapshot = read(CORE / "Persistence/ProjectStateSnapshot.cs")
store = read(CORE / "Persistence/QsdbProjectStore.cs")
stamp = read(CORE / "Persistence/ProjectPersistenceStamp.cs")
smoke = read(TESTS / "ProjectPersistenceLifecycleSmoke.cs")
registration = read(TESTS / "SmokeTestRegistration.cs")

for token in (
    "document.Database.SaveComplete += saveComplete",
    "document.BeginDocumentClose += beginClose",
    "document.Database.SaveComplete -= saveComplete",
    "ProjectContextCoordinator.TrySavePending(document, out var path)",
    "MessageBoxButton.YesNoCancel",
    "e.Veto()",
    "ProjectContextCoordinator.SaveRecoveryCopy(document, saveError)",
    "document.Database.SaveComplete -= saveComplete",
    "document.BeginDocumentClose -= beginClose",
):
    if token not in lifecycle:
        errors.append("document save/close lifecycle contract missing: " + token)

destroy = lifecycle.find("private static void OnDocumentToBeDestroyed")
detach = lifecycle.find("DetachProjectPersistence(document);", destroy)
forget = lifecycle.find("ProjectContextCoordinator.Forget(document);", destroy)
if destroy < 0 or detach < destroy or forget < detach:
    errors.append("document destruction must detach native persistence events before forgetting the cached project")

if destroy >= 0:
    destroy_end = lifecycle.find("private static void AttachProjectPersistence", destroy)
    destroy_body = lifecycle[destroy:destroy_end if destroy_end >= 0 else len(lifecycle)]
    if "ProjectContextCoordinator.Save(document)" in destroy_body:
        errors.append("DocumentToBeDestroyed must not blindly persist a project after the user may have discarded DWG changes")

for token in (
    "Dictionary<Document, ProjectPersistenceStamp> PersistenceStamps",
    "new ProjectPersistenceStamp(project)",
    "GetPersistenceStamp(document, project).MarkSaved(project)",
    "public static bool HasPendingChanges(Document document)",
    "public static bool TrySavePending(Document document, out string path)",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    'Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QS3D", "Recovery")',
    "ProjectFileLock.Acquire(recoveryPath)",
    "SafeRecoveryText(saveFailure.GetType().Name",
    "LimitFileStem(SafeFileStem(project.ProjectId), 80)",
    "PersistenceStamps.Remove(document)",
):
    if token not in context:
        errors.append("project persistence coordinator contract missing: " + token)

for token in (
    "public long ChangeVersion { get; private set; }",
    "ChangeVersion = checked(ChangeVersion + 1L)",
    "RestorePersistenceState(DateTime updatedUtc, long changeVersion)",
):
    if token not in state:
        errors.append("monotonic project dirty-state contract missing: " + token)

if "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion)" not in snapshot:
    errors.append("ProjectStateSnapshot rollback must restore the project change version")
for token in ("var previousChangeVersion = project.ChangeVersion;", "project.RestorePersistenceState(previousUpdatedUtc, previousChangeVersion);"):
    if token not in store:
        errors.append("failed QSDB save must restore the project change version: " + token)
for token in ("RequiresSave(ProjectState project)", "MarkSaved(ProjectState project)", "project.ChangeVersion != _savedChangeVersion"):
    if token not in stamp:
        errors.append("project persistence stamp contract missing: " + token)
for token in ("StampTracksSemanticChanges()", "SnapshotRollbackRestoresChangeVersion()", "StampRejectsAnotherProject()"):
    if token not in smoke:
        errors.append("project persistence lifecycle smoke coverage missing: " + token)
if "ProjectPersistenceLifecycleSmoke.Run();" not in registration:
    errors.append("project persistence lifecycle smoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: successful DWG saves persist pending QSDB state; close is explicit Save/Discard/Cancel; failed saves retain rollback-safe recovery")
