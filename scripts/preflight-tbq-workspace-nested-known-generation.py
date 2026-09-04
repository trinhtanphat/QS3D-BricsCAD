#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqWorkspaceNestedKnownGenerationSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "SnapshotNestedGeneration(",
    "SameRateReferenceState",
    "SameLibraryEntryState",
    "rateReferenceSnapshot",
    "libraryEntrySnapshot",
    "content changed across semantic generation replay",
]
required_smoke = [
    "RejectsRateReferenceContentDriftAcrossCountedGeneration();",
    "RejectsLibraryEntryContentDriftAcrossCountedGeneration();",
    "AcceptsHonestCountedNestedSources();",
    "LeavesUncountedNestedSourcesSinglePass();",
    "EnumerationCount == 2",
    "EnumerationCount == 1",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    print("FAIL TBQ workspace nested known-generation source contract")
    for token in missing:
        print(" - missing:", token)
    raise SystemExit(1)

if "RateReferences = new RateReferenceGraph(Bounded(" in source:
    print("FAIL rate-reference known-Count identity is still erased by Bounded iterator")
    raise SystemExit(1)
if "LibraryId,\n                Bounded(" in source:
    print("FAIL BQ-library known-Count identity is still erased by Bounded iterator")
    raise SystemExit(1)

print("PASS TBQ workspace nested known-generation source contract")
