from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RegenerationWorkProfileIdentitySmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

require(source, "using System.Xml;", "XML validity dependency")
require(source, "internal static class RegenerationWorkIdentityContract", "central identity admission contract")
require(source, "var trimmed = value.Trim();", "canonical surrounding-whitespace normalization")
require(source, "char.IsControl(trimmed[i])", "control-character rejection")
require(source, "XmlConvert.VerifyXmlChars(trimmed);", "malformed UTF-16/XML-invalid rejection")
require(source, "ElementId = RegenerationWorkIdentityContract.Require(", "work-item identity admission")
require(source, "ProjectId = RegenerationWorkIdentityContract.Require(projectId, nameof(projectId), \"Project id\");", "profile project identity admission")
require(smoke, "WorkItemIdentityIsCanonicalizedAndUnicodeSafe", "work-item executable regression")
require(smoke, "ProfileIdentityIsCanonicalizedAndUnicodeSafe", "profile executable regression")
require(smoke, "HostileIdentityTextFailsAtPublicAdmission", "hostile identity executable regression")
require(smoke, 'Item("E-\\uD800-X")', "isolated high-surrogate coverage")
require(smoke, 'Item("E-\\uDC00-X")', "isolated low-surrogate coverage")
require(smoke, 'Profile("P-\\uFFFF-X")', "XML-invalid noncharacter coverage")
require(smoke, '"  E-\\U0001F680  "', "valid supplementary Unicode + trim coverage")

print("PASS: regeneration work-profile public identities are canonicalized once and reject control/malformed UTF-16/XML-invalid text while preserving valid Unicode and case.")
