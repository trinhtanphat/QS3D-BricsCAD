#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
text = SOURCE.read_text(encoding="utf-8")

match = re.search(
    r"private static IReadOnlyList<ProjectElement> ResolveOwnedElements\(ProjectState project, IEnumerable<ProjectElement> elements\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static int\? SnapshotKnownTargetCount",
    text,
    re.S,
)
if not match:
    raise SystemExit("FAIL Floor mutation target Count stability: ResolveOwnedElements not found")
body = match.group("body")

if "foreach (var element in elements)" in body:
    raise SystemExit("FAIL Floor mutation target Count stability: caller-controlled targets must not use foreach")

required = [
    "using (var enumerator = elements.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (knownTargetCount.HasValue && observed >= knownTargetCount.Value)",
    "var element = enumerator.Current;",
]
for token in required:
    if token not in body:
        raise SystemExit("FAIL Floor mutation target Count stability: missing required no-overread shape: " + token)

move = body.find("while (enumerator.MoveNext())")
known = body.find("if (knownTargetCount.HasValue && observed >= knownTargetCount.Value)")
current = body.find("var element = enumerator.Current;")
cap = body.find("if (observed >= MaxMutationTargetCount)")
if min(move, known, current, cap) < 0 or not (move < known < cap < current):
    raise SystemExit("FAIL Floor mutation target Count stability: admission must be MoveNext -> Count -> cap -> Current")

if "observed++;" not in body:
    raise SystemExit("FAIL Floor mutation target Count stability: admitted targets must advance observed count")
if "observed != knownTargetCount.Value" not in body:
    raise SystemExit("FAIL Floor mutation target Count stability: completed traversal equality check missing")
if "Project changed while Floor mutation targets were being enumerated" not in body:
    raise SystemExit("FAIL Floor mutation target Count stability: project freshness guard missing")

print("PASS Floor mutation target known-Count no-overread source guard")
