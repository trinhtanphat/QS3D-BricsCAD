from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RegenerationWorkProfileIdentitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "regeneration-work-profile-target-identity.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

require(source, "TargetElementIds = MaterializeTargetElementIds(", "dedicated target identity admission")
require(source, "var materialized = MaterializeBounded(targetElementIds, maxCount, parameterName, \"target element\");", "preserved bounded/count-safe traversal")
require(source, "RegenerationWorkIdentityContract.Require(", "shared identity contract reuse")
require(source, '"Regeneration work profile target element id"', "target identity diagnostic label")
require(source, "canonical.Add(RegenerationWorkIdentityContract.Require(", "canonical target publication")
require(smoke, "TargetIdentityIsCanonicalizedAndUnicodeSafe", "valid target identity regression")
require(smoke, 'ProfileWithTargets("T-\\u0001-X")', "target control-character regression")
require(smoke, 'ProfileWithTargets("T-\\uD800-X")', "target malformed high-surrogate regression")
require(smoke, 'ProfileWithTargets("T-\\uDC00-X")', "target malformed low-surrogate regression")
require(smoke, 'ProfileWithTargets("T-\\uFFFF-X")', "target XML-invalid regression")
require(smoke, '"  T-\\U0001F680  "', "valid target supplementary Unicode + trim regression")
require(runbook, "Lane-Key: `issue-5303`", "runbook lane identity")
require(runbook, "TargetElementIds", "runbook target identity boundary")
require(runbook, "known-Count", "runbook collection-integrity preservation")

print("PASS: regeneration work-profile target element ids reuse the canonical identity contract after bounded/count-safe detachment and before immutable publication.")
