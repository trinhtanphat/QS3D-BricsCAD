#!/usr/bin/env python3
"""Guard V25 preview publication against source substitution while allowing protected main to advance."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / ".github/workflows/release-v25-cloud.yml").read_text(encoding="utf-8")
failures: list[str] = []
publish_step = source.find("      - name: Publish GitHub prerelease")
publish = source[publish_step:] if publish_step >= 0 else ""
if publish_step < 0:
    failures.append("could not locate V25 publication step")

required = (
    "$finalMainResponse = Invoke-RestMethod -Method Get",
    "$finalMainRef = 'refs/remotes/origin/qs3d-release-final-main'",
    "$fetchedFinalMain -ne $finalMain",
    "git merge-base --is-ancestor $env:SOURCE_SHA $finalMain",
    "$publishMainResponse = Invoke-RestMethod -Method Get",
    "if ($publishMain -ne $finalMain)",
    "git merge-base --is-ancestor $env:SOURCE_SHA $publishMain",
    "$publishBody = @{ draft = $false }",
    "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
)
for token in required:
    if token not in publish:
        failures.append(f"final V25 publication admission is incomplete; missing: {token}")

for token in ("$finalReleaseRelevantPaths", "$finalReleaseDriftStatus", "refusing stale V25 cloud publication", "newer release-relevant main integration supersedes this publication"):
    if token in publish:
        failures.append(f"final V25 publication still contains race-starvation policy token: {token}")

asset_identity = publish.find("$assetIdentityDrift = @(")
final_api = publish.find("$finalMainResponse = Invoke-RestMethod -Method Get")
ancestry = publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $finalMain")
second_main = publish.find("$publishMainResponse = Invoke-RestMethod -Method Get")
moved_ancestry = publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $publishMain")
publish_body = publish.find("$publishBody = @{ draft = $false }")
release_patch = publish.find("$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri")
ordered = [asset_identity, final_api, ancestry, second_main, moved_ancestry, publish_body, release_patch]
if min(ordered) < 0 or ordered != sorted(ordered):
    failures.append("final preview publication must order verified assets -> current-main ancestry -> moved-main reconfirmation -> publish PATCH")

if final_api >= 0 and release_patch >= 0 and "exit 0" in publish[final_api:release_patch]:
    failures.append("verified V25 preview publication must not exit success before publishing")
if "continue-on-error" in source:
    failures.append("release source admission must not become fail-open through continue-on-error")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}", file=sys.stderr)
    raise SystemExit(1)
print("PASS: V25 preview publication keeps verified SOURCE_SHA provenance across concurrent protected-main advancement")
