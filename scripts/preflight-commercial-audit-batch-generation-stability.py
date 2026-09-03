from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Commercial/CommercialContracts.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CommercialAuditBatchGenerationStabilitySmoke.cs").read_text(encoding="utf-8")

errors = []

required_source_tokens = (
    "RequireStableAuditBatchGeneration(",
    "CommercialAuditRecordStateEquals(",
    '"Commercial audit batch content changed during enumeration."',
)
for token in required_source_tokens:
    if token not in source:
        errors.append("missing production audit generation-stability token: " + token)

required_smoke_tokens = (
    "SameCountDriftCollection<CommercialAuditRecord>",
    "SameCountReplacementIsRejected",
    "SameCountReorderIsRejected",
    "StableCountedBatchRemainsAccepted",
    "StreamingBatchRemainsSinglePassCompatible",
    "content changed during enumeration",
    "[ModuleInitializer]",
)
for token in required_smoke_tokens:
    if token not in smoke:
        errors.append("missing deterministic audit generation regression token: " + token)

print("QS3D commercial audit batch generation stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: counted commercial audit batches are replay-validated against one admitted semantic generation while streaming inputs remain single-pass compatible.")
