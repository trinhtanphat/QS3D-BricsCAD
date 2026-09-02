from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Commercial" / "EstimatingWorkflow.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "CommercialEstimatingWorkflowSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

apply_start = source.index("public EstimatingPortfolio ApplyManualRateOverride(")
remove_start = source.index("public EstimatingPortfolio RemoveManualRateOverride(")
stale_start = source.index("public EstimatingPortfolio MarkQuantitySourceStale(")
apply = source[apply_start:remove_start]
remove = source[remove_start:stale_start]

apply_stale = 'if (target.IsStale)\n                throw new InvalidOperationException("A stale estimating line cannot receive a manual rate override.");'
remove_stale = 'if (target.IsStale)\n                throw new InvalidOperationException("A stale estimating line cannot remove a manual rate override.");'

if apply_stale not in apply:
    raise SystemExit("commercial stale manual-rate preflight failed: apply override lacks stale fail-closed admission")
if remove_stale not in remove:
    raise SystemExit("commercial stale manual-rate preflight failed: remove override lacks stale fail-closed admission")
if apply.index("if (target.IsStale)") > apply.index("target.WithOverride("):
    raise SystemExit("commercial stale manual-rate preflight failed: apply stale admission occurs after WithOverride")
if remove.index("if (target.IsStale)") > remove.index("target.WithoutOverride()"):
    raise SystemExit("commercial stale manual-rate preflight failed: remove stale admission occurs after WithoutOverride")
if apply.index("if (target.IsStale)") > apply.index("auditLog.Append("):
    raise SystemExit("commercial stale manual-rate preflight failed: apply stale admission occurs after audit append")
if remove.index("if (target.IsStale)") > remove.index("auditLog.Append("):
    raise SystemExit("commercial stale manual-rate preflight failed: remove stale admission occurs after audit append")

for anchor in [
    "staleWithOverride",
    "override-stale-remove",
    "L1 stale preserved override rate",
    "L1 stale preserved overridden historical amount",
    "override-stale-create",
    "L1 stale preserved historical amount after rejected override",
    "Equal(4, audit.Events.Count);",
    "Equal(5, audit.Events.Count);",
]:
    if anchor not in smoke:
        raise SystemExit(f"commercial stale manual-rate preflight failed: missing smoke anchor {anchor!r}")

if smoke.count("Throws<InvalidOperationException>(() => service.RemoveManualRateOverride(") < 1:
    raise SystemExit("commercial stale manual-rate preflight failed: smoke does not reject stale override removal")
if smoke.count("Throws<InvalidOperationException>(() => service.ApplyManualRateOverride(") < 1:
    raise SystemExit("commercial stale manual-rate preflight failed: smoke does not reject stale override creation")

print("PASS commercial stale manual-rate immutability source guard")
