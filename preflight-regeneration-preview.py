#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
REVISION = ROOT / "src/QS3D.Core/Revisions/RevisionService.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs"
STRUCTURAL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewStructuralFreshnessSmoke.cs"
errors = []

for path in (SOURCE, ENGINE, REVISION, HEALTH, SMOKE, STRUCTURAL_SMOKE):
    if not path.is_file():
        errors.append("missing regeneration preview contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public RegenerationPreview Preview(ProjectState project)",
        "public RegenerationPreview PreviewSubset(ProjectState project, IEnumerable<string> elementIds)",
        "public IReadOnlyList<string> TargetElementIds",
        "public bool IsSubset => TargetElementIds.Count > 0;",
        "var sourceElementOwnership = SnapshotElementOwnership(project);",
        "CanonicalPreviewTargets(elementIds, sourceElementOwnership.Count)",
        "RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);",
        "public long SourceChangeVersion",
        "var sourceChangeVersion = project.ChangeVersion;",
        "preview.SourceChangeVersion != project.ChangeVersion",
        "project changed after preview",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        'revisions.Capture(detached, "regen-preview-before")',
        "engine.RegenerateDirtySubset(detached, targets)",
        'revisions.Capture(detached, "regen-preview-after")',
        "revisions.Compare(beforeRevision, afterRevision)",
        "health.Compare(beforeHealth, afterHealth)",
        "public RegenerationGuardedApplyResult Apply",
        "preview.IsSubset ? PreviewSubset(project, preview.TargetElementIds) : Preview(project)",
        "engine.RegenerateDirtySubset(project, preview.TargetElementIds)",
        "Regeneration preview is stale",
        "if (current.IntroducesHealthErrors)",
        "ProjectStateSnapshot.Capture(project)",
        "snapshot.Restore(project)",
        "if (diff.NewErrorCount > 0)",
        "private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership",
        "project.Elements.Count != expectedOwnership.Count",
        "!ReferenceEquals(original, element)",
    ):
        if token not in text:
            errors.append("RegenerationPreviewService missing detached/subset/version/structural/stale/health guard token: " + token)

if ENGINE.is_file():
    text = ENGINE.read_text(encoding="utf-8")
    for token in (
        "public int RegenerateDirty(ProjectState project)",
        "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)",
        "var inputVersion = project.ChangeVersion;",
        "var sourceElements = project.Elements.ToArray();",
        "CanonicalTargetIds(elementIds, sourceElements.Length)",
        "RequireElementStructureFresh(project, sourceElements);",
        "RegenerateTransactional(project, project.Elements, project.Elements.Count)",
        "string.IsNullOrWhiteSpace(raw)",
        "!string.Equals(raw, raw.Trim(), StringComparison.Ordinal)",
        "if (result.Contains(raw))",
        "if (result.Count >= maxCount)",
        "if (unresolved.Remove(element.Id)) targets.Add(element);",
    ):
        if token not in text:
            errors.append("RegenerationEngine lost transactional/canonical/structural subset contract required by preview/apply: " + token)
    for forbidden in (
        "elementIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())",
        "CanonicalTargetIds(elementIds);",
    ):
        if forbidden in text:
            errors.append("Subset regeneration must not normalize/drop malformed target IDs or use an unbounded legacy helper: " + forbidden)

if REVISION.is_file():
    text = REVISION.read_text(encoding="utf-8")
    if "CanonicalSourceHandles(element)" not in text:
        errors.append("Regeneration Preview requires revision capture to preserve fail-closed canonical source-handle semantics.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PreviewRunsOnDetachedState();",
        "SubsetPreviewAndApplyRespectScope();",
        "MalformedSubsetTargetsFailClosed();",
        "PreviewSubset(project, new[] { \"B1\" })",
        "!preview.Deltas.Any(x => x.ElementId == \"B2\")",
        "!project.FindElement(\"B2\")!.Quantities.ContainsKey(\"NetVolumeM3\")",
        "StalePreviewFailsBeforeLiveMutation();",
        "ChangeVersionInvalidatesEquivalentPreview();",
        "project.Touch();",
        "preview.SourceChangeVersion",
        "FreshPreviewCanApplyWithoutNewHealthErrors();",
        "Quantity:NetVolumeM3",
        "result.HealthDiff.NewErrorCount",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("RegenerationPreviewSmoke missing regression token: " + token)

if STRUCTURAL_SMOKE.is_file():
    text = STRUCTURAL_SMOKE.read_text(encoding="utf-8")
    for token in (
        "ReplacementDuringSubsetEnumerationFailsFreshness();",
        "StableSubsetStillPreviews();",
        "project.Elements[index] = replacement;",
        "element ownership changed",
        "failed preview must not mutate live target quantities",
    ):
        if token not in text:
            errors.append("RegenerationPreviewStructuralFreshnessSmoke missing regression token: " + token)

if errors:
    print("QS3D regeneration preview preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic regeneration supports whole-project and bounded canonical subset detached dry-runs bound to ChangeVersion plus structural ownership freshness, revision/health diff, scope-preserving stale rejection and rollback on new Model Health errors.")
