#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"

source = PREPARE.read_text(encoding="utf-8")
errors: list[str] = []


def require(token: str, label: str) -> None:
    if token not in source:
        errors.append(f"missing {label}: {token}")


def forbid(token: str, label: str) -> None:
    if token in source:
        errors.append(f"forbidden {label}: {token}")


# A release-relevant main advance after dispatch is an expected supersession,
# not an exceptional preparation failure. Keep the dispatched release surface
# bounded and hand it to the publish-stage protected-main stale-source no-op.
forbid(
    'throw "main moved after dispatch with release-relevant changes.',
    "hard failure for expected release-relevant supersession",
)
require(
    "function Assert-ReleaseBaseIsSafe",
    "backward-compatible release-base safety interface",
)
require(
    "return -not (Test-ReleaseRelevantDrift -TargetSha $TargetSha)",
    "release-base safety classification delegates to fail-closed drift detection",
)
require(
    "if (-not (Assert-ReleaseBaseIsSafe -TargetSha $releaseBase)) {",
    "initial release-relevant supersession classification",
)
require(
    "$releaseBase = $dispatch",
    "bounded stale release source selection",
)
require(
    "publish-stage stale-source no-op",
    "explicit stale-source handoff audit message",
)
require(
    "if (-not (Assert-ReleaseBaseIsSafe -TargetSha $latestMain)) {",
    "final release-relevant supersession classification",
)
require(
    "Write-Output -InputObject $releaseBase",
    "successful stale handoff output",
)

# Ambiguous history is still a correctness failure: the dispatch must remain an
# ancestor of protected main before any stale-source handoff is allowed.
require(
    "git merge-base --is-ancestor $dispatch $TargetSha",
    "dispatch ancestry validation",
)
require(
    'throw "Dispatched source $dispatch is not an ancestor of current main $TargetSha. Refusing ambiguous release preparation."',
    "ambiguous-history fail-closed guard",
)

if errors:
    print("ERROR: V25 stale release preparation no-op preflight failed closed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: V25 release preparation hands expected release-relevant supersession to the publish-stage stale-source no-op while preserving ancestry fail-closed safety")
