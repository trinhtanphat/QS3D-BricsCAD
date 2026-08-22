#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs"
STRUCTURAL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewStructuralFreshnessSmoke.cs"
errors = []

for path in (SOURCE, SMOKE, STRUCTURAL_SMOKE):
    if not path.is_file():
        errors.append("missing regeneration preview subset freshness contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    subset_start = source.find("public RegenerationPreview PreviewSubset(ProjectState project, IEnumerable<string> elementIds)")
    apply_start = source.find("public RegenerationGuardedApplyResult Apply", subset_start)
    if subset_start < 0 or apply_start <= subset_start:
        errors.append("cannot isolate RegenerationPreviewService.PreviewSubset")
    else:
        body = source[subset_start:apply_start]
        version = body.find("var sourceChangeVersion = project.ChangeVersion;")
        ownership = body.find("var sourceElementOwnership = SnapshotElementOwnership(project);")
        targets = body.find("CanonicalPreviewTargets(elementIds, sourceElementOwnership.Count)")
        first_fresh = body.find("RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);")
        call = body.find("PreviewInternal(project, targets, sourceChangeVersion)")
        second_fresh = body.find("RequireProjectFresh(project, sourceChangeVersion, sourceElementOwnership);", first_fresh + 1)
        returned = body.find("return preview;", call)
        if min(version, ownership, targets, first_fresh, call, second_fresh, returned) < 0 or not (
            version < ownership < targets < first_fresh < call < second_fresh < returned
        ):
            errors.append("subset preview must snapshot revision/ownership before caller target enumeration and validate structural freshness before and after detached preview")
        if "CanonicalPreviewTargets(elementIds, project.Elements.Count)" in body:
            errors.append("subset preview must not read live project cardinality inside target enumeration setup")
        if "var sourceElementCount = project.Elements.Count;" in body:
            errors.append("subset preview regressed to count-only freshness instead of captured ownership identity")

    internal_start = source.find("private RegenerationPreview PreviewInternal(ProjectState project, IReadOnlyList<string> targets, long sourceChangeVersion)")
    snapshot_ownership_start = source.find("private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership", internal_start)
    if internal_start < 0 or snapshot_ownership_start <= internal_start:
        errors.append("cannot isolate freshness-bound RegenerationPreviewService.PreviewInternal")
    else:
        body = source[internal_start:snapshot_ownership_start]
        guard = body.find("if (project.ChangeVersion != sourceChangeVersion)")
        snapshot = body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        if min(guard, snapshot) < 0 or guard >= snapshot:
            errors.append("preview internal freshness guard must reject revision changes before detached snapshot creation")

    freshness_start = source.find("private static void RequireProjectFresh(")
    canonical_start = source.find("private static IReadOnlyList<string> CanonicalPreviewTargets", freshness_start)
    if freshness_start < 0 or canonical_start <= freshness_start:
        errors.append("cannot isolate regeneration preview structural freshness helper")
    else:
        body = source[freshness_start:canonical_start]
        for token in (
            "project.ChangeVersion != expectedChangeVersion",
            "project.Elements.Count != expectedOwnership.Count",
            "var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
            "!expectedOwnership.TryGetValue(element.Id, out var original)",
            "!ReferenceEquals(original, element)",
        ):
            if token not in body:
                errors.append("regeneration preview structural freshness helper missing: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "MutationDuringSubsetTargetEnumerationFailsFreshness();",
        "private static void MutationDuringSubsetTargetEnumerationFailsFreshness()",
        "IEnumerable<string> Targets()",
        "project.Touch();",
        "PreviewSubset(project, Targets())",
        "beforeVersion + 1L",
    ):
        if token not in smoke:
            errors.append("regeneration preview smoke missing subset freshness assertion: " + token)

if STRUCTURAL_SMOKE.is_file():
    smoke = STRUCTURAL_SMOKE.read_text(encoding="utf-8")
    for token in (
        "ReplacementDuringSubsetEnumerationFailsFreshness();",
        "StableSubsetStillPreviews();",
        "project.Elements[index] = replacement;",
        "beforeVersion, project.ChangeVersion",
        "element ownership changed",
    ):
        if token not in smoke:
            errors.append("regeneration preview structural freshness smoke missing assertion: " + token)

print("QS3D regeneration preview subset freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: subset regeneration preview binds revision plus semantic element ownership before caller target enumeration, bounds the target set by captured ownership, and fails closed on ChangeVersion or same-id structural replacement drift.")
