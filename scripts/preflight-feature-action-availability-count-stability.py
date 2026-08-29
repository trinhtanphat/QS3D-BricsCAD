#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Features" / "FeatureActionBar.cs"
text = SOURCE.read_text(encoding="utf-8")

match = re.search(
    r"private static Dictionary<FeatureActionId, FeatureActionAvailability> SnapshotAvailability\(\s*IEnumerable<FeatureActionAvailability>\? availability\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static int\? SnapshotKnownCount",
    text,
    re.S,
)
if not match:
    raise SystemExit("FAIL Feature Action availability Count stability: SnapshotAvailability not found")
body = match.group("body")

if "foreach (var state in availability)" in body:
    raise SystemExit("FAIL Feature Action availability Count stability: caller-controlled availability must not use foreach")

required = [
    "using (var enumerator = availability.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (observed == Definitions.Length)",
    "if (expectedCount.HasValue && observed >= expectedCount.Value)",
    "var state = enumerator.Current;",
    "observed++;",
    "var reboundCount = SnapshotKnownCount(availability);",
    "if (expectedCount != reboundCount)",
]
for token in required:
    if token not in body:
        raise SystemExit("FAIL Feature Action availability Count stability: missing required no-overread shape: " + token)

move = body.find("while (enumerator.MoveNext())")
cap = body.find("if (observed == Definitions.Length)", move)
known = body.find("if (expectedCount.HasValue && observed >= expectedCount.Value)", cap)
current = body.find("var state = enumerator.Current;", known)
increment = body.find("observed++;", current)
rebound = body.find("var reboundCount = SnapshotKnownCount(availability);", increment)
if min(move, cap, known, current, increment, rebound) < 0 or not (move < cap < known < current < increment < rebound):
    raise SystemExit("FAIL Feature Action availability Count stability: admission must be MoveNext -> cap -> known Count -> Current -> observe -> rebound")

if "AvailabilityCountMismatch(expectedCount.Value, observed + 1)" not in body:
    raise SystemExit("FAIL Feature Action availability Count stability: known Count overrun must fail before Current")
if "expectedCount.HasValue && observed != expectedCount.Value" not in body:
    raise SystemExit("FAIL Feature Action availability Count stability: completed traversal equality check missing")
if "Feature action availability count changed during enumeration." not in body:
    raise SystemExit("FAIL Feature Action availability Count stability: post-traversal Count drift diagnostic missing")

print("PASS Feature Action availability known-Count no-overread source guard")
