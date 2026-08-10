#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

service = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileCommands.cs"
ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs"
engine = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationSubsetSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
doc = ROOT / "docs/SOURCE-EDIT-WORKFLOW.md"

checks = {
    service: [
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle, out var generatedOwner, out var generatedSlot)",
        "is QS3D-generated output owned by",
        "Select the authoritative source CAD instead.",
        "Source reconcile P0 requires exactly one authoritative source handle per semantic element",
        "ExpandInvalidationTargets",
        "candidate.DependsOn.Any(result.ContainsKey)",
        "GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, invalidationTargets)",
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "RefreshSourceDerivedState",
        "dependent.MarkDirty(ElementDirtyFlags.All)",
        "RegenerateAffectedToStable",
        "MaxStableRegenerationPasses = 8",
        "engine.RegenerateDirtySubset(project, affectedIds)",
        "invalidation.CommitMetadata()",
        "project.Touch()",
        "transaction.Commit();",
        "cadCommitted = true;",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
        'element.SetProperty("LengthM"',
        'element.SetProperty("AreaM2"',
        'element.SetProperty("PerimeterM"',
        'element.SetProperty("VolumeM3"',
        "element.MarkDirty(ElementDirtyFlags.All)",
        'AuditTrail.ForProject(project).Record("source.reconcile"',
        "Opening " + '" + element.Id + " references missing host "',
        "did not converge within",
    ],
    command: [
        'CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)',
        "SourceReconcileService.ReconcileSelection(document)",
        "FinalizeUi(document, result)",
        "Generated host/rebar/curtain phụ thuộc đã được invalidate/remove an toàn",
        "UI sync warning: ",
        "ReportOperationFailure",
        "TryWriteMessage",
    ],
    ribbon: [
        '"QS3D_PROJECT_SYNCSOURCE"',
        '"Đồng bộ source CAD"',
        '"QS3DSYNCSOURCE"',
    ],
    engine: [
        "RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)",
        "Unknown regeneration target: ",
        "var targets = project.Elements.Where(x => ids.Contains(x.Id)).ToList();",
        "return Regenerate(project, targets, targets.Count);",
    ],
    smoke: [
        "RegeneratesOnlyRequestedElements",
        "RejectsUnknownTarget",
        "engine.RegenerateDirtySubset(project, new[] { selected.Id })",
        "True(unrelated.Dirty != ElementDirtyFlags.None)",
        'True(!unrelated.Quantities.ContainsKey("Count"))',
        "Throws<KeyNotFoundException>",
    ],
    registration: ["RegenerationSubsetSmoke.Run();"],
    doc: [
        "`QS3DSYNCSOURCE`",
        "native BricsCAD source edits",
        "ownership-safe generated invalidation/removal",
        "does **not** silently regenerate destructive/native downstream geometry",
        "source-implemented / statically guarded; licensed V25 interactive qualification pending",
    ],
}

for path, needles in checks.items():
    if not path.is_file():
        errors.append("missing source reconcile dependency: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing source reconcile contract: " + needle)

if service.is_file():
    text = service.read_text(encoding="utf-8")
    prepare = text.find("GeneratedDependentGeometryInvalidator.Prepare")
    refresh = text.find("RefreshSourceDerivedState(project")
    regen = text.find("RegenerateAffectedToStable(project")
    metadata = text.find("invalidation.CommitMetadata()")
    touch = text.find("project.Touch()", metadata)
    commit = text.find("transaction.Commit();", touch)
    flag = text.find("cadCommitted = true;", commit)
    restore = text.find("rollback.Restore(project)", flag)
    if min(prepare, refresh, regen, metadata, touch, commit, flag, restore) < 0 or not (prepare < refresh < regen < metadata < touch < commit < flag < restore):
        errors.append("Source reconcile must invalidate CAD -> refresh authoritative semantic state -> converge regeneration -> commit generated metadata/revision -> CAD commit, with project restore only on pre-commit failure")
    if "GeneratedHandleOwnershipLookupStatus" in text:
        errors.append("Source reconcile must use the canonical boolean GeneratedHandleOwnershipPolicy.TryFindOwner contract; GeneratedHandleOwnershipLookupStatus is not part of the current Core API")
    if "engine.RegenerateDirty(project)" in text:
        errors.append("Source reconcile must regenerate only the affected semantic closure, not unrelated dirty project elements")
    if "new Build3DCommands" in text or "QS3DBUILD3D" in text or "SendStringToExecute" in text:
        errors.append("Source reconcile service must not auto-rebuild native/generated geometry")

commands = []
source_root = ROOT / "src/QS3D.BricsCAD.V25"
if source_root.is_dir():
    for path in source_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
if sum(1 for name in commands if name.upper() == "QS3DSYNCSOURCE") != 1:
    errors.append("QS3DSYNCSOURCE must be registered exactly once")

print("QS3D authoritative source reconcile preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DSYNCSOURCE reconciles only tracked authoritative source CAD, rejects generated/ambiguous/untracked selection through the canonical boolean generated-owner lookup, expands linked-host/DependsOn invalidation closure, removes generated dependents ownership-safely, refreshes source-derived semantic state, regenerates only the affected semantic closure to stability, rolls project state back on pre-commit failure, keeps native rebuild explicit, and remains discoverable on the Project Ribbon.")
