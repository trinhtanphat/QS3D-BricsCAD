#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
TAG_RE = re.compile(r"^v0\.1\.0-preview\.([1-9][0-9]*)$")
errors = []


def parse_ordinal(text, source):
    if not re.fullmatch(r"[1-9][0-9]*", text):
        raise ValueError(source + " ordinal is non-canonical: " + text)
    if len(text) > 5:
        raise ValueError(source + " ordinal exceeds FileVersion width: " + text)
    value = int(text, 10)
    if value > 65535:
        raise ValueError(source + " ordinal exceeds FileVersion range: " + text)
    return value


def next_preview(tags):
    values = []
    for tag in tags:
        match = TAG_RE.fullmatch(tag)
        if not match:
            raise ValueError("published tag is non-canonical: " + tag)
        values.append(parse_ordinal(match.group(1), "published"))
    current = max(values, default=0)
    if current >= 65535:
        raise OverflowError("preview ordinal exhausted")
    return current + 1


def expect(label, actual, expected):
    if actual != expected:
        errors.append(f"{label}: expected {expected}, got {actual}")


expect("published sequence advances once", next_preview(["v0.1.0-preview.10018"]), 10019)
expect(
    "successful publication advances the following run",
    next_preview(["v0.1.0-preview.10018", "v0.1.0-preview.10019"]),
    10020,
)
expect(
    "failed unpublished attempts do not consume ordinals",
    next_preview(["v0.1.0-preview.10018"]),
    10019,
)
expect("empty series starts at one", next_preview([]), 1)

for bad in ("v0.1.0-preview.0", "v0.1.0-preview.01", "v0.1.0-preview.65536"):
    try:
        next_preview([bad])
        errors.append("non-canonical/out-of-range tag was accepted: " + bad)
    except ValueError:
        pass

try:
    next_preview(["v0.1.0-preview.65535"])
    errors.append("preview exhaustion at 65535 was not rejected")
except OverflowError:
    pass

if not WORKFLOW.is_file():
    errors.append("missing V25 post-main dispatcher workflow")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "contents: read",
        "actions: write",
        "cancel-in-progress: true",
        "max_wait_checks=240",
        'actions/workflows/release-v25-cloud.yml/runs?per_page=100',
        'select(.status != "completed")',
        "if (( active_runs == 0 )); then",
        "sleep 15",
        "git fetch --force --tags origin",
        'series_prefix="v0.1.0-preview."',
        'git tag --list "${series_prefix}*"',
        "preview=$((max_preview + 1))",
        'gh workflow run release-v25-cloud.yml',
        '-f source_sha="${source_sha}"',
        '-f release_tag="${tag}"',
    )
    for token in required:
        if token not in text:
            errors.append("dispatcher published-sequence contract missing token: " + token)

    for forbidden in (
        "issues: write",
        "QS3D_V25_PREVIEW_RESERVATION",
        "reservation_issue=",
        "reservation_prefix=",
        "gh api --method POST",
        '-f body="${reservation}"',
    ):
        if forbidden in text:
            errors.append("dispatcher must not consume preview ordinals through the historical ledger: " + forbidden)

    if "contents: write" in text:
        errors.append("dispatcher must keep main contents read-only")

    wait_index = text.find('actions/workflows/release-v25-cloud.yml/runs?per_page=100')
    no_active_index = text.find("if (( active_runs == 0 )); then", wait_index)
    refresh_index = text.find("git fetch --force --tags origin", no_active_index)
    tag_scan_index = text.find('git tag --list "${series_prefix}*"', refresh_index)
    dispatch_index = text.find("gh workflow run release-v25-cloud.yml", tag_scan_index)
    indexes = (wait_index, no_active_index, refresh_index, tag_scan_index, dispatch_index)
    if min(indexes) < 0 or not (
        wait_index < no_active_index < refresh_index < tag_scan_index < dispatch_index
    ):
        errors.append(
            "dispatcher must wait for prior V25 children, then refresh published tags, derive the ordinal, and only then dispatch"
        )

print("QS3D V25 preview published-sequence preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: automatic V25 preview allocation waits for prior child runs and derives only from published canonical tags, "
    "so failed/cancelled attempts retry the same next publishable ordinal without weakening sequence validation."
)
