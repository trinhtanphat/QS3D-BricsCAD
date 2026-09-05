#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateReferenceGraphGenerationStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableEdgeGeneration",
    "Rate reference edge source content changed during traversal.",
    "SourceRateCode",
    "TargetKind",
    "TargetId",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing RateReferenceGraph generation-stability source contract: {token}")

required_smoke = [
    "SameCountReplacementIsRejected",
    "SameCountReorderIsRejected",
    "StableCountedGenerationRemainsAccepted",
    "StreamingInputRemainsSinglePassCompatible",
    "GetEnumeratorCalls == 2",
    "GetEnumeratorCalls == 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing RateReferenceGraph generation-stability regression: {token}")

print("PASS rate reference graph generation stability preflight")
