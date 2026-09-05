#!/usr/bin/env python3
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow_path = root / ".github" / "workflows" / "release-v25.yml"
gitmodules_path = root / ".gitmodules"
workflow = workflow_path.read_text(encoding="utf-8")
gitmodules = gitmodules_path.read_bytes()

# Deliberately checked in beside this auto-discovered guard. Any legitimate
# .gitmodules edit must update this scripts/ file in the same candidate; the
# V25 final publication classifier already treats scripts/ as release-relevant,
# so an older release SHA cannot silently publish across changed submodule
# acquisition metadata even when the external/ gitlink itself is unchanged.
EXPECTED_GITMODULES_SHA256 = "c6763e859259d63fc1c7df6ef0c726e7e5bc03af00fd5224a3004dec064ccd6c"


def validate(workflow_text: str, gitmodules_bytes: bytes, expected_digest: str) -> list[str]:
    errors: list[str] = []
    start = workflow_text.find("$finalReleaseRelevantPaths = @(")
    if start < 0:
        return ["V25 workflow missing final release-relevant protected-main path classifier"]
    end = workflow_text.find("\n            )", start)
    if end < 0:
        return ["V25 workflow final release-relevant path classifier is not bounded"]
    block = workflow_text[start:end]

    required = [
        "'.github/workflows/'",
        "'scripts/'",
        "'src/QS3D.BricsCAD.V25/'",
        "'src/QS3D.Core/'",
        "'external/'",
        "'Directory.Build.props'",
        "'Directory.Build.targets'",
        "'VERSION'",
        "'installer/'",
    ]
    for token in required:
        if token not in block:
            errors.append(f"V25 final-main release drift classifier missing release-relevant path: {token}")

    actual_digest = hashlib.sha256(gitmodules_bytes).hexdigest()
    if actual_digest != expected_digest:
        errors.append(
            ".gitmodules changed without refreshing the release-relevant scripts/ binding: "
            f"expected {expected_digest}, actual {actual_digest}"
        )

    required_flow = [
        'git merge-base --is-ancestor $env:GITHUB_SHA $finalMain',
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        "newer release-relevant protected main supersedes this commercial publication; refusing stale commercial publication",
        "$confirmedMainResponse = Invoke-RestMethod -Method Get",
        "if ($confirmedFinalMain -ne $finalMain)",
        "Protected main moved during final commercial release admission.",
    ]
    for token in required_flow:
        if token not in workflow_text:
            errors.append(f"V25 final-main freshness flow missing fail-closed contract: {token}")

    classifier = workflow_text.find("$finalReleaseRelevantPaths = @(")
    drift = workflow_text.find(
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        classifier + 1,
    )
    confirm = workflow_text.find("$confirmedMainResponse = Invoke-RestMethod -Method Get", drift + 1)
    publish = workflow_text.find("$publishPatchAttempted = $true", confirm + 1)
    if min(classifier, drift, confirm, publish) < 0 or not (classifier < drift < confirm < publish):
        errors.append(
            "V25 final publication order must remain classifier -> protected-main diff -> "
            "main confirmation -> publish attempt"
        )

    return errors


canonical_errors = validate(workflow, gitmodules, EXPECTED_GITMODULES_SHA256)
if canonical_errors:
    raise SystemExit("V25 release-relevant main-drift contract failed: " + "; ".join(canonical_errors))

mutated_gitmodules = gitmodules + b"# mutation: changed submodule acquisition metadata\n"
if not validate(workflow, mutated_gitmodules, EXPECTED_GITMODULES_SHA256):
    raise SystemExit("V25 .gitmodules binding mutation probe did not fail closed")

mutated_workflow = workflow.replace("              'scripts/',\n", "", 1)
if not validate(mutated_workflow, gitmodules, EXPECTED_GITMODULES_SHA256):
    raise SystemExit("V25 release-relevant scripts/ classifier mutation probe did not fail closed")

print(
    "PASS V25 final publication binds submodule metadata through a release-relevant scripts/ "
    "fingerprint and preserves final-main freshness gates"
)
