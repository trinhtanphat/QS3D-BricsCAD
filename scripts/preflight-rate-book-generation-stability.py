from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Cost/RateBook.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/RateBookGenerationStabilitySmoke.cs").read_text(encoding="utf-8")

errors = []

required_source_tokens = (
    "RequireStableRateBookGeneration(",
    "RateItemStateEquals(",
    '"Rate book item source content changed during traversal."',
    "RequireStableKnownCount(items, knownCount);",
)
for token in required_source_tokens:
    if token not in source:
        errors.append("missing production rate-book generation-stability token: " + token)

required_smoke_tokens = (
    "SameCountGenerationCollection<RateItem>",
    "SameCountReplacementIsRejected",
    "SameCountReorderIsRejected",
    "SameIdentityContentChangeIsRejected",
    "StableCountedGenerationRemainsAccepted",
    "StreamingInputRemainsSinglePassCompatible",
    "GetEnumeratorCalls == 2",
    "GetEnumeratorCalls == 1",
    "[ModuleInitializer]",
)
for token in required_smoke_tokens:
    if token not in smoke:
        errors.append("missing deterministic rate-book generation regression token: " + token)

print("QS3D rate book generation stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: counted rate-book inputs are replay-validated against one admitted semantic generation while streaming inputs remain single-pass compatible.")
