#!/usr/bin/env python3
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
PLATFORM = ROOT / "external" / "QS3D-Platform"
SCHEDULE = PLATFORM / "src" / "QS3D.Platform.Quantity" / "QuantitySchedule.cs"
BOQ = PLATFORM / "src" / "QS3D.Platform.Quantity" / "BoqProjection.cs"
SCHEDULE_SMOKE = PLATFORM / "tests" / "QS3D.Platform.SmokeTests" / "QuantityScheduleKnownCountNoOverreadModuleSmoke.cs"
BOQ_SMOKE = PLATFORM / "tests" / "QS3D.Platform.SmokeTests" / "BoqKnownCountNoOverreadModuleSmoke.cs"

errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required pinned Platform hardening file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


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


sha = pinned_sha()
schedule = read(SCHEDULE)
boq = read(BOQ)
schedule_smoke = read(SCHEDULE_SMOKE)
boq_smoke = read(BOQ_SMOKE)

# The protected pin must carry the actual hostile-count regressions, not merely
# a version string or a parent-repository expected literal.
if schedule_smoke and "CurrentReadCount" not in schedule_smoke:
    errors.append("schedule no-overread smoke does not observe Current reads")
if boq_smoke and "CurrentReadCount" not in boq_smoke:
    errors.append("BOQ no-overread smoke does not observe Current reads")

# Known-count materialization must bound successful MoveNext() before Current.
# This deliberately rejects the older captured-Count + foreach implementation
# that can read one surplus Current before reporting cardinality drift.
for label, source in (("schedule", schedule), ("BOQ", boq)):
    if not source:
        continue
    if "advertisedCount" not in source and "knownCount" not in source:
        errors.append(f"{label} materializer no longer exposes an auditable known-count admission path")
    if "MoveNext()" not in source:
        errors.append(f"{label} known-count materializer lacks explicit MoveNext traversal")
    if re.search(r"foreach\s*\(\s*var\s+item\s+in\s+source\s*\)", source):
        errors.append(f"{label} materializer still uses captured-Count + foreach traversal")

if errors:
    print("ERROR: pinned QS3D-Platform is below the Quantity/BOQ hostile-count hardening floor")
    if sha:
        print("Pinned gitlink:", sha)
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS pinned QS3D-Platform Quantity/BOQ hardening floor", sha)
