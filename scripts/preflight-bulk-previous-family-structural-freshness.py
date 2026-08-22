from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/BulkEditService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/BulkPreviousFamilyStructuralFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/BulkPreviousFamilyStructuralFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

method_start = source.index("public int AssignFamily(ProjectState project, IEnumerable<string> elementIds, string familyId)")
method_end = source.index("private static IReadOnlyList<ProjectElement> OwnedDistinctByIds", method_start)
method = source[method_start:method_end]

for token in (
    "var familyOwnership = SnapshotFamilyOwnership(project);",
    "var beforeTargetEnumeration = project.ChangeVersion;",
    "var targets = OwnedDistinctByIds(project, elementIds);",
    'RequireTargetEnumerationFreshness(project, beforeTargetEnumeration, "Bulk Family target-id enumeration");',
    "RequireFamilyOwnershipUnchanged(project, familyOwnership);",
    "RequireCurrentFamilyAssignmentOwnership(project, family, targets);",
    'var previousFamily = project.FindFamily(previousFamilyId)',
):
    assert token in method, f"missing bulk previous-Family structural-freshness contract: {token}"

snapshot = method.index("var familyOwnership = SnapshotFamilyOwnership(project);")
version = method.index("var beforeTargetEnumeration = project.ChangeVersion;")
enumeration = method.index("var targets = OwnedDistinctByIds(project, elementIds);")
version_check = method.index("RequireTargetEnumerationFreshness(project, beforeTargetEnumeration")
family_check = method.index("RequireFamilyOwnershipUnchanged(project, familyOwnership);")
existing_check = method.index("RequireCurrentFamilyAssignmentOwnership(project, family, targets);")
previous_read = method.index("var previousFamily = project.FindFamily(previousFamilyId)")
assert snapshot < version < enumeration < version_check < existing_check < family_check < previous_read, (
    "bulk Family structural freshness guard precedence drifted"
)

for token in (
    "private static IReadOnlyDictionary<string, ProjectFamily> SnapshotFamilyOwnership",
    "private static void RequireFamilyOwnershipUnchanged",
    "project.Families.Count != expected.Count",
    "!seen.Add(family.Id)",
    "!expected.TryGetValue(family.Id, out var original)",
    "!ReferenceEquals(original, family)",
    "Target Family no longer belongs to the project after bulk assignment target enumeration",
):
    assert token in source, f"missing bulk previous-Family ownership guard: {token}"

for token in (
    "StableLazyAssignmentMigratesInheritedDefault",
    "SameIdPreviousFamilyReplacementFailsClosed",
    "PreviousFamilyRemovalThenEmptyFailsClosed",
    "ReplacePreviousFamilyThenYield",
    "RemovePreviousFamilyThenEmpty",
    'replacement.Properties["Width"] = "9.9";',
    'targetFamily.Properties["Width"] = "0.8";',
    "project.ChangeVersion != version",
):
    assert token in smoke, f"missing bulk previous-Family structural smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "bulk previous-Family structural smoke is not registered"
assert "BulkPreviousFamilyStructuralFreshnessSmoke.Run();" in registration, (
    "bulk previous-Family structural freshness registration drifted"
)

print("PASS: bulk Family assignment rejects previous-Family ownership drift during lazy target enumeration")
