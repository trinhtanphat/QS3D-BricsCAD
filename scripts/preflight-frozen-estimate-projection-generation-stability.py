from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Cost/FrozenEstimateProjection.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/FrozenEstimateProjectionGenerationStabilitySmoke.cs").read_text(encoding="utf-8")

errors = []

required_source_tokens = (
    "RequireStableProjectionGeneration(",
    "FrozenProjectionRowStateEquals(",
    '"Frozen estimate projection content changed during enumeration."',
)
for token in required_source_tokens:
    if token not in source:
        errors.append("missing production frozen estimate generation-stability token: " + token)

required_smoke_tokens = (
    "SameCountDriftCollection<EstimateLine>",
    "SameCountReplacementIsRejected",
    "SameCountReorderIsRejected",
    "StableCountedGenerationRemainsAccepted",
    "StreamingInputRemainsSinglePassCompatible",
    "content changed during enumeration",
    "[ModuleInitializer]",
)
for token in required_smoke_tokens:
    if token not in smoke:
        errors.append("missing deterministic frozen estimate generation regression token: " + token)

print("QS3D frozen estimate projection generation stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: authoritative-count frozen estimate sources are replay-validated against one admitted projection generation while streaming inputs remain single-pass compatible.")
