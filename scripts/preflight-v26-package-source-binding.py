#!/usr/bin/env python3
"""Fail closed if V26 package provenance can drift from the workflow source SHA."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v26.yml"
VALIDATOR = ROOT / "scripts/assert-v26-release-package-identity.ps1"


def validate(workflow: str, validator: str) -> list[str]:
    errors: list[str] = []

    validator_tokens = (
        "[string]$ExpectedSourceCommit",
        "ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40}$'",
        "([string]$metadata.gitCommit).Trim()",
        "metadata gitCommit must be one exact 40-hex Git commit SHA",
        "$metadataSourceCommit.ToLowerInvariant()",
        "$expectedSourceCommitNormalized",
        "does not match expected workflow SHA",
        "SourceCommit = $metadataSourceCommit.ToLowerInvariant()",
    )
    for token in validator_tokens:
        if token not in validator:
            errors.append(f"V26 package identity validator missing source-binding token: {token}")

    workflow_tokens = (
        "assert-v26-release-package-identity.ps1",
        "-ReleaseTag $env:RELEASE_TAG `\n            -ExpectedSourceCommit $env:GITHUB_SHA | Out-Null",
    )
    for token in workflow_tokens:
        if token not in workflow:
            errors.append(f"V26 release workflow missing exact source-binding token: {token}")

    if "-ExpectedSourceCommit ${{" in workflow:
        errors.append("V26 release workflow must pass the runner's exact GITHUB_SHA environment value, not an interpolated mutable string surface")

    return errors


workflow_text = WORKFLOW.read_text(encoding="utf-8")
validator_text = VALIDATOR.read_text(encoding="utf-8")
errors = validate(workflow_text, validator_text)
if errors:
    raise SystemExit("V26 package source-binding preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "workflow omits exact SHA argument": (
        workflow_text.replace("            -ExpectedSourceCommit $env:GITHUB_SHA | Out-Null", "            | Out-Null", 1),
        validator_text,
    ),
    "validator ignores metadata source": (
        workflow_text,
        validator_text.replace("([string]$metadata.gitCommit).Trim()", "([string]$ExpectedSourceCommit).Trim()", 1),
    ),
    "validator loses SHA shape admission": (
        workflow_text,
        validator_text.replace("ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40}$'", "ExpectedSourceCommit -notmatch '.+'", 1),
    ),
    "validator loses mismatch refusal": (
        workflow_text,
        validator_text.replace("does not match expected workflow SHA", "does not match source", 1),
    ),
}
for label, (mutated_workflow, mutated_validator) in mutations.items():
    if not validate(mutated_workflow, mutated_validator):
        raise SystemExit(f"V26 package source-binding preflight mutation escaped detection: {label}")

print("PASS V26 package metadata workflow SHA binding")
