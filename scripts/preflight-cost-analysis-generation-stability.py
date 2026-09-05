#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CostAnalysisGenerationStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableBuildUpGeneration",
    "RequireStableTradeGeneration",
    "SameBuildUpRateState",
    "SameTradeItemState",
    '"Build-up analysis rate collection content changed during traversal."',
    '"Trade analysis item collection content changed during traversal."',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing cost analysis generation-stability source contract: {token}")

required_smoke = [
    "BuildUpSameCountReplacementIsRejected",
    "TradeSameCountReplacementIsRejected",
    "StableCountedSourcesReplayExactlyOnce",
    "StreamingSourcesRemainSinglePassCompatible",
    "GetEnumeratorCalls == 2",
    "GetEnumeratorCalls == 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing cost analysis generation-stability regression: {token}")

print("PASS cost analysis generation stability preflight")
