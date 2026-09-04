#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Cost" / "RateBook.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RateBookKnownCountEvidencePrecedenceSmoke.cs"
ENTRY = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestEntryPoint.cs"


def fail(message: str) -> None:
    print(f"FAIL preflight-rate-book-known-count-evidence-precedence: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
entry = ENTRY.read_text(encoding="utf-8")

negative = 'if (hasNegativeKnownCount)'
conflict = 'if (conflictingKnownCounts)'
legacy_early = 'if (maximumKnownCount > MaxItems)\n                return true;'
constructor_bound = 'if (hasKnownCount && knownCount > MaxItems)\n                ThrowTooManyItems();'

for token in (negative, conflict, constructor_bound):
    if token not in source:
        fail(f"RateBook source is missing required token: {token}")

if legacy_early in source:
    fail("oversized Count short-circuits malformed/conflicting Count evidence")

if source.index(negative) > source.index(conflict):
    fail("negative Count evidence must retain precedence over conflicting Count evidence")

required_smoke_tokens = (
    'new HostileCountCollection(10001, -1, 10001)',
    'new HostileCountCollection(10001, 1, 10001)',
    'new HostileCountCollection(10001, 10001, 10001)',
    'Rate book item source reports an invalid negative known count.',
    'Rate book item source reports conflicting known counts.',
    'Rate book supports at most 10000 rate items.',
    'EnumerationAttempts',
)
for token in required_smoke_tokens:
    if token not in smoke:
        fail(f"deterministic smoke is missing hostile Count coverage token: {token}")

if 'RateBookKnownCountEvidencePrecedenceSmoke.Run();' not in entry:
    fail("deterministic smoke is not registered in SmokeTestEntryPoint")

print("PASS preflight-rate-book-known-count-evidence-precedence")
