#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Persistence/ProjectSidecarRevisionStamp.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
CONTEXT = ROOT / "src/QS3D.BricsCAD.V25/ExistingProjectMutationContext.cs"
CONFIRMATION = ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeConfirmationGuard.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectSidecarRevisionStampSmoke.cs"
PERSISTENCE_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PersistenceHardeningSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (CORE, COORDINATOR, CONTEXT, CONFIRMATION, SMOKE, PERSISTENCE_SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing sidecar freshness contract: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    # Capture now names the backup path before opening it; pin that semantic form instead of the retired inline expression.
    for token in (
        "MaxSidecarBytes = 64L * 1024L * 1024L",
        'var backupPath = fullPath + ".bak";',
        "FileCapture.Open(backupPath)",
        "new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)",
        "SHA256.Create()",
        "CaptureStableRevision()",
        "EnsurePresenceUnchanged()",
        "var second = ComputeDigest(_stream);",
        "public bool MatchesCurrent()",
        "public bool IsForPath(string primaryPath)",
    ):
        if token not in text:
            errors.append("ProjectSidecarRevisionStamp missing bounded content-revision token: " + token)
    for forbidden in ("public string PrimaryPath", "public byte[]", "Convert.ToBase64String", "BitConverter.ToString"):
        if forbidden in text:
            errors.append("ProjectSidecarRevisionStamp must not expose path/digest evidence: " + forbidden)

if COORDINATOR.is_file():
    text = COORDINATOR.read_text(encoding="utf-8")
    for token in (
        "Dictionary<Document, ProjectSidecarRevisionStamp> SidecarRevisionStamps",
        "var before = ProjectSidecarRevisionStamp.Capture(path);",
        "EnsureStableCapture(before, after",
        "EnsureBackingStoreUnchanged(document, existing, false",
        "EnsureBackingStoreUnchanged(document, existing, allowPathTransition",
        "SidecarRevisionStamps[document] = ProjectSidecarRevisionStamp.Capture(path);",
        "SidecarRevisionStamps.Remove(document);",
        "public static void RequireBackingStoreUnchanged(Document document, ProjectState project, string operation)",
        "baseline.MatchesCurrent()",
        "baseline.IsForPath(currentPath)",
        "refused to overwrite an existing QS3D sidecar at the new DWG path",
        'using (ProjectFileLock.Acquire(path))',
        'EnsureBackingStoreUnchanged(document, project, true, "QS3D save")',
        "Store.SaveNew(project, path)",
        "Store.SavePreservingValidatedBackup(project, path)",
    ):
        if token not in text:
            errors.append("ProjectContextCoordinator missing warm-cache backing-store guard: " + token)
    save_start = text.find("public static string Save(Document document)")
    save_end = text.find("public static ProjectState Reload(Document document)", save_start)
    save = text[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
    lock = save.find("using (ProjectFileLock.Acquire(path))")
    freshness = save.find('EnsureBackingStoreUnchanged(document, project, true, "QS3D save")')
    store = save.find("Store.Save(project, path)")
    revise = save.find("SidecarRevisionStamps[document] = ProjectSidecarRevisionStamp.Capture(path)")
    mark = save.find("MarkSaved(project)")
    lock_end = save.find("CleanupObsoleteUnsavedProject(document, path)")
    if min(lock, freshness, store, revise, mark, lock_end) < 0 or not lock < freshness < store < revise < mark < lock_end:
        errors.append("Save must lock, revalidate, commit QSDB, capture that revision and mark saved before releasing the write boundary")
    else:
        opening = save.find("{", lock)
        depth = 0
        closing = -1
        for index in range(opening, len(save)):
            if save[index] == "{":
                depth += 1
            elif save[index] == "}":
                depth -= 1
                if depth == 0:
                    closing = index
                    break
        if opening < 0 or closing < 0 or not mark < closing < lock_end:
            errors.append("committed sidecar capture and MarkSaved must remain inside the ProjectFileLock using block")

if CONTEXT.is_file():
    text = CONTEXT.read_text(encoding="utf-8")
    token = 'ProjectContextCoordinator.RequireBackingStoreUnchanged(document, canonical, "QS3D existing-project mutation")'
    if token not in text:
        errors.append("ExistingProjectMutationContext must verify backing-store revision before returning canonical state")

if CONFIRMATION.is_file():
    text = CONFIRMATION.read_text(encoding="utf-8")
    token = "ProjectContextCoordinator.RequireBackingStoreUnchanged(document, currentProject, operation)"
    if token not in text:
        errors.append("Interchange confirmation must verify backing-store revision after modal review")
    freshness = text.find("currentProject.ChangeVersion != reviewedChangeVersion")
    sidecar = text.find(token)
    returned = text.find("return currentProject;")
    if min(freshness, sidecar, returned) < 0 or not freshness < sidecar < returned:
        errors.append("Interchange confirmation ordering must be semantic freshness -> sidecar freshness -> return")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "Stable absent sidecar pair",
        "New primary sidecar",
        "Changed primary content",
        "New backup sidecar",
        "Changed backup content",
        "Removed backup",
        "Removed primary",
        "64L * 1024L * 1024L + 1L",
        "Directory.CreateDirectory(primary)",
    ):
        if token not in text:
            errors.append("sidecar revision smoke missing regression: " + token)

if PERSISTENCE_SMOKE.is_file():
    text = PERSISTENCE_SMOKE.read_text(encoding="utf-8")
    for token in ("RecoverySavePreservesValidatedBackup", "PublishNewRejectsExistingPair"):
        if token not in text:
            errors.append("QSDB conditional/recovery publication smoke missing regression: " + token)

if REGISTRATION.is_file() and "ProjectSidecarRevisionStampSmoke.Run();" not in REGISTRATION.read_text(encoding="utf-8"):
    errors.append("sidecar revision smoke is not registered")

print("QS3D sidecar revision freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cached project access, existing-project mutations and Interchange confirmation fail closed when bounded primary/backup content revisions change; save updates the private revision only after commit.")
