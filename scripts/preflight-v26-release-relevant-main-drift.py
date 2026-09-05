#!/usr/bin/env python3
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
gitmodules_path = root / ".gitmodules"
binding_path = root / "scripts" / "v26-release-gitmodules.sha256"
publisher = publisher_path.read_text(encoding="utf-8")
gitmodules = gitmodules_path.read_bytes()
binding = binding_path.read_text(encoding="ascii").strip() if binding_path.exists() else ""


def validate(text: str, gitmodules_bytes: bytes, binding_text: str) -> list[str]:
    errors: list[str] = []
    start = text.find("$finalReleaseRelevantPaths = @(")
    if start < 0:
        return ["V26 publisher missing final release-relevant protected-main path classifier"]
    end = text.find("\n  )", start)
    if end < 0:
        return ["V26 publisher final release-relevant path classifier is not bounded"]
    block = text[start:end]

    required_paths = [
        "'.github/workflows/'",
        "'scripts/'",
        "'src/QS3D.BricsCAD.V25/'",
        "'src/QS3D.BricsCAD.V26/'",
        "'src/QS3D.Core/'",
        "'Directory.Build.props'",
        "'Directory.Build.targets'",
        "'VERSION'",
        "'installer/'",
        "'external/'",
    ]
    for token in required_paths:
        if token not in block:
            errors.append(f"V26 final-main release drift classifier missing release-relevant path: {token}")

    expected_binding = f"{hashlib.sha256(gitmodules_bytes).hexdigest()}  .gitmodules"
    if binding_text != expected_binding:
        errors.append(
            "V26 .gitmodules metadata is not bound through the release-relevant scripts/ classifier: "
            f"expected '{expected_binding}', found '{binding_text or '<missing>'}'"
        )

    required_flow = [
        'git merge-base --is-ancestor $env:GITHUB_SHA $finalMain',
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        "newer release-relevant protected main supersedes this V26 publication; refusing stale V26 publication",
        "$confirmedMainResponse = Invoke-RestMethod -Method Get",
        "if ($confirmedFinalMain -ne $finalMain)",
        "Protected main moved during final V26 release admission.",
        "$publishPatchAttempted = $true",
    ]
    for token in required_flow:
        if token not in text:
            errors.append(f"V26 final-main freshness flow missing fail-closed contract: {token}")

    classifier = text.find("$finalReleaseRelevantPaths = @(")
    drift = text.find(
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        classifier + 1,
    )
    confirm = text.find("$confirmedMainResponse = Invoke-RestMethod -Method Get", drift + 1)
    publish = text.find("$publishPatchAttempted = $true", confirm + 1)
    if min(classifier, drift, confirm, publish) < 0 or not (classifier < drift < confirm < publish):
        errors.append(
            "V26 final publication order must remain classifier -> protected-main diff -> "
            "main confirmation -> publish attempt"
        )

    return errors


canonical_errors = validate(publisher, gitmodules, binding)
if canonical_errors:
    raise SystemExit("V26 release-relevant main-drift contract failed: " + "; ".join(canonical_errors))

mutated_gitmodules = gitmodules + b"# mutation: changed submodule acquisition metadata\n"
if not validate(publisher, mutated_gitmodules, binding):
    raise SystemExit("V26 .gitmodules binding mutation probe did not fail closed")

mutated_publisher = publisher.replace("    'scripts/',\n", "", 1)
if not validate(mutated_publisher, gitmodules, binding):
    raise SystemExit("V26 release-relevant scripts/ classifier mutation probe did not fail closed")

print(
    "PASS V26 final publication binds submodule metadata through a release-relevant scripts/ checksum "
    "and preserves final-main freshness gates"
)
