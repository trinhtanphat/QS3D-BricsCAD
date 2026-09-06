#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow_path = root / ".github" / "workflows" / "release-v25-cloud.yml"
workflow = workflow_path.read_text(encoding="utf-8")

REQUIRED_PATHS = (
    "src/",
    "tests/",
    "scripts/",
    "external/QS3D-Platform",
    ".gitmodules",
    "Directory.Build.props",
    "QS3D.sln",
    ".github/workflows/release-v25-cloud.yml",
)


def validate(text: str) -> list[str]:
    errors: list[str] = []

    ancestry = text.find("git merge-base --is-ancestor $env:SOURCE_SHA $finalMain")
    classifier = text.find("$finalReleaseRelevantPaths = @(")
    diff_probe = text.find("git diff --quiet --no-ext-diff $env:SOURCE_SHA $finalMain -- $finalReleaseRelevantPaths")
    second_main = text.find("$publishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"")
    second_equal = text.find("if ($publishMain -ne $finalMain)")
    publish = text.find("$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri")

    if ancestry < 0:
        errors.append("missing final SOURCE_SHA ancestry admission")
    if classifier < 0:
        errors.append("missing final release-relevant protected-main classifier")
    if diff_probe < 0:
        errors.append("missing fail-closed SOURCE_SHA..finalMain release-relevant diff probe")
    if second_main < 0 or second_equal < 0:
        errors.append("missing second protected-main identity confirmation immediately before publish")
    if publish < 0:
        errors.append("missing final publication PATCH")

    if classifier >= 0:
        end = text.find("\n          )", classifier)
        if end < 0:
            errors.append("final release-relevant classifier is not bounded")
        else:
            block = text[classifier:end]
            for required in REQUIRED_PATHS:
                literal = f"'{required}'"
                if block.count(literal) != 1:
                    errors.append(f"final release-relevant classifier requires exactly one {required} literal")

    if diff_probe >= 0:
        status_capture = text.find("$finalReleaseDriftStatus = $LASTEXITCODE", diff_probe)
        drift_reject = text.find("if ($finalReleaseDriftStatus -eq 1)", diff_probe)
        error_reject = text.find("if ($finalReleaseDriftStatus -ne 0)", diff_probe)
        if status_capture < 0 or drift_reject < 0 or error_reject < 0:
            errors.append("release-relevant diff probe must distinguish drift from git failure and fail closed")

    ordered = [ancestry, classifier, diff_probe, second_main, second_equal, publish]
    if all(position >= 0 for position in ordered) and ordered != sorted(ordered):
        errors.append("final-main ancestry/classification/reconfirmation must occur before publication in fail-closed order")

    return errors


errors = validate(workflow)
if errors:
    raise SystemExit("V25 cloud final-main drift admission failed: " + "; ".join(errors))

mutations = {
    "classifier removal": workflow.replace("$finalReleaseRelevantPaths = @(", "$removedFinalReleaseRelevantPaths = @(", 1),
    "diff removal": workflow.replace("git diff --quiet --no-ext-diff $env:SOURCE_SHA $finalMain -- $finalReleaseRelevantPaths", "git diff --quiet --no-ext-diff $env:SOURCE_SHA $finalMain", 1),
    "second-main removal": workflow.replace("$publishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", "$removedPublishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", 1),
    "fail-open git error": workflow.replace("if ($finalReleaseDriftStatus -ne 0)", "if ($finalReleaseDriftStatus -eq 99)", 1),
}
for name, mutated in mutations.items():
    if mutated == workflow:
        raise SystemExit(f"V25 cloud final-main drift mutation fixture could not apply: {name}")
    if not validate(mutated):
        raise SystemExit(f"V25 cloud final-main drift mutation probe did not fail closed: {name}")

print("PASS V25 cloud publication rejects release-relevant protected-main drift and re-confirms exact main before PATCH")
