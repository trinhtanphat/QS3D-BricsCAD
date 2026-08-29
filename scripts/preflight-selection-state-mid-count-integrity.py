#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/SelectionState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SelectionStateMidCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableKnownCount(ids, knownCount);\n                    if (!enumerator.MoveNext()) break;\n                    RequireStableKnownCount(ids, knownCount);",
    "private static void RequireStableKnownCount(IEnumerable<string> ids, int? expectedCount)",
    "var observedCount = ResolveKnownCount(ids);",
    "Semantic selection known Count changed during traversal",
]
required_smoke = [
    "DriftAfterCurrentFailsBeforeNextMoveNext();",
    "MoveNextInducedDriftFailsBeforeCurrent();",
    "CrossInterfaceConflictFailsBeforeNextMoveNext();",
    "Equal(1, source.MoveNextCalls, \"pre-MoveNext drift MoveNext calls\")",
    "Equal(1, source.CurrentReads, \"MoveNext-induced drift Current reads\")",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("SelectionState mid-Count integrity preflight failed; missing contract token(s): " + repr(missing))

legacy = "while (enumerator.MoveNext())"
if legacy in source:
    raise SystemExit("SelectionState mid-Count integrity preflight failed: traversal regressed to terminal-only while(MoveNext) shape")

print("PASS SelectionState mid-traversal known-Count integrity source guard")
