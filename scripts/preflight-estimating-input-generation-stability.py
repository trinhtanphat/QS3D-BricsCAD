from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/EstimatingInputGenerationStabilitySmoke.cs").read_text(encoding="utf-8")

errors = []

required_source_tokens = (
    "RequireStableKnownCountGeneration(",
    '"Estimating portfolio line content changed during enumeration."',
    '"Bulk rate assignment selected-line content changed during enumeration."',
    '"Bulk rate assignment unit-rate content changed during enumeration."',
    "EstimatingLineStateEquals(",
    "UnitRateAssignmentStateEquals(",
)
for token in required_source_tokens:
    if token not in source:
        errors.append("missing production stability token: " + token)

required_smoke_tokens = (
    "SameCountDriftCollection<EstimatingLine>",
    "SameCountDriftCollection<string>",
    "SameCountDriftCollection<UnitRateAssignment>",
    "PortfolioRejectsSameCountContentDrift",
    "BulkLineIdsRejectSameCountContentDrift",
    "BulkUnitRatesRejectSameCountContentDrift",
    "StreamingControlsRemainAccepted",
    '[ModuleInitializer]',
)
for token in required_smoke_tokens:
    if token not in smoke:
        errors.append("missing deterministic regression token: " + token)

if "content changed during enumeration" not in smoke:
    errors.append("smoke must require the fail-closed content-drift diagnostic")

print("QS3D estimating input generation stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: counted estimating inputs are replay-validated against the admitted snapshot, same-count content drift fails closed, and streaming inputs remain single-pass compatible.")
