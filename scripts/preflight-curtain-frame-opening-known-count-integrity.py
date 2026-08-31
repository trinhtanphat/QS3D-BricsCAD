from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningKnownCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/curtain-frame-opening-known-count-integrity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var frameEnumerator = frames.GetEnumerator())",
    "while (true)",
    "if (!frameEnumerator.MoveNext())",
    "if (frameKnownCount.HasValue && result.Count >= frameKnownCount.Value)",
    "var frame = frameEnumerator.Current;",
    "using (var openingEnumerator = openings.GetEnumerator())",
    "if (!openingEnumerator.MoveNext())",
    "if (openingKnownCount.HasValue && cuts.Count >= openingKnownCount.Value)",
    "var opening = openingEnumerator.Current;",
    "RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources",
    "RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources",
    "currentKnownCountSources != initialKnownCountSources",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"Curtain frame/opening Count-integrity source guard missing token: {token}")

frame_loop = source.index("using (var frameEnumerator = frames.GetEnumerator())")
frame_pre = source.index("RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources", frame_loop)
frame_move = source.index("if (!frameEnumerator.MoveNext())", frame_pre)
frame_post = source.index("RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources", frame_move)
frame_overrun = source.index("if (frameKnownCount.HasValue && result.Count >= frameKnownCount.Value)", frame_post)
frame_current = source.index("var frame = frameEnumerator.Current;", frame_overrun)
if not frame_loop < frame_pre < frame_move < frame_post < frame_overrun < frame_current:
    raise SystemExit("Curtain frame traversal must rebind Count around MoveNext and reject overrun before Current")

opening_loop = source.index("using (var openingEnumerator = openings.GetEnumerator())")
opening_pre = source.index("RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources", opening_loop)
opening_move = source.index("if (!openingEnumerator.MoveNext())", opening_pre)
opening_post = source.index("RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources", opening_move)
opening_overrun = source.index("if (openingKnownCount.HasValue && cuts.Count >= openingKnownCount.Value)", opening_post)
opening_current = source.index("var opening = openingEnumerator.Current;", opening_overrun)
if not opening_loop < opening_pre < opening_move < opening_post < opening_overrun < opening_current:
    raise SystemExit("Curtain opening traversal must rebind Count around MoveNext and reject overrun before Current")

if "foreach (var frame in frames)" in source or "foreach (var opening in openings)" in source:
    raise SystemExit("Curtain caller-controlled frame/opening traversal must not regress to foreach")

required_smoke = [
    "[ModuleInitializer]",
    "RejectFrameOverrunBeforeCurrent",
    "RejectOpeningOverrunBeforeCurrent",
    "RejectFrameTransientGrowthAfterMoveNextBeforeCurrent",
    "RejectOpeningTransientNegativeAfterMoveNextBeforeCurrent",
    "RejectOpeningTransientShrinkBeforeNextMoveNext",
    "RejectFramePostTraversalCountDrift",
    "RejectOpeningPostTraversalNegativeCount",
    "RejectOpeningPostTraversalCountConflict",
    "AcceptStableMultiInterfaceCounts",
    "AcceptPureStreamingInputs",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"Curtain frame/opening Count-integrity smoke guard missing token: {token}")

if not RUNBOOK.exists():
    raise SystemExit("Curtain frame/opening Count-integrity runbook is missing")

print("PASS curtain frame/opening known-Count integrity source guard")
