#!/usr/bin/env python3
"""Fail closed if V26 rollback can destructively delete a reusable release tag."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "rollback-v26-draft-release.ps1"

PRESERVE_MARKER = "Preserving exact V26 tag"
TAG_DELETED_FALSE = "TagDeleted = $false"
DRAFT_DELETE = "Invoke-RestMethod -Method Delete -Uri $releaseUri"
OWNERSHIP_CHECK = "Assert-NoReleaseOwnsTag"
EXACT_TAG_RESOLVE = "Resolve-ExactRemoteTagSha"
TAG_DELETE = "Invoke-RestMethod -Method Delete -Uri $tagRefUri"
TAG_DELETE_HELPER = "Assert-TagDeleteCommittedAfterError"
TAG_DELETE_URI = "$tagRefUri ="


def validate(text: str) -> list[str]:
    failures: list[str] = []
    for token in (
        OWNERSHIP_CHECK,
        EXACT_TAG_RESOLVE,
        DRAFT_DELETE,
        PRESERVE_MARKER,
        TAG_DELETED_FALSE,
    ):
        if token not in text:
            failures.append(f"rollback tag-preservation contract missing: {token}")
    for token in (TAG_DELETE, TAG_DELETE_HELPER, TAG_DELETE_URI):
        if token in text:
            failures.append(f"rollback retains destructive tag-delete path: {token}")
    if text.count(TAG_DELETED_FALSE) < 2:
        failures.append("both owned and non-owned rollback outcomes must report TagDeleted = $false")
    return failures


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)
    mutations = {
        "draft release ownership deletion": source.replace(DRAFT_DELETE, "# draft delete removed"),
        "release exhaustion": source.replace(OWNERSHIP_CHECK, "ReleaseOwnershipCheck_REMOVED"),
        "exact tag identity recheck": source.replace(EXACT_TAG_RESOLVE, "ExactTagResolve_REMOVED"),
        "preservation marker": source.replace(PRESERVE_MARKER, "deleted tag"),
        "non-destructive result": source.replace(TAG_DELETED_FALSE, "TagDeleted = $true"),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            failures.append(f"mutation fixture did not modify source for {label}")
        elif not validate(mutated):
            failures.append(f"mutation escaped rollback tag-preservation guard: {label}")
    if failures:
        print("V26 rollback tag-preservation preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1
    print("PASS: V26 rollback deletes only its owned draft release and preserves the exact reusable release tag.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
