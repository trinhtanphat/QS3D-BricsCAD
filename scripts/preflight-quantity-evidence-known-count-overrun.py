#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/QuantityEvidenceGraph.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityEvidenceKnownCountOverrunSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/issue-4298-quantity-evidence-known-count-overrun.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Quantity evidence known-Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    "var knownCount = ReadKnownCount(source, label);",
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    'throw new InvalidOperationException(label + " count changed during snapshot.");',
    "if (snapshot.Count >= MaximumItems)",
    "if (item is null)",
    "if (knownCount.HasValue && snapshot.Count != knownCount.Value)",
    "if (source is ICollection<T> genericCollection)",
    "if (source is IReadOnlyCollection<T> readOnlyCollection)",
    "if (source is ICollection collection)",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Quantity evidence known-Count preflight missing source contract: " + ", ".join(missing))

overrun_guard = source.index("if (knownCount.HasValue && snapshot.Count >= knownCount.Value)")
stream_guard = source.index("if (snapshot.Count >= MaximumItems)", overrun_guard)
null_guard = source.index("if (item is null)", overrun_guard)
append = source.index("snapshot.Add(item);", overrun_guard)
if not overrun_guard < stream_guard < null_guard < append:
    raise SystemExit("Quantity evidence Count-overrun guard must run before streaming/null/retention processing.")

required_smoke = (
    "[ModuleInitializer]",
    "OperandOverrunPrecedesUnexpectedNullValidation();",
    "ExplanationOverrunPrecedesUnexpectedNullValidation();",
    "UnderTraversalStillFailsAfterValidEnumeration();",
    "HonestCountedInputsRemainAcceptedAndOrdered();",
    "PureStreamingInputKeepsIndependentCapacityBound();",
    "null!",
    "new MisreportedCollection<QuantityEvidenceOperand>(2, valid)",
    "Stream(operand, 10001)",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Quantity evidence known-Count preflight missing deterministic smoke contract: " + ", ".join(missing_smoke))

for token in (
    "Lane-Key: `issue-4298`",
    "known Count",
    "10,000",
    "NOT_APPLICABLE",
    "MERGED_MAIN",
):
    if token not in runbook:
        raise SystemExit("Quantity evidence known-Count runbook missing contract token: " + token)

print("PASS quantity evidence known-Count overrun ordering")
