from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs").read_text(encoding="utf-8")

start = source.index("public EstimatingPortfolio ApplyManualRateOverride(")
end = source.index("public EstimatingPortfolio RemoveManualRateOverride(", start)
method = source[start:end]

required = [
    "if (!target.ReferencedRate.HasValue)",
    "if (target.IsBlocked)",
    "if (target.IsStale)",
    "var next = target.WithOverride(overrideRate, reason);",
    "auditLog.Append(new CommercialAuditRecord(",
]
for token in required:
    if token not in method:
        raise SystemExit("Missing estimating manual-override stale admission contract: " + token)

base_rate = method.index("if (!target.ReferencedRate.HasValue)")
blocked = method.index("if (target.IsBlocked)")
stale = method.index("if (target.IsStale)")
replacement = method.index("var next = target.WithOverride(overrideRate, reason);")
audit = method.index("auditLog.Append(new CommercialAuditRecord(")
if not (base_rate < blocked < stale < replacement < audit):
    raise SystemExit("Stale/blocked manual-override admission must fail before replacement and audit mutation.")

smoke = (root / "tests/QS3D.Core.SmokeTests/EstimatingManualOverrideStaleSmoke.cs").read_text(encoding="utf-8")
for token in [
    "StaleLineRejectsManualOverrideWithoutAuditMutation",
    "ValidCurrentLineStillAcceptsManualOverride",
    "audit.Events.Count != 0",
    "updated.State != EstimatingReadinessState.PricedWithOverride",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic stale manual-override smoke contract: " + token)

print("Estimating stale manual override admission preflight passed.")
