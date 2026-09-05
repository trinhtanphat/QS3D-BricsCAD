from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityReportRevisionReview.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityReportRevisionIdentityIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/quantity-report-revision-identity-integrity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.exists() else ""

anchor = "private static string CanonicalIdentity(string value, string label)"
start = source.find(anchor)
if start < 0:
    raise SystemExit("missing QuantityReportRevisionService.CanonicalIdentity")
end = source.find("\n        }", start)
if end < 0:
    raise SystemExit("unable to bound CanonicalIdentity")
body = source[start:end]

required_source = [
    "string.IsNullOrWhiteSpace(raw)",
    "raw.Trim()",
    "char.IsControl",
    "XmlConvert.VerifyXmlChars(raw)",
    "invalid in XML",
]
for token in required_source:
    if token not in body:
        raise SystemExit(f"quantity report revision identity guard missing source token: {token}")

if "using System.Xml;" not in source:
    raise SystemExit("QuantityReportRevisionReview.cs must import System.Xml")

for token in ["REV\\uD800", "REV\\uFFFF", "REV\\U0001F680", "ModuleInitializer"]:
    if token not in smoke:
        raise SystemExit(f"quantity report revision identity smoke missing token: {token}")

for token in ["malformed UTF-16", "XML-invalid", "supplementary-plane Unicode", "NOT_APPLICABLE"]:
    if token not in runbook:
        raise SystemExit(f"quantity report revision identity runbook missing token: {token}")

print("PASS quantity report revision identity integrity")
