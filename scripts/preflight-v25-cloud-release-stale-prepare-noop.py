#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
PREPARE = ROOT / "scripts/prepare-v25-cloud-release.ps1"
workflow = WORKFLOW.read_text(encoding="utf-8")
prepare = PREPARE.read_text(encoding="utf-8")
errors: list[str] = []

admission = workflow[workflow.find("  source-admission:\n"):workflow.find("  release:\n")]
for token, label in (
    ("permissions:\n      contents: read", "read-only admission permissions"),
    ("ref: main", "trusted protected-main checkout"),
    ("git merge-base --is-ancestor $sourceSha $currentMain", "SOURCE_SHA ancestry validation"),
    ('"proceed=true" | Out-File -FilePath $env:GITHUB_OUTPUT', "release proceed output"),
    ("V25 release source pinned", "advanced-main pinned-source notice"),
):
    if token not in admission:
        errors.append(f"missing {label}: {token}")
for token in ("proceed=false", "V25_RELEASE_SUPERSEDED", "successful no-op", "git diff --quiet"):
    if token in admission:
        errors.append(f"trusted admission must not suppress an ancestor-pinned release: {token}")

release = workflow[workflow.find("  release:\n"):]
for token in ("needs: source-admission", "needs.source-admission.outputs.proceed == 'true'", "ref: ${{ inputs.source_sha || github.sha }}"):
    if token not in release:
        errors.append(f"release job lost exact-source dependency: {token}")

for token in ("function Assert-ReleaseSourceReachable", "$releaseBase = $dispatch", "git merge-base --is-ancestor $dispatch $TargetSha"):
    if token not in prepare:
        errors.append(f"release preparation lost pinned-source contract: {token}")
for token in ("function Test-ReleaseRelevantDrift", "main moved after dispatch with release-relevant changes", "git checkout --detach $releaseBase"):
    if token in prepare:
        errors.append(f"release preparation still rejects/rebases an admitted source: {token}")

if errors:
    print("ERROR: V25 trusted pinned-source admission preflight failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)
print("PASS: trusted V25 admission and preparation keep exact ancestor provenance without green stale-source no-op or main rebasing")
