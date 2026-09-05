#!/usr/bin/env python3
import hashlib
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
publisher = publisher_path.read_text(encoding="utf-8")

# Read the exact Git blob, not checkout-materialized bytes. Windows runners can
# materialize tracked LF text as CRLF depending on Git checkout configuration;
# hashing the working tree would make this release gate platform-dependent even
# when HEAD is identical.
tracked_gitmodules = subprocess.run(
    ["git", "cat-file", "blob", "HEAD:.gitmodules"],
    cwd=root,
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,
    check=False,
)
if tracked_gitmodules.returncode != 0:
    raise SystemExit(
        "V26 .gitmodules release binding could not read exact tracked blob: "
        + tracked_gitmodules.stderr.decode("utf-8", errors="replace").strip()
    )
gitmodules = tracked_gitmodules.stdout

# This reviewed fingerprint deliberately lives in scripts/. The publisher's
# final protected-main drift classifier already treats scripts/ as release-
# relevant, so every legitimate .gitmodules metadata edit must update this
# guard in the same candidate and therefore cannot look non-release-only to a
# stale V26 publication.
EXPECTED_GITMODULES_SHA256 = "c6763e859259d63fc1c7df6ef0c726e7e5bc03af00fd5224a3004dec064ccd6c"


def validate(text: str, gitmodules_bytes: bytes, expected_digest: str) -> list[str]:
    errors: list[str] = []
    start = text.find("$finalReleaseRelevantPaths = @(")
    if start < 0:
        return ["V26 publisher missing final release-relevant protected-main path classifier"]
    end = text.find("\n  )", start)
    if end < 0:
        return ["V26 publisher final release-relevant path classifier is not bounded"]
    block = text[start:end]

    # #5890 only owns the metadata-to-release-relevance binding. Existing V26
    # publisher/preflight coverage owns ancestry, final-main confirmation and
    # publish ordering; duplicating those contracts here makes this focused
    # guard brittle without strengthening the binding under test.
    for token in ("'scripts/'", "'external/'"):
        if token not in block:
            errors.append(f"V26 final-main release drift classifier missing binding path: {token}")

    actual_digest = hashlib.sha256(gitmodules_bytes).hexdigest()
    if actual_digest != expected_digest:
        errors.append(
            ".gitmodules changed without refreshing the release-relevant scripts/ binding: "
            f"expected {expected_digest}, actual {actual_digest}"
        )

    return errors


canonical_errors = validate(publisher, gitmodules, EXPECTED_GITMODULES_SHA256)
if canonical_errors:
    raise SystemExit("V26 .gitmodules release binding failed: " + "; ".join(canonical_errors))

mutated_gitmodules = gitmodules + b"# mutation: changed submodule acquisition metadata\n"
if not validate(publisher, mutated_gitmodules, EXPECTED_GITMODULES_SHA256):
    raise SystemExit("V26 .gitmodules binding mutation probe did not fail closed")

mutated_publisher = publisher.replace("    'scripts/',\n", "", 1)
if mutated_publisher == publisher:
    raise SystemExit("V26 scripts/ classifier mutation probe could not mutate publisher fixture")
if not validate(mutated_publisher, gitmodules, EXPECTED_GITMODULES_SHA256):
    raise SystemExit("V26 release-relevant scripts/ classifier mutation probe did not fail closed")

commented_publisher = publisher.replace("    'scripts/',\n", "    # 'scripts/',\n", 1)
if commented_publisher == publisher:
    raise SystemExit("V26 commented scripts/ classifier mutation probe could not mutate publisher fixture")
if not validate(commented_publisher, gitmodules, EXPECTED_GITMODULES_SHA256):
    raise SystemExit("V26 commented scripts/ classifier mutation probe did not fail closed")

print("PASS V26 .gitmodules metadata is bound to the release-relevant scripts/ classifier")
