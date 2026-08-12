from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/PhysicalOpeningSemanticTargetFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/PhysicalOpeningSemanticTargetFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

method_start = source.index("public static IReadOnlyList<ProjectElement> Resolve")
method_end = source.index("public static void Write", method_start)
method = source[method_start:method_end]

for token in (
    "ValidateProjectElements(project);",
    "var canonicalHost = project.FindElement(host.Id);",
    "ReferenceEquals(canonicalHost, host)",
    "var targetEnumerationVersion = project.ChangeVersion;",
    "var ids = Normalize(openingIds);",
    "if (project.ChangeVersion != targetEnumerationVersion)",
    "Project changed while physical opening target ids were being enumerated",
    "if (ids.Count == 0)",
    "var currentHost = project.FindElement(canonicalHost.Id);",
    "ReferenceEquals(currentHost, canonicalHost)",
    'opening.Properties.TryGetValue("HostWallId", out var linkedHostId)',
):
    assert token in method, f"missing physical opening semantic-freshness contract: {token}"

capture = method.index("var targetEnumerationVersion = project.ChangeVersion;")
enumeration = method.index("var ids = Normalize(openingIds);")
version_check = method.index("if (project.ChangeVersion != targetEnumerationVersion)")
empty_check = method.index("if (ids.Count == 0)")
second_integrity = method.index("ValidateProjectElements(project);", method.index("var ids = Normalize(openingIds);"))
assert capture < enumeration < version_check < empty_check < second_integrity, (
    "physical opening semantic freshness ordering drifted"
)

for token in (
    "StableLazyTargetsStillResolve",
    "TouchThenYieldFailsClosed",
    "TouchThenEmptyFailsClosed",
    "PhysicalOpeningCutTargetStateCodec.Resolve(project, host, TouchThenYield(project))",
    "PhysicalOpeningCutTargetStateCodec.Resolve(project, host, TouchThenEmpty(project))",
    "project.Touch();",
    "yield break;",
):
    assert token in smoke, f"missing physical opening semantic-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "physical opening semantic-freshness smoke is not registered"
assert "PhysicalOpeningSemanticTargetFreshnessSmoke.Run();" in registration, (
    "physical opening semantic-freshness registration drifted"
)

print("PASS: physical opening target resolution fails closed across ProjectState.ChangeVersion changes")
