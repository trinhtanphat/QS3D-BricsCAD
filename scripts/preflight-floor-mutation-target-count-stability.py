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
    "observed++;",
    "if (observed > MaxMutationTargetCount)",
    "if (knownTargetCount.HasValue && observed > knownTargetCount.Value)",
    "continue;",
    "var element = enumerator.Current;",
]
for token in required:
    if token not in body:
        raise SystemExit("FAIL Floor mutation target Count stability: missing required no-overread shape: " + token)

move = body.find("while (enumerator.MoveNext())")
observed = body.find("observed++;", move)
cap = body.find("if (observed > MaxMutationTargetCount)", observed)
known = body.find("if (knownTargetCount.HasValue && observed > knownTargetCount.Value)", cap)
current = body.find("var element = enumerator.Current;", known)
if min(move, observed, cap, known, current) < 0 or not (move < observed < cap < known < current):
    raise SystemExit("FAIL Floor mutation target Count stability: admission must be MoveNext -> observe -> cap -> Count admission -> Current")

known_slice = body[known:current]
if "continue;" not in known_slice:
    raise SystemExit("FAIL Floor mutation target Count stability: entries beyond known Count must continue bounded traversal without Current")
if "observed != knownTargetCount.Value" not in body:
    raise SystemExit("FAIL Floor mutation target Count stability: completed traversal equality check missing")
if "Project changed while Floor mutation targets were being enumerated" not in body:
    raise SystemExit("FAIL Floor mutation target Count stability: project freshness guard missing")

print("PASS Floor mutation target known-Count no-overread source guard")
