#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFamilyService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyAssignmentKnownCountCurrentSmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "project-family-assignment-known-count-current-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit(
            "ProjectFamilyService Count/Current integrity preflight missing file: "
            + str(path.relative_to(ROOT))
        )

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "using (var enumerator = elements.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (expectedKnownCount.HasValue && observedEntries >= expectedKnownCount.Value)",
    "if (observedEntries >= MaxAssignmentTargetEntries)",
    "var element = enumerator.Current;",
    "observedEntries++;",
    "if (project.ChangeVersion != targetEnumerationVersion)",
    "if (expectedKnownCount.HasValue && observedEntries != expectedKnownCount.Value)",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("ProjectFamilyService Count/Current source contract missing: " + repr(missing))

resolve_start = source.index(
    "private static IReadOnlyList<ProjectElement> ResolveOwnedElements(ProjectState project, IEnumerable<ProjectElement> elements, ProjectFamily target)"
)
count_helper_start = source.index(
    "private static int? RequireAssignmentTargetCountWithinLimit(IEnumerable<ProjectElement> elements)",
    resolve_start,
)
resolve = source[resolve_start:count_helper_start]

move_next = resolve.index("while (enumerator.MoveNext())")
known_guard = resolve.index(
    "if (expectedKnownCount.HasValue && observedEntries >= expectedKnownCount.Value)",
    move_next,
)
cap_guard = resolve.index("if (observedEntries >= MaxAssignmentTargetEntries)", known_guard)
current = resolve.index("var element = enumerator.Current;", cap_guard)
count_increment = resolve.index("observedEntries++;", current)
freshness = resolve.index("if (project.ChangeVersion != targetEnumerationVersion)", count_increment)
under_yield = resolve.index(
    "if (expectedKnownCount.HasValue && observedEntries != expectedKnownCount.Value)",
    freshness,
)
publication = resolve.index("return unique.Values", under_yield)

if not (
    move_next
    < known_guard
    < cap_guard
    < current
    < count_increment
    < freshness
    < under_yield
    < publication
):
    raise SystemExit("ProjectFamilyService Count/Current traversal or publication ordering changed.")

if "foreach (var element in elements)" in resolve:
    raise SystemExit(
        "ProjectFamilyService caller-controlled assignment traversal must not regress to foreach before Count admission."
    )

required_smoke = (
    "KnownCountOverrunRejectsBeforeCurrent",
    "StreamingHardLimitRejectsBeforeCurrent",
    "MoveNextCalls",
    "CurrentReads",
    "Equal(2, source.MoveNextCalls,",
    "Equal(1, source.CurrentReads,",
    "Equal(10001, source.MoveNextCalls,",
    "Equal(10000, source.CurrentReads,",
    "POISON counted Current beyond known Count.",
    "POISON streaming Current beyond Family assignment hard limit.",
    "[ModuleInitializer]",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("ProjectFamilyService Count/Current smoke contract missing: " + repr(missing_smoke))

print("PASS ProjectFamilyService known-Count Current observation integrity")
