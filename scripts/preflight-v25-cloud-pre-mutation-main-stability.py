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

    first_release_post = text.find("$release = Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"")
    main_fetch = text.find("$preMutationMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"")
    fetched_ref = text.find("$preMutationMainRef = 'refs/remotes/origin/qs3d-release-pre-mutation-main'")
    ancestry = text.find("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationMain")
    classifier = text.find("$preMutationReleaseRelevantPaths = @(")
    diff_probe = text.find("git diff --quiet --no-ext-diff $env:SOURCE_SHA $preMutationMain -- $preMutationReleaseRelevantPaths")
    status_capture = text.find("$preMutationReleaseDriftStatus = $LASTEXITCODE")
    drift_reject = text.find("if ($preMutationReleaseDriftStatus -eq 1)")
    git_error_reject = text.find("if ($preMutationReleaseDriftStatus -ne 0)")
    second_main = text.find("$preMutationPublishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"")
    second_equal = text.find("if ($preMutationPublishMain -ne $preMutationMain)")

    if first_release_post < 0:
        errors.append("missing V25 draft release POST")
    if main_fetch < 0 or fetched_ref < 0:
        errors.append("missing authenticated protected-main API/fetch binding before remote release mutation")
    if ancestry < 0:
        errors.append("missing SOURCE_SHA ancestry admission before remote release mutation")
    if classifier < 0:
        errors.append("missing pre-mutation release-relevant protected-main classifier")
    if diff_probe < 0:
        errors.append("missing scoped SOURCE_SHA..preMutationMain release-relevant diff probe")
    if status_capture < 0 or drift_reject < 0 or git_error_reject < 0:
        errors.append("pre-mutation diff probe must distinguish release drift from git failure and fail closed")
    if second_main < 0 or second_equal < 0:
        errors.append("missing second exact protected-main stability confirmation before draft creation")

    if classifier >= 0:
        end = text.find("\n          )", classifier)
        if end < 0:
            errors.append("pre-mutation release-relevant classifier is not bounded")
        else:
            block = text[classifier:end]
            for required in REQUIRED_PATHS:
                literal = f"'{required}'"
                if block.count(literal) != 1:
                    errors.append(f"pre-mutation classifier requires exactly one {required} literal")

    ordered = [
        main_fetch,
        fetched_ref,
        ancestry,
        classifier,
        diff_probe,
        status_capture,
        drift_reject,
        git_error_reject,
        second_main,
        second_equal,
        first_release_post,
    ]
    if all(position >= 0 for position in ordered) and ordered != sorted(ordered):
        errors.append("protected-main binding/classification/reconfirmation must complete before the first V25 release mutation")

    return errors


errors = validate(workflow)
if errors:
    raise SystemExit("V25 cloud pre-mutation protected-main admission failed: " + "; ".join(errors))

mutations = {
    "initial main API fetch removal": workflow.replace("$preMutationMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", "$removedPreMutationMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", 1),
    "scoped diff removal": workflow.replace("git diff --quiet --no-ext-diff $env:SOURCE_SHA $preMutationMain -- $preMutationReleaseRelevantPaths", "git diff --quiet --no-ext-diff $env:SOURCE_SHA $preMutationMain", 1),
    "second-main removal": workflow.replace("$preMutationPublishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", "$removedPreMutationPublishMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"", 1),
    "fail-open git error": workflow.replace("if ($preMutationReleaseDriftStatus -ne 0)", "if ($preMutationReleaseDriftStatus -eq 99)", 1),
    "post-before-fence": workflow.replace("$release = Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"", "$release = Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"\n          # mutation probe marker", 1).replace("$preMutationMainResponse = Invoke-RestMethod", "$release = Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\" -Headers $headers -ContentType 'application/json' -Body $body\n          $preMutationMainResponse = Invoke-RestMethod", 1),
}
for name, mutated in mutations.items():
    if mutated == workflow:
        raise SystemExit(f"V25 cloud pre-mutation admission mutation fixture could not apply: {name}")
    if not validate(mutated):
        raise SystemExit(f"V25 cloud pre-mutation admission mutation probe did not fail closed: {name}")

print("PASS V25 cloud protected-main drift is rejected before the first persistent GitHub release mutation")
