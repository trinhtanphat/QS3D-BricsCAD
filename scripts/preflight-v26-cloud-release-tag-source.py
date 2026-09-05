#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"


def fail(message: str) -> None:
    print(f"ERROR: V26 cloud release tag source preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


if not WORKFLOW.is_file():
    fail("missing .github/workflows/release-v26-cloud.yml")

workflow = WORKFLOW.read_text(encoding="utf-8")

required = (
    "REQUESTED_RELEASE_TAG: ${{ inputs.release_tag }}",
    "id: release-identity",
    "release_tag: ${{ steps.release-identity.outputs.release_tag }}",
    "$requestedReleaseTag = ([string]$env:REQUESTED_RELEASE_TAG).Trim()",
    "$env:RELEASE_TAG = $expectedCloudTag",
    '"release_tag=$expectedCloudTag" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append',
    '"RELEASE_TAG=$expectedCloudTag" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append',
    "Ignoring requested V26 release_tag",
    "RELEASE_TAG: ${{ needs.qualify.outputs.release_tag }}",
    "name: QS3D-BricsCAD-V26-cloud-${{ steps.release-identity.outputs.release_tag }}-${{ github.sha }}",
    "name: QS3D-BricsCAD-V26-cloud-${{ needs.qualify.outputs.release_tag }}-${{ github.sha }}",
)
for token in required:
    if token not in workflow:
        fail(f"workflow is missing source-owned tag contract token: {token}")

direct_input_binding = "RELEASE_TAG: ${{ inputs.release_tag }}"
if direct_input_binding in {line.strip() for line in workflow.splitlines()}:
    fail("workflow must not bind publication RELEASE_TAG directly to workflow_dispatch input")

core_match = "[string]::Equals($v26Versions[0], $coreVersions[0], [StringComparison]::Ordinal)"
if core_match not in workflow:
    fail("workflow must preserve fail-closed Core/V26 source-version equality")

print("V26 cloud release tag source preflight passed.")
