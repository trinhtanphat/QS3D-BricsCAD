#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityRevisionReport.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRevisionReportIdentityIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/quantity-revision-report-identity-integrity.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: quantity revision identity guard missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "using System.Xml;", "XML validation dependency")
    require(source, "XmlConvert.VerifyXmlChars(value);", "canonical XML character validation")
    require(source, "contains characters that are invalid in XML", "stable invalid-XML diagnostic")
    require(source, "ValidateCanonicalRequired(beforeProjectId, \"before project id\")", "project identity admission")
    require(source, "ValidateCanonicalRequired(element.ElementId, label + \" element id\")", "element identity admission")
    require(source, "ValidateCanonicalRequired(quantity.Key, label + \" element \" + element.ElementId + \" quantity key\")", "build quantity identity admission")
    require(source, "ValidateCanonicalRequired(row.ElementId, \"summary row \" + index + \" element id\")", "summary element identity admission")
    require(source, "ValidateCanonicalRequired(row.QuantityName, \"summary row \" + index + \" quantity key\")", "summary quantity identity admission")

    require(smoke, "BuildRejectsMalformedProjectIdentity", "project hostile regression")
    require(smoke, "BuildRejectsMalformedElementIdentity", "element hostile regression")
    require(smoke, "BuildRejectsMalformedQuantityIdentity", "quantity hostile regression")
    require(smoke, "SummarizeRejectsMalformedElementIdentity", "summary element hostile regression")
    require(smoke, "SummarizeRejectsMalformedQuantityIdentity", "summary quantity hostile regression")
    require(smoke, "ValidSupplementaryUnicodeRemainsAccepted", "valid Unicode control")
    require(smoke, "[ModuleInitializer]", "automatic smoke registration")

    require(runbook, "summary row element IDs", "summary element identity contract")
    require(runbook, "malformed UTF-16", "malformed UTF-16 contract")
    require(runbook, "XML-invalid", "XML-invalid contract")
    require(runbook, "supplementary-plane Unicode", "valid Unicode contract")
    require(runbook, "Runtime: NOT_APPLICABLE", "runtime boundary")

    print("PASS quantity revision report identity integrity")
    return 0


if __name__ == "__main__":
    sys.exit(main())
