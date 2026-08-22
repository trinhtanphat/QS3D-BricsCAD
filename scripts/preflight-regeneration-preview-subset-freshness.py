#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
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
        count = body.find("var sourceElementCount = project.Elements.Count;")
        targets = body.find("CanonicalPreviewTargets(elementIds, sourceElementCount)")
        call = body.find("PreviewInternal(project, targets, sourceChangeVersion)")
        if min(version, count, targets, call) < 0 or not (version < count < targets < call):
            errors.append("subset preview must snapshot revision/cardinality before caller target enumeration and pass immutable revision forward")
        if "CanonicalPreviewTargets(elementIds, project.Elements.Count)" in body:
            errors.append("subset preview must not read live project cardinality inside target enumeration setup")

    internal_start = source.find("private RegenerationPreview PreviewInternal(ProjectState project, IReadOnlyList<string> targets, long sourceChangeVersion)")
    canonical_start = source.find("private static IReadOnlyList<string> CanonicalPreviewTargets", internal_start)
    if internal_start < 0 or canonical_start <= internal_start:
        errors.append("cannot isolate freshness-bound RegenerationPreviewService.PreviewInternal")
    else:
        body = source[internal_start:canonical_start]
        guard = body.find("if (project.ChangeVersion != sourceChangeVersion)")
        snapshot = body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        if min(guard, snapshot) < 0 or guard >= snapshot:
            errors.append("preview internal freshness guard must reject scope changes before detached snapshot creation")

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

print("QS3D regeneration preview subset freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: subset regeneration preview binds project revision/cardinality before caller target enumeration and fails closed if scope establishment changes the project.")
