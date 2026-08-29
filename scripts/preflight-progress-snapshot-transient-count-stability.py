#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Progress/ProgressSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProgressSnapshotCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required = [
    "RequireKnownCountStable(source, knownCount, parameterName, label);",
    "if (!enumerator.MoveNext())",
    "var item = enumerator.Current;",
    "TransientCountGrowthFailsBeforeCurrent",
    "TransientCountShrinkFailsBeforeCurrent",
    "TransientNegativeCountFailsBeforeCurrent",
    "Equal(0, source.CurrentReads",
]
missing = [token for token in required if token not in source and token not in smoke]
if missing:
    raise SystemExit("Progress snapshot transient Count guard missing tokens: " + ", ".join(missing))

loop = source.index("while (true)")
pre = source.index("RequireKnownCountStable(source, knownCount, parameterName, label);", loop)
move = source.index("if (!enumerator.MoveNext())", pre)
post = source.index("RequireKnownCountStable(source, knownCount, parameterName, label);", move)
current = source.index("var item = enumerator.Current;", post)
if not loop < pre < move < post < current:
    raise SystemExit("Progress snapshot transient Count checks must straddle MoveNext and precede Current")

if "while (enumerator.MoveNext())" in source:
    raise SystemExit("Progress snapshot must not regress to foreach-style MoveNext/Current coupling")

print("PASS progress snapshot transient Count stability source guard")
