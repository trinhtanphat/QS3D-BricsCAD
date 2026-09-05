#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow_path = root / ".github" / "workflows" / "release-v25.yml"
workflow = workflow_path.read_text(encoding="utf-8")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    start = text.find("$finalReleaseRelevantPaths = @(")
    if start < 0:
        return ["V25 workflow missing final release-relevant protected-main path classifier"]
    end = text.find("\n            )", start)
    if end < 0:
        return ["V25 workflow final release-relevant path classifier is not bounded"]
    block = text[start:end]

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
        "'.gitmodules'",
    ]
    for token in required:
        if token not in block:
            errors.append(f"V25 final-main release drift classifier missing release-relevant path: {token}")

    required_flow = [
        'git merge-base --is-ancestor $env:GITHUB_SHA $finalMain',
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        "newer release-relevant protected main supersedes this commercial publication; refusing stale commercial publication",
        "$confirmedMainResponse = Invoke-RestMethod -Method Get",
        "if ($confirmedFinalMain -ne $finalMain)",
        "Protected main moved during final commercial release admission.",
    ]
    for token in required_flow:
        if token not in text:
            errors.append(f"V25 final-main freshness flow missing fail-closed contract: {token}")

    classifier = text.find("$finalReleaseRelevantPaths = @(")
    drift = text.find('git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths', classifier + 1)
    confirm = text.find("$confirmedMainResponse = Invoke-RestMethod -Method Get", drift + 1)
    publish = text.find("$publishPatchAttempted = $true", confirm + 1)
    if min(classifier, drift, confirm, publish) < 0 or not (classifier < drift < confirm < publish):
        errors.append("V25 final publication order must remain classifier -> protected-main diff -> main confirmation -> publish attempt")

    return errors


canonical_errors = validate(workflow)
if canonical_errors:
    raise SystemExit("V25 release-relevant main-drift contract failed: " + "; ".join(canonical_errors))

mutated = workflow.replace("              '.gitmodules',\n", "", 1)
if not validate(mutated):
    raise SystemExit("V25 release-relevant main-drift mutation probe did not fail closed when .gitmodules classification was removed")

print("PASS V25 final publication treats submodule metadata as release-relevant protected-main drift")
