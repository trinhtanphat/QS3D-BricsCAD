#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateBuildUpGenerationStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableComponentGeneration",
    "SameComponentState",
    '"Rate build-up component collection content changed during traversal."',
    "hasKnownComponentCount",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing rate build-up generation-stability source contract: {token}")

required_smoke = [
    "SameCountReplacementIsRejected",
    "StableCountedSourceReplaysExactlyOnce",
    "StreamingSourceRemainsSinglePassCompatible",
    "GetEnumeratorCalls == 2",
    "GetEnumeratorCalls == 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing rate build-up generation-stability regression: {token}")

print("PASS rate build-up generation stability preflight")
