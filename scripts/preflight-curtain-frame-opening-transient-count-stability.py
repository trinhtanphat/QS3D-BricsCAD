from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningKnownCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/curtain-frame-opening-transient-count-stability.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for subject, enumerator, known, sources, current in [
    ("frame", "frameEnumerator", "frameKnownCount", "frameKnownCountSources", "var frame = frameEnumerator.Current;"),
    ("opening", "openingEnumerator", "openingKnownCount", "openingKnownCountSources", "var opening = openingEnumerator.Current;"),
]:
    loop = source.index(f"using (var {enumerator}")
    pre_token = f"RequireStableKnownCount({subject}s, {known}, {sources}"
    pre = source.index(pre_token, loop)
    move = source.index(f"if (!{enumerator}.MoveNext())", pre)
    post = source.index(pre_token, move)
    current_index = source.index(current, post)
    if not loop < pre < move < post < current_index:
        raise SystemExit(f"Curtain {subject} transient Count checks must straddle MoveNext and precede Current")

if "while (frameEnumerator.MoveNext())" in source or "while (openingEnumerator.MoveNext())" in source:
    raise SystemExit("Curtain caller-controlled traversal must retain pre-MoveNext Count stability admission")

required_smoke = [
    "RejectFrameTransientGrowthAfterMoveNextBeforeCurrent",
    "RejectOpeningTransientNegativeAfterMoveNextBeforeCurrent",
    "RejectOpeningTransientShrinkBeforeNextMoveNext",
    "MoveNextCalls != 1 || frames.CurrentReads != 0",
    "MoveNextCalls != 1 || openings.CurrentReads != 0",
    "MoveNextCalls != 1 || openings.CurrentReads != 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"Curtain transient Count smoke guard missing token: {token}")

if not RUNBOOK.exists():
    raise SystemExit("Curtain frame/opening transient Count runbook is missing")

print("PASS curtain frame/opening transient Count stability source guard")
