#!/usr/bin/env python3
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
PLATFORM = ROOT / "external" / "QS3D-Platform"
SCHEDULE = PLATFORM / "src" / "QS3D.Platform.Quantity" / "QuantitySchedule.cs"
BOQ = PLATFORM / "src" / "QS3D.Platform.Quantity" / "BoqProjection.cs"
SCHEDULE_SMOKE = PLATFORM / "tests" / "QS3D.Platform.SmokeTests" / "QuantityScheduleKnownCountNoOverreadModuleSmoke.cs"
BOQ_SMOKE = PLATFORM / "tests" / "QS3D.Platform.SmokeTests" / "BoqKnownCountNoOverreadModuleSmoke.cs"

errors = []


def pinned_sha() -> str:
    result = subprocess.run(
        ["git", "ls-tree", "HEAD", "external/QS3D-Platform"],
        cwd=ROOT,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        errors.append("cannot read QS3D-Platform gitlink from HEAD: " + result.stderr.strip())
        return ""
    fields = result.stdout.strip().split()
    if len(fields) < 3 or fields[1] != "commit":
        errors.append("external/QS3D-Platform is not a gitlink in HEAD")
        return ""
    return fields[2]


def ensure_pinned_platform_checkout(sha: str) -> None:
    if not sha:
        return
    required = (SCHEDULE, BOQ, SCHEDULE_SMOKE, BOQ_SMOKE)
    if all(path.is_file() for path in required):
        return

    # Shared CI intentionally performs a non-recursive checkout. This guard needs
    # the exact gitlink tree it is validating, so initialize only this pinned
    # submodule path. Git verifies the resulting submodule HEAD against the
    # superproject gitlink; any fetch/checkout mismatch fails closed here.
    result = subprocess.run(
        ["git", "submodule", "update", "--init", "--depth", "1", "--", "external/QS3D-Platform"],
        cwd=ROOT,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        errors.append("cannot initialize pinned QS3D-Platform gitlink for hardening validation: " + result.stderr.strip())
        return

    head = subprocess.run(
        ["git", "-C", str(PLATFORM), "rev-parse", "HEAD"],
        cwd=ROOT,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    actual = head.stdout.strip().lower() if head.returncode == 0 else ""
    if actual != sha.lower():
        errors.append(f"initialized QS3D-Platform HEAD does not match superproject gitlink: expected {sha}, got {actual or '<unresolved>'}")


def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required pinned Platform hardening file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require_known_count_shape(label: str, source: str) -> None:
    if not source:
        return
    marker = "private static List<T> MaterializeCaptured<T>"
    start = source.find(marker)
    if start < 0:
        errors.append(f"{label} lacks dedicated captured-count materialization")
        return

    body = source[start:]
    tokens = (
        "if (!advertisedCount.HasValue)",
        "foreach (var item in source)",
        "for (var index = 0; index < advertisedCount.Value; index++)",
        "if (!enumerator.MoveNext())",
        "result.Add(enumerator.Current);",
        "if (enumerator.MoveNext())",
    )
    positions = []
    for token in tokens:
        position = body.find(token)
        if position < 0:
            errors.append(f"{label} captured-count materializer is missing {token!r}")
            return
        positions.append(position)

    # Unknown-count input may remain a bounded foreach. The known-count branch
    # must instead consume exactly N MoveNext+Current pairs and then make only
    # one surplus MoveNext probe, with no surplus Current read.
    if positions != sorted(positions):
        errors.append(f"{label} captured-count traversal order does not preserve the no-overread contract")


sha = pinned_sha()
ensure_pinned_platform_checkout(sha)
schedule = read(SCHEDULE)
boq = read(BOQ)
schedule_smoke = read(SCHEDULE_SMOKE)
boq_smoke = read(BOQ_SMOKE)

# Require executable hostile-enumerator regressions in the pinned tree rather
# than trusting a parent-repository version literal.
if schedule_smoke and "CurrentReads" not in schedule_smoke:
    errors.append("schedule no-overread smoke does not observe Current reads")
if boq_smoke and "CurrentReads" not in boq_smoke:
    errors.append("BOQ no-overread smoke does not observe Current reads")

require_known_count_shape("schedule", schedule)
require_known_count_shape("BOQ", boq)

if errors:
    print("ERROR: pinned QS3D-Platform is below the Quantity/BOQ hostile-count hardening floor")
    if sha:
        print("Pinned gitlink:", sha)
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS pinned QS3D-Platform Quantity/BOQ hardening floor", sha)
