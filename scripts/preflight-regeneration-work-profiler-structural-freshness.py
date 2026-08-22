from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfilerStructuralFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfilerStructuralFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

method_start = source.index("public RegenerationWorkProfile ProfileSubset")
method_end = source.index("private RegenerationWorkProfile Build", method_start)
method = source[method_start:method_end]

for token in (
    "var elementOwnership = SnapshotElementOwnership(project);",
    "var inputVersion = project.ChangeVersion;",
    "var requested = CanonicalTargetIds(elementIds, sourceElementCount);",
    "if (project.ChangeVersion != inputVersion)",
    "RequireElementOwnershipUnchanged(project, elementOwnership);",
    "if (requested.Count == 0)",
):
    assert token in method, f"missing regeneration profiler structural-freshness contract: {token}"

snapshot = method.index("var elementOwnership = SnapshotElementOwnership(project);")
version = method.index("var inputVersion = project.ChangeVersion;")
enumeration = method.index("var requested = CanonicalTargetIds(elementIds, sourceElementCount);")
version_check = method.index("if (project.ChangeVersion != inputVersion)")
ownership_check = method.index("RequireElementOwnershipUnchanged(project, elementOwnership);")
empty_check = method.index("if (requested.Count == 0)")
assert snapshot < version < enumeration < version_check < ownership_check < empty_check, (
    "regeneration profiler structural freshness ordering drifted"
)

for token in (
    "private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership",
    "private static void RequireElementOwnershipUnchanged",
    "project.Elements.Count != expected.Count",
    "!seen.Add(element.Id)",
    "!expected.TryGetValue(element.Id, out var original)",
    "!ReferenceEquals(original, element)",
):
    assert token in source, f"missing regeneration profiler ownership guard: {token}"

for token in (
    "StableLazySubsetStillProfiles",
    "SameIdReplacementFailsClosed",
    "RemovalThenEmptyFailsClosed",
    "ReplaceSameIdThenYield",
    "RemoveThenEmpty",
    "project.Elements[index] = new ProjectElement(original.Id, original.Category);",
    "project.Elements.Remove(original)",
    "project.ChangeVersion != version",
):
    assert token in smoke, f"missing regeneration profiler structural smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "regeneration profiler structural smoke is not registered"
assert "RegenerationWorkProfilerStructuralFreshnessSmoke.Run();" in registration, (
    "regeneration profiler structural freshness registration drifted"
)

print("PASS: regeneration work profiler rejects structural ownership drift during lazy subset enumeration")
