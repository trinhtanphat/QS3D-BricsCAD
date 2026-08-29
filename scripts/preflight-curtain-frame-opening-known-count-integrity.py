from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainFrameOpeningKnownCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/curtain-frame-opening-known-count-integrity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var frameEnumerator = frames.GetEnumerator())",
    "while (frameEnumerator.MoveNext())",
    "if (frameKnownCount.HasValue && result.Count >= frameKnownCount.Value)",
    "var frame = frameEnumerator.Current;",
    "using (var openingEnumerator = openings.GetEnumerator())",
    "while (openingEnumerator.MoveNext())",
    "if (openingKnownCount.HasValue && cuts.Count >= openingKnownCount.Value)",
    "var opening = openingEnumerator.Current;",
    "RequireStableKnownCount(frames, frameKnownCount, frameKnownCountSources",
    "RequireStableKnownCount(openings, openingKnownCount, openingKnownCountSources",
    "currentKnownCountSources != initialKnownCountSources",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"Curtain frame/opening Count-integrity source guard missing token: {token}")

if source.index("if (frameKnownCount.HasValue && result.Count >= frameKnownCount.Value)") > source.index("var frame = frameEnumerator.Current;"):
    raise SystemExit("Curtain frame known-Count guard must run before IEnumerator.Current")
if source.index("if (openingKnownCount.HasValue && cuts.Count >= openingKnownCount.Value)") > source.index("var opening = openingEnumerator.Current;"):
    raise SystemExit("Curtain opening known-Count guard must run before IEnumerator.Current")
if "foreach (var frame in frames)" in source or "foreach (var opening in openings)" in source:
    raise SystemExit("Curtain caller-controlled frame/opening traversal must not regress to foreach")

required_smoke = [
    "[ModuleInitializer]",
    "RejectFrameOverrunBeforeCurrent",
    "RejectOpeningOverrunBeforeCurrent",
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
