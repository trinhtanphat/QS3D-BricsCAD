from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectOnboardingService.cs").read_text(encoding="utf-8")
metadata_source = (root / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectOnboardingRevisionAtomicitySmoke.cs").read_text(encoding="utf-8")

bootstrap = source.index("public static ProjectOnboardingResult Bootstrap")
metadata_capacity_call = source.index("RequireMetadataCapacity(project, needsOverride);", bootstrap)
headroom_call = source.index(
    "RequireRevisionCapacity(project, needsOverride, effectiveUnit, existingFloorToActivate, plans);",
    bootstrap,
)
first_mutation = source.index("DrawingUnitResolutionPolicy.SetProjectOverride", bootstrap)
if metadata_capacity_call >= first_mutation:
    raise SystemExit("Project onboarding metadata-capacity admission must occur before the first mutation.")
if headroom_call >= first_mutation:
    raise SystemExit("Project onboarding revision-capacity admission must occur before the first mutation.")

required_tokens = [
    "private static void RequireMetadataCapacity(",
    "metadata.EnsureCanSetPublicKeys(keys);",
    "DrawingUnitResolutionPolicy.OverrideMetadataKey",
    "DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey",
    "DrawingUnitResolutionPolicy.BindingSourceMetadataKey",
    "private static void RequireRevisionCapacity(",
    "CountDrawingUnitOverrideRevisionAdvances(project.Metadata, effectiveUnit)",
    "private static long CountDrawingUnitOverrideRevisionAdvances(",
    "DrawingUnitResolutionPolicy.BoundMetadataKey",
    "DrawingUnitResolutionSource.ProjectOverride.ToString()",
    "private static long CountMetadataWrite(",
    "StringComparison.Ordinal",
    "if (project.Floors.Count == 0)",
    "requiredAdvances = checked(requiredAdvances + 2L);",
    "else if (existingFloorToActivate != null)",
    "requiredAdvances = checked(requiredAdvances + 1L);",
    "requiredAdvances + 2L + plan.Values.Count",
    "requiredAdvances > long.MaxValue - project.ChangeVersion",
    "insufficient remaining capacity",
]
for token in required_tokens:
    if token not in source:
        raise SystemExit("Missing onboarding revision/capacity contract token: " + token)

metadata_tokens = [
    "internal void EnsureCanSetPublicKeys(IEnumerable<string> keys)",
    "var finalKeys = new HashSet<string>(_items.Keys, StringComparer.OrdinalIgnoreCase);",
    "var canonicalKey = RequirePublicKey(key);",
    "if (finalKeys.Count >= MaximumEntries) throw MetadataCountError();",
    "finalKeys.Add(canonicalKey);",
]
for token in metadata_tokens:
    if token not in metadata_source:
        raise SystemExit("Missing project metadata capacity-admission contract token: " + token)

smoke_tokens = [
    "RejectsInsufficientRevisionCapacityBeforeMutation",
    "AcceptsExactRevisionCapacity",
    "RejectsBoundUnitMetadataInsufficientCapacityBeforeMutation",
    "AcceptsExactBoundUnitMetadataCapacity",
    "RejectsMetadataCapacityFailureBeforeRevisionMutation",
    "long.MaxValue - requiredAdvances + 1L",
    "long.MaxValue - requiredAdvances",
    "OnboardingCapacityFiller.",
    "BoundMetadataKey",
    "EffectiveUnitMetadataKey",
    "BindingSourceMetadataKey",
    "OverrideMetadataKey",
]
for token in smoke_tokens:
    if token not in smoke:
        raise SystemExit("Missing onboarding revision atomicity smoke token: " + token)

print("Project onboarding revision and metadata capacity atomicity preflight passed.")
