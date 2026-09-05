from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs").read_text(encoding="utf-8")

start = source.index("public EstimatingPortfolio RemoveManualRateOverride(")
end = source.index("public EstimatingPortfolio MarkQuantitySourceStale(", start)
method = source[start:end]

required = [
    "if (!target.OverrideRate.HasValue)",
    "if (target.IsBlocked)",
    "if (target.IsStale)",
    "var next = target.WithoutOverride();",
    "auditLog.Append(new CommercialAuditRecord(",
    '"rate-override-removed"',
]
for token in required:
    if token not in method:
        raise SystemExit("Missing estimating manual-override removal admission contract: " + token)

has_override = method.index("if (!target.OverrideRate.HasValue)")
blocked = method.index("if (target.IsBlocked)")
stale = method.index("if (target.IsStale)")
replacement = method.index("var next = target.WithoutOverride();")
audit = method.index("auditLog.Append(new CommercialAuditRecord(")
if not (has_override < blocked < stale < replacement < audit):
    raise SystemExit("Blocked/stale manual-override removal admission must fail before replacement and audit mutation.")

smoke = (root / "tests/QS3D.Core.SmokeTests/EstimatingManualOverrideStaleSmoke.cs").read_text(encoding="utf-8")
for token in [
    "StaleLineRejectsManualOverrideRemovalWithoutAuditMutation",
    "BlockedLineRejectsManualOverrideRemovalWithoutAuditMutation",
    "ValidCurrentLineStillRemovesManualOverride",
    "audit.Events.Count != 0",
    "unchanged.OverrideRate != 12m",
    "updated.State != EstimatingReadinessState.Priced",
    '"rate-override-removed"',
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic manual-override removal smoke contract: " + token)

print("Estimating manual override removal admission preflight passed.")
