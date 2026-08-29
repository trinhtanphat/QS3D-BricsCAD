#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Coordination/CoordinationRuleMatrix.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CoordinationRuleCollectionKnownCountNoOverreadSmoke.cs").read_text(encoding="utf-8")
runbook = ROOT / "docs/FEATURE-RUNBOOKS/coordination-rule-known-count-no-overread.md"

required_source = [
    "using (var enumerator = items.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (hasKnownCount && observedCount >= knownCount)",
    "if (observedCount >= MaximumEntries)",
    "var item = enumerator.Current;",
    "known Count changed during traversal",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"coordination Count no-overread source guard missing token: {token}")

move = source.index("while (enumerator.MoveNext())")
known = source.index("if (hasKnownCount && observedCount >= knownCount)", move)
ceiling = source.index("if (observedCount >= MaximumEntries)", known)
current = source.index("var item = enumerator.Current;", ceiling)
if not (move < known < ceiling < current):
    raise SystemExit("coordination collection must check known Count and ceiling after MoveNext but before Current")

if "foreach (var item in items)" in source:
    raise SystemExit("coordination collection must not regress to foreach before cardinality admission")

required_smoke = [
    "[ModuleInitializer]",
    "KnownCountOverrunRejectsBeforeExtraCurrent",
    "StreamingCeilingRejectsBeforeOverflowCurrent",
    "UnderYieldAndCountDriftReject",
    "ConflictingAndNegativeCountsRejectBeforeTraversal",
    "HonestCountedAndStreamingInputsRemainAccepted",
    "Equal(1, source.CurrentReads, \"known Count overrun Current\")",
    "Equal(10000, source.CurrentReads, \"streaming ceiling Current\")",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"coordination Count no-overread smoke guard missing token: {token}")

if not runbook.is_file():
    raise SystemExit("coordination Count no-overread runbook is missing")

print("PASS coordination rule collection known-Count no-overread guard")
