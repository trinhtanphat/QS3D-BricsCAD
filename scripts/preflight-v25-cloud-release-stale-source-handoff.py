#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RELEASE = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
VERSION_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "QS3D.BricsCAD.V25.csproj"

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


# A release whose exact SOURCE_SHA has been superseded by release-relevant main
# changes must publish nothing, but it must finish successfully so the canonical
# workflow_run dispatcher can re-evaluate protected main. This is a no-op, not an
# ownership transfer: preview ordinals are immutable once reserved.
stale_start = release.find("if ($preMutationReleaseDriftStatus -eq 1) {")
stale_end = release.find("if ($preMutationReleaseDriftStatus -ne 0) {", stale_start + 1)
if stale_start < 0 or stale_end < 0:
    errors.append("missing pre-mutation release-relevant drift branch")
    stale_block = ""
else:
    stale_block = release[stale_start:stale_end]

require(stale_block, "V25_RELEASE_SUPERSEDED source_sha=", "superseded release audit marker")
require(stale_block, "::notice title=V25 release source superseded::", "superseded release notice")
require(stale_block, "exit 0", "successful stale-release no-op exit")
if "throw " in stale_block:
    errors.append("stale release-relevant main drift still throws instead of completing as a successful no-op")
require_before(
    release,
    "V25_RELEASE_SUPERSEDED source_sha=",
    '$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"',
    "superseded release no-op before draft-release POST mutation",
)

# The successful release completion wakes only the canonical dispatcher. The
# dispatcher intentionally resolves workflow_run to current main, then keeps the
# existing immutable-ownership rule: an ordinal owned by another SHA is never
# rebound and protected-main ProductVersion must advance before dispatch resumes.
for token, label in (
    ('- "QS3D Cloud V25 Preview Build & Release"', "canonical release workflow_run trigger"),
    ("github.event.workflow_run.conclusion == 'success'", "successful workflow_run admission"),
    ('if [[ "${GITHUB_EVENT_NAME}" == "workflow_run" ]]; then', "workflow_run source selection"),
    ('source_sha="${current_main,,}"', "workflow_run current-main rebinding"),
    ("reservation_owner_source=", "immutable reservation-owner tracking"),
    ("dispatch_fence_owner_source=", "immutable dispatch-fence-owner tracking"),
    ("will not reassign or duplicate-dispatch that ordinal", "no-reassignment decision"),
    ("The protected main ProductVersion must advance before the next automatic preview dispatch.", "fresh ProductVersion requirement"),
):
    require(dispatch, token, label)

for forbidden in (
    "handoff_rebind",
    "V25 preview reservation handoff admitted",
    "# Rebind protected main immediately before the first durable side effect.",
):
    if forbidden in dispatch:
        errors.append(f"dispatcher contains forbidden preview-ordinal rebind support: {forbidden}")

# 10307 is a burned historical identity. The incident fix must commit a strictly
# newer canonical identity, and all three assembly/product version surfaces must
# remain bound to the same ordinal. Future bumps remain valid because this guard
# checks monotonicity rather than pinning one preview forever.
try:
    root = ET.parse(VERSION_PROJECT).getroot()
    values = {}
    for name in ("Version", "FileVersion", "InformationalVersion"):
        matches = [node.text.strip() for node in root.iter(name) if node.text and node.text.strip()]
        if len(matches) != 1:
            errors.append(f"V25 project must contain exactly one {name}; found {len(matches)}")
        else:
            values[name] = matches[0]
    version = values.get("Version", "")
    match = re.fullmatch(r"0\.1\.0-preview\.([1-9][0-9]*)", version)
    if not match:
        errors.append(f"V25 Version is not a canonical preview identity: {version!r}")
    else:
        ordinal = int(match.group(1))
        if ordinal <= 10307:
            errors.append(f"V25 preview ordinal must advance beyond burned 10307; found {ordinal}")
        if values.get("FileVersion") != f"0.1.0.{ordinal}":
            errors.append("V25 FileVersion is not bound to the committed preview ordinal")
        if values.get("InformationalVersion") != version:
            errors.append("V25 InformationalVersion is not bound to Version")
except (ET.ParseError, OSError) as exc:
    errors.append(f"could not parse V25 version project: {exc}")

if errors:
    print("ERROR: V25 cloud stale-source recovery preflight failed closed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: V25 stale release is a successful no-op, preview ownership is immutable, and fresh identity is required")
