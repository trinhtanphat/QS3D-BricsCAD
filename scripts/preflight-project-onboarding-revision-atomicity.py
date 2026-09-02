from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectOnboardingService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectOnboardingRevisionAtomicitySmoke.cs").read_text(encoding="utf-8")

bootstrap = source.index("public static ProjectOnboardingResult Bootstrap")
headroom_call = source.index("RequireRevisionCapacity(project, needsOverride, existingFloorToActivate, plans);", bootstrap)
first_mutation = source.index("DrawingUnitResolutionPolicy.SetProjectOverride", bootstrap)
if headroom_call >= first_mutation:
    raise SystemExit("Project onboarding revision-capacity admission must occur before the first mutation.")

required_tokens = [
    "private static void RequireRevisionCapacity(",
    "requiredAdvances = needsOverride ? 1L : 0L",
    "project.Floors.Count == 0 || existingFloorToActivate != null",
    "requiredAdvances + 2L + plan.Values.Count",
    "requiredAdvances > long.MaxValue - project.ChangeVersion",
    "insufficient remaining capacity",
]
for token in required_tokens:
    if token not in source:
        raise SystemExit("Missing onboarding revision-capacity contract token: " + token)

smoke_tokens = [
    "RejectsInsufficientRevisionCapacityBeforeMutation",
    "AcceptsExactRevisionCapacity",
    "long.MaxValue - requiredAdvances + 1L",
    "long.MaxValue - requiredAdvances",
    "Rejected onboarding Floors",
    "Rejected onboarding Families",
    "OverrideMetadataKey",
]
for token in smoke_tokens:
    if token not in smoke:
        raise SystemExit("Missing onboarding revision atomicity smoke token: " + token)

print("Project onboarding revision atomicity preflight passed.")
