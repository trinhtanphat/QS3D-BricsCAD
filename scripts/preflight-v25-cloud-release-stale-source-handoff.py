#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RELEASE = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"

release = RELEASE.read_text(encoding="utf-8")
dispatch = DISPATCH.read_text(encoding="utf-8")
errors = []


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        errors.append(f"missing {label}: {token}")


def require_before(source: str, first: str, second: str, label: str) -> None:
    first_pos = source.find(first)
    second_pos = source.find(second)
    if first_pos < 0 or second_pos < 0 or first_pos >= second_pos:
        errors.append(f"invalid ordering for {label}: {first!r} must precede {second!r}")


# A stale source must stop before the first release mutation without making the
# whole workflow RED. Its successful completion is the authenticated workflow_run
# signal that lets the canonical dispatcher re-evaluate protected main.
require(release, "V25_RELEASE_SUPERSEDED source_sha=", "superseded release audit marker")
require(release, "::notice title=V25 release source superseded::", "superseded release notice")
require_before(
    release,
    "V25_RELEASE_SUPERSEDED source_sha=",
    "$body = @{",
    "superseded release exit before draft-release mutation",
)
if (
    'throw "Protected main contains release-relevant changes after SOURCE_SHA; refusing to create a stale V25 draft release.'
    in release
):
    errors.append("stale release-relevant main drift still throws instead of handing off cleanly")

# Dispatcher handoff is fail-closed: only the successful canonical upstream
# release run may move an existing preview ordinal from its previous owner to a
# descendant protected-main SHA. Ownership history remains append-only; the most
# recent complete reservation/fence pair becomes the effective owner.
for token, label in (
    ("UPSTREAM_RELEASE_RUN_ID:", "upstream release run id binding"),
    ("UPSTREAM_RELEASE_HEAD_SHA:", "upstream release source binding"),
    ("latest_reservation_source=", "latest reservation owner tracking"),
    ("latest_dispatch_fence_source=", "latest dispatch-fence owner tracking"),
    ("handoff_rebind=0", "explicit handoff default-deny state"),
    ("actions/runs/${UPSTREAM_RELEASE_RUN_ID}", "upstream release API provenance check"),
    (".github/workflows/release-v25-cloud.yml", "canonical upstream release workflow path check"),
    ("V25 preview reservation handoff admitted", "auditable handoff admission"),
):
    require(dispatch, token, label)

require_before(
    dispatch,
    "V25 preview reservation handoff admitted",
    "# Rebind protected main immediately before the first durable side effect.",
    "handoff validation before durable reservation/dispatch mutation",
)

if "reservation_owner_conflict=0" in dispatch or "dispatch_fence_owner_conflict=0" in dispatch:
    errors.append("legacy any-prior-owner conflict state remains; dispatcher cannot express append-only handoff ownership")

if errors:
    print("ERROR: V25 cloud stale-source handoff preflight failed closed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: V25 cloud stale-source handoff remains fail-closed, append-only, and self-reconciling")
