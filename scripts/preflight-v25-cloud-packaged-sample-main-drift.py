#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"

workflow = WORKFLOW.read_text(encoding="utf-8")
package = PACKAGE.read_text(encoding="utf-8")
errors = []

for token, label in (
    ("$sampleSource = Join-Path $root 'samples/generated'", "synthetic sample root"),
    ("Join-Path $sampleSource $sampleName", "sample consumption from bound root"),
):
    if token not in package:
        errors.append(f"missing package {label}: {token}")

for token, label in (
    ("ref: ${{ inputs.source_sha || github.sha }}", "exact source checkout"),
    ("Package source HEAD must equal RELEASE_COMMIT_SHA", "package source identity gate"),
    ("PACKAGE-METADATA gitCommit must match exact release source commit", "package metadata source gate"),
    ("target_commitish = $env:RELEASE_COMMIT_SHA", "release target pin"),
    ("Publish source HEAD must equal RELEASE_COMMIT_SHA", "publish source identity gate"),
):
    if token not in workflow:
        errors.append(f"missing workflow {label}: {token}")

for forbidden in (
    "successful no-op before the first persistent release mutation",
    "V25_RELEASE_SUPERSEDED source_sha=$env:SOURCE_SHA",
):
    if forbidden in workflow:
        errors.append(f"release workflow can still finish green without publishing: {forbidden}")

if errors:
    raise SystemExit("V25 cloud packaged-sample source pin failed: " + "; ".join(errors))
print("PASS V25 cloud package keeps samples and release metadata pinned to the admitted exact source SHA across concurrent main advancement")
