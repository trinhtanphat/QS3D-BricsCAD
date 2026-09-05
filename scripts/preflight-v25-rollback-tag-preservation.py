#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "rollback-v25-draft-release.ps1"
text = TARGET.read_text(encoding="utf-8")

required = {
    "draft identity gate": "if ([long]$release.id -ne $ReleaseId)",
    "draft repository identity gate": "release.url, $releaseUri",
    "draft-only gate": "$release.draft -ne $true",
    "draft tag gate": "release.tag_name, $ReleaseTag",
    "draft delete": "Invoke-RestMethod -Method Delete -Uri $releaseUri",
    "owner exhaustion": "Assert-NoReleaseOwnsTag",
    "post-cleanup tag resolution": "$resolvedPreserved = Resolve-ExactRemoteTagSha",
    "post-cleanup exact-SHA gate": "V25 release tag $ReleaseTag changed during draft rollback",
    "preservation marker": "Preserving exact V25 tag $ReleaseTag",
    "non-destructive result": "TagDeleted = $false",
}
missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("V25 rollback tag-preservation guard failed; missing: " + ", ".join(missing))

forbidden = {
    "tag DELETE URI": "/git/refs/tags/",
    "tag reconciliation URI": "/git/ref/tags/",
    "tag delete reconciliation helper": "Assert-TagDeleteCommittedAfterError",
    "successful destructive result": "TagDeleted = $true",
}
present = [name for name, token in forbidden.items() if token in text]
if present:
    raise SystemExit("V25 rollback tag-preservation guard failed; destructive surface remains: " + ", ".join(present))

# Mutation controls: each invariant must be independently detectable by this guard.
def rejects(mutated: str) -> bool:
    for token in required.values():
        if token not in mutated:
            return True
    for token in forbidden.values():
        if token in mutated:
            return True
    return False

for name, token in required.items():
    mutated = text.replace(token, "__REMOVED__", 1)
    if not rejects(mutated):
        raise SystemExit(f"mutation control did not detect removed invariant: {name}")

for name, token in forbidden.items():
    mutated = text + "\n# injected mutation\n" + token + "\n"
    if not rejects(mutated):
        raise SystemExit(f"mutation control did not detect destructive invariant: {name}")

print("PASS V25 rollback preserves exact reusable tag after bounded draft cleanup")
