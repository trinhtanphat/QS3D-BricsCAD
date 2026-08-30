#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper_path = root / "scripts" / "rollback-v26-draft-release.ps1"
workflow_path = root / ".github" / "workflows" / "release-v26.yml"

helper = helper_path.read_text(encoding="utf-8")
workflow = workflow_path.read_text(encoding="utf-8")

required_helper = [
    "if ($release.draft -ne $true)",
    "Release $ReleaseId is not a draft; refusing destructive rollback.",
    "release.tag_name",
    "Resolve-ExactRemoteTagSha",
    "git ls-remote --tags origin $tagRef $peeledRef",
    "Remote tag $ReleaseTag moved to",
    "Invoke-RestMethod -Method Delete -Uri $releaseUri",
    "releases/tags/",
    "A release still owns tag $ReleaseTag after draft deletion; refusing tag deletion.",
    "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
    "git/refs/tags/",
    "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
]
for needle in required_helper:
    if needle not in helper:
        raise SystemExit(f"V26 draft rollback helper lost fail-closed contract: {needle}")

for forbidden in [
    "git push --delete",
    "git push origin :refs/tags/",
    "-Force",
]:
    if forbidden in helper:
        raise SystemExit(f"V26 draft rollback helper must not use broad/destructive shortcut: {forbidden}")

# The helper is intentionally useless until the publication transaction wires it.
# Keep this fail-closed so landing the helper without rollback integration cannot pass.
required_workflow = [
    "rollback-v26-draft-release.ps1",
    "Automatic V26 draft rollback failed",
    "V26 publication failed after draft creation",
]
for needle in required_workflow:
    if needle not in workflow:
        raise SystemExit(f"V26 publication workflow is not restart-safe yet: missing {needle}")

print("PASS V26 draft release rollback contract")
