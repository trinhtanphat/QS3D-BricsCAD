#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "SelectionState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SelectionStateKnownCountStabilitySmoke.cs"


def validate(source: str, smoke: str) -> list[str]:
    errors: list[str] = []
    replace_start = source.find("public void Replace(IEnumerable<string> ids)")
    replace_end = source.find("public void Clear()", replace_start)
    if replace_start < 0 or replace_end <= replace_start:
        return ["SelectionState.Replace scope is missing"]
    body = source[replace_start:replace_end]

    known = body.find("var knownCount = ResolveKnownCount(ids);")
    pre = body.find("RequireStableKnownCount(ids, knownCount);", known)
    acquire = body.find("using (var enumerator = ids.GetEnumerator())", pre)
    post = body.find("RequireStableKnownCount(ids, knownCount);", acquire)
    loop = body.find("while (true)", post)
    first_move = body.find("enumerator.MoveNext()", loop)
    if min(known, pre, acquire, post, loop, first_move) < 0 or not (known < pre < acquire < post < loop < first_move):
        errors.append("SelectionState must rebound known Count immediately around enumerator acquisition before traversal")

    required_smoke = (
        "EnumeratorAcquisitionCountDriftFailsBeforeTraversal",
        "AcquisitionCountDriftCollection",
        "GetEnumeratorCalls",
        "MoveNextCalls",
        "CurrentReads",
        "known Count cannot be negative",
        "exposes conflicting known Counts",
    )
    for token in required_smoke:
        if token not in smoke:
            errors.append("SelectionState acquisition regression missing token: " + token)
    return errors


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
errors = validate(source, smoke)
if errors:
    raise SystemExit("SelectionState enumerator Count integrity failed: " + "; ".join(errors))

needle = "            using (var enumerator = ids.GetEnumerator())\n            {\n                RequireStableKnownCount(ids, knownCount);"
if needle not in source:
    raise SystemExit("SelectionState enumerator Count regression probe target is missing")
mutated = source.replace(
    needle,
    "            using (var enumerator = ids.GetEnumerator())\n            {",
    1,
)
if not validate(mutated, smoke):
    raise SystemExit("SelectionState enumerator Count regression probe did not fail closed")

print("PASS: SelectionState rebounds caller-known Count around GetEnumerator before any MoveNext/Current or publication.")
