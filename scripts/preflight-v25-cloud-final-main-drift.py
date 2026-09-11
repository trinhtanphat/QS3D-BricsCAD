#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow = (root / ".github/workflows/release-v25-cloud.yml").read_text(encoding="utf-8")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    publish = text[text.find("      - name: Publish GitHub prerelease"):]
    patch = publish.find("$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri")
    anchors = [
        publish.find('$finalMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main"'),
        publish.find("$finalMainRef = 'refs/remotes/origin/qs3d-release-final-main'"),
        publish.find("$fetchedFinalMain -ne $finalMain"),
        publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $finalMain"),
        publish.find('$publishMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main"'),
        publish.find("if ($publishMain -ne $finalMain)"),
        publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $publishMain"),
        patch,
    ]
    if min(anchors) < 0:
        errors.append("final exact-source ancestry/reconfirmation flow is incomplete")
    elif anchors != sorted(anchors):
        errors.append("final publication order must remain main API/fetch -> ancestry -> reconfirmation -> moved-main ancestry -> publish PATCH")
    final_start = anchors[0] if anchors[0] >= 0 else 0
    final_window = publish[final_start:patch] if patch >= 0 else publish[final_start:]
    for forbidden in ("$finalReleaseRelevantPaths", "$finalReleaseDriftStatus", "refusing stale V25 cloud publication", "exit 0"):
        if forbidden in final_window:
            errors.append(f"verified exact-source publication still has race-starvation logic: {forbidden}")
    return errors

errors = validate(workflow)
if errors:
    raise SystemExit("V25 cloud final pinned-source admission failed: " + "; ".join(errors))
mutated = workflow.replace("git merge-base --is-ancestor $env:SOURCE_SHA $publishMain", "git merge-base --is-ancestor $env:SOURCE_SHA $finalMain", 1)
if not validate(mutated):
    raise SystemExit("V25 final moved-main ancestry mutation probe did not fail closed")
print("PASS V25 cloud publication remains pinned to verified SOURCE_SHA while reconfirming ancestry across concurrent main advancement")
