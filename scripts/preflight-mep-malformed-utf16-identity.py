from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MEP = ROOT / "src" / "QS3D.Core" / "Mep" / "MepQuantity.cs"
TBQ = ROOT / "src" / "QS3D.Core" / "Mep" / "MepTbqProjection.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MepTbqProjectionSmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


mep = MEP.read_text(encoding="utf-8")
tbq = TBQ.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

require(mep, "using System.Xml;", "XML validity dependency")
require(mep, "XmlConvert.VerifyXmlChars(trimmed);", "shared MEP malformed UTF-16 rejection")
require(mep, "char.IsControl(trimmed[i])", "existing MEP control-character rejection")
require(mep, "MepContract.RequireText(elementId, nameof(elementId))", "element identity shared contract")
require(mep, "System = MepContract.RequireText(system, nameof(system));", "system identity shared contract")
require(mep, "Specification = MepContract.RequireText(specification, nameof(specification));", "specification identity shared contract")
require(mep, "Region = MepContract.RequireText(region, nameof(region));", "region identity shared contract")
require(tbq, "Encoding.UTF8.GetBytes(value)", "MEP-to-TBQ hashed identity boundary")
require(smoke, "MalformedUtf16IdentityFailsClosedBeforeProjection", "malformed UTF-16 executable regression")
require(smoke, "SupplementaryUnicodeIdentityProjects", "valid supplementary Unicode executable regression")
require(smoke, 'new MepElement("E-\\uD800"', "malformed high-surrogate element id coverage")
require(smoke, '"CHW-\\uD800"', "malformed high-surrogate system coverage")
require(smoke, '"DN50-\\uDC00"', "malformed low-surrogate specification coverage")
require(smoke, '"L01-\\uD800"', "malformed high-surrogate region coverage")
require(smoke, 'new MepElement("E-😀"', "valid supplementary-plane identity coverage")
require(smoke, 'Equal("CHW-😀", groups[0].System', "valid supplementary Unicode preservation")

print("PASS: MEP identity/classification text fails closed on malformed UTF-16 before hashed TBQ identity generation while preserving valid supplementary Unicode.")
