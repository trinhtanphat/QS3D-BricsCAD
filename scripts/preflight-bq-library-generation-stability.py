#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BqLibraryGenerationStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableEntryGeneration",
    "SameEntryState",
    'collectionLabel + " content changed during traversal."',
    "ReferenceUnitRate",
    'RequireStableEntryGeneration(entries, knownEntryCount, snapshot, "BQ library entry collection")',
    'RequireStableEntryGeneration(projectEntries, knownProjectEntryCount, admittedEntries, "BQ project import collection")',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing BQ library generation-stability source contract: {token}")

required_smoke = [
    "ConstructorSameCountReplacementIsRejected",
    "ImportSameCountReorderIsRejected",
    "StableCountedSourcesRemainAccepted",
    "StreamingSourcesRemainSinglePassCompatible",
    "GetEnumeratorCalls == 2",
    "GetEnumeratorCalls == 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing BQ library generation-stability regression: {token}")

print("PASS BQ library generation stability preflight")
