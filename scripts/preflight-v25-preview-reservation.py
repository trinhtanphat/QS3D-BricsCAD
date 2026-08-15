#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
RESERVATION_PREFIX = "QS3D_V25_PREVIEW_RESERVATION"
RESERVATION_ISSUE = 1441
TAG_RE = re.compile(r"^v0\.1\.0-preview\.([1-9][0-9]*)$")
RESERVATION_RE = re.compile(
    rf"^{RESERVATION_PREFIX} ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{{40}}) run_id=([1-9][0-9]*)$"
)
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


def next_preview(tags, reservation_comments):
    values = []
    for tag in tags:
        match = TAG_RE.fullmatch(tag)
        if not match:
            raise ValueError("published tag is non-canonical: " + tag)
        values.append(parse_ordinal(match.group(1), "published"))
    for body in reservation_comments:
        match = RESERVATION_RE.fullmatch(body)
        if not match:
            continue
        values.append(parse_ordinal(match.group(1), "reserved"))
    current = max(values, default=0)
    if current >= 65535:
        raise OverflowError("preview ordinal exhausted")
    return current + 1


def expect(label, actual, expected):
    if actual != expected:
        errors.append(f"{label}: expected {expected}, got {actual}")


expect("published-only sequence", next_preview(["v0.1.0-preview.10015"], []), 10016)
expect(
    "in-flight reservation advances sequence",
    next_preview(
        ["v0.1.0-preview.10015"],
        [RESERVATION_PREFIX + " ordinal=10016 source_sha=" + "a" * 40 + " run_id=31856129239"],
    ),
    10017,
)
expect(
    "failed reservation remains consumed",
    next_preview(
        ["v0.1.0-preview.10014"],
        [
            RESERVATION_PREFIX + " ordinal=10015 source_sha=" + "b" * 40 + " run_id=1",
            RESERVATION_PREFIX + " ordinal=10016 source_sha=" + "c" * 40 + " run_id=2",
        ],
    ),
    10017,
)
expect(
    "unrelated issue comments are ignored",
    next_preview(["v0.1.0-preview.9"], ["human note", "ordinal=50000"]),
    10,
)

for bad in ("v0.1.0-preview.0", "v0.1.0-preview.01", "v0.1.0-preview.65536"):
    try:
        next_preview([bad], [])
        errors.append("non-canonical/out-of-range tag was accepted: " + bad)
    except ValueError:
        pass

try:
    next_preview(["v0.1.0-preview.65535"], [])
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
        "issues: write",
        f"reservation_issue={RESERVATION_ISSUE}",
        f'reservation_prefix="{RESERVATION_PREFIX}"',
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
        'reservation="${reservation_prefix} ordinal=${preview} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
        'gh api --method POST',
        '"repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
        '-f body="${reservation}"',
        'gh workflow run release-v25-cloud.yml',
        '-f source_sha="${source_sha}"',
        '-f release_tag="${tag}"',
        "cancel-in-progress: true",
    )
    for token in required:
        if token not in text:
            errors.append("dispatcher reservation contract missing token: " + token)

    if "contents: write" in text:
        errors.append("dispatcher must not gain contents: write for preview reservation")

    reserve_index = text.find('-f body="${reservation}"')
    dispatch_index = text.find("gh workflow run release-v25-cloud.yml")
    if reserve_index < 0 or dispatch_index < 0 or reserve_index >= dispatch_index:
        errors.append("preview ordinal must be durably reserved before downstream release dispatch")

print("QS3D V25 preview reservation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: automatic V25 preview allocation advances across both published tags and durable in-flight reservations, "
    "records the reservation before dispatch, and keeps main contents read-only."
)
