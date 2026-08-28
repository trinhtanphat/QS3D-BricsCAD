#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingKnownCountOverrunOrderingSmoke.cs"
PORTFOLIO_LEGACY = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingPortfolioCountIntegritySmoke.cs"
BULK_LEGACY = ROOT / "tests/QS3D.Core.SmokeTests/BulkRateAssignmentRequestCountIntegritySmoke.cs"

for path in (SOURCE, SMOKE, PORTFOLIO_LEGACY, BULK_LEGACY):
    if not path.is_file():
        raise SystemExit("Estimating known-Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
portfolio_legacy = PORTFOLIO_LEGACY.read_text(encoding="utf-8")
bulk_legacy = BULK_LEGACY.read_text(encoding="utf-8")

required_source = (
    "if (knownCount.HasValue && snapshot.Count >= knownCount.Value)",
    'throw new InvalidOperationException("Estimating portfolio line count changed during enumeration.");',
    "if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)",
    'throw new InvalidOperationException("Bulk rate assignment selected-line count changed during enumeration.");',
    "if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)",
    'throw new InvalidOperationException("Bulk rate assignment unit-rate count changed during enumeration.");',
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Estimating known-Count preflight missing source contract: " + ", ".join(missing))

portfolio_guard = source.index("if (knownCount.HasValue && snapshot.Count >= knownCount.Value)")
portfolio_null = source.index('if (line == null) throw new ArgumentException("Estimating portfolio contains a null line."')
portfolio_duplicate = source.index("if (_byId.ContainsKey(line.LineId))")
if not portfolio_guard < portfolio_null or not portfolio_guard < portfolio_duplicate:
    raise SystemExit("Estimating portfolio Count-overrun guard must precede line semantic validation.")

line_guard = source.index("if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)")
line_token = source.index("var id = CommercialGuard.RequireToken(raw, nameof(lineIds));")
line_duplicate = source.index("if (!uniqueIds.Add(id))")
if not line_guard < line_token or not line_guard < line_duplicate:
    raise SystemExit("Bulk selected-line Count-overrun guard must precede token/duplicate validation.")

rate_guard = source.index("if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)")
rate_null = source.index('if (assignment == null) throw new ArgumentException("Bulk rate assignment contains a null unit rate."')
rate_duplicate = source.index("if (!units.Add(assignment.Unit))")
if not rate_guard < rate_null or not rate_guard < rate_duplicate:
    raise SystemExit("Bulk unit-rate Count-overrun guard must precede null/duplicate validation.")

required_smoke = (
    "[ModuleInitializer]",
    "PortfolioOverrunPrecedesUnexpectedLineValidation();",
    "SelectedLineOverrunPrecedesTokenValidation();",
    "UnitRateOverrunPrecedesUnexpectedAssignmentValidation();",
    "UnderTraversalStillFailsAfterOtherwiseValidEnumeration();",
    "HonestCountedInputsRemainAccepted();",
    "null!",
    'new MisreportedReadOnlyCollection<string>(1, "LINE-1", "")',
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Estimating known-Count preflight missing deterministic smoke contract: " + ", ".join(missing_smoke))

for legacy_token in (
    "NegativeKnownCountFailsBeforeEnumeration();",
    "OversizedKnownCountFailsBeforeEnumeration();",
    "ConflictingKnownCountsFailBeforeEnumeration();",
    "PureStreamStopsAtItem10001();",
    "DuplicateIdentityRemainsCaseInsensitive();",
):
    if legacy_token not in portfolio_legacy:
        raise SystemExit("Estimating portfolio legacy Count-integrity control missing: " + legacy_token)

for legacy_token in (
    "LineIdMalformedKnownCountsFailBeforeEnumeration();",
    "UnitRateMalformedKnownCountsFailBeforeEnumeration();",
    "PureStreamsPreserveIndependentBounds();",
    "HonestKnownCountsRemainAccepted();",
):
    if legacy_token not in bulk_legacy:
        raise SystemExit("Bulk-rate legacy Count-integrity control missing: " + legacy_token)

print("PASS estimating known-Count overrun ordering")
