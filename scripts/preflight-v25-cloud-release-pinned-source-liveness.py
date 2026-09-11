#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"

workflow = WORKFLOW.read_text(encoding="utf-8")
prepare = PREPARE.read_text(encoding="utf-8")
errors: list[str] = []

publish_marker = "      - name: Publish GitHub prerelease"
publish_pos = workflow.find(publish_marker)
publish = workflow[publish_pos:] if publish_pos >= 0 else ""

if publish_pos < 0:
    errors.append("missing GitHub prerelease publication step")

for token in (
    "git merge-base --is-ancestor $env:SOURCE_SHA $preMutationMain",
    "git merge-base --is-ancestor $env:SOURCE_SHA $finalMain",
    "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
):
    if token not in publish:
        errors.append(f"missing pinned-source publication invariant: {token}")

for forbidden in (
    "successful no-op before the first persistent release mutation",
    "V25_RELEASE_SUPERSEDED source_sha=$env:SOURCE_SHA",
):
    if forbidden in publish:
        errors.append(f"publish step may not silently succeed without a release: {forbidden}")

if "main moved after dispatch with release-relevant changes" in prepare:
    errors.append("release preparation still rejects an admitted exact source after protected main advances")
if "$releaseBase = $dispatch" not in prepare:
    errors.append("release preparation must remain pinned to the admitted dispatch SHA")
if "git checkout --detach $releaseBase" in prepare:
    errors.append("release preparation must not rebase the verified release workspace onto a newer main")

source_admission = workflow[workflow.find("  source-admission:\n"):workflow.find("  release:\n")]
if '"proceed=false" | Out-File' in source_admission:
    errors.append("release workflow admission may not report success while suppressing the release job")
if "successful no-op before stale source scripts/build/package execute" in source_admission:
    errors.append("release workflow admission still contains a green-without-release path")

if errors:
    print("ERROR: V25 pinned-source release liveness contract failed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: admitted V25 cloud releases remain pinned to exact source provenance and cannot finish green without publication merely because main advanced")
