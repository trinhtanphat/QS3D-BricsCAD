#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "RebarFabricationQualificationHealthService.cs"
RELEASE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReleaseReadinessCommands.cs"


def require(text, token, label, failures):
    if token not in text:
        failures.append(label + ": missing `" + token + "`")


def main():
    failures = []
    if not SERVICE.is_file():
        failures.append("missing fabrication qualification health service")
        service = ""
    else:
        service = SERVICE.read_text(encoding="utf-8")

    if not RELEASE.is_file():
        failures.append("missing ReleaseReadinessCommands.cs")
        release = ""
    else:
        release = RELEASE.read_text(encoding="utf-8")

    require(service, "QS3D.RebarFabrication.RequireQualification", "opt-in project gate", failures)
    require(service, "QS3D.RebarFabrication.StandardCode", "project standard provenance", failures)
    require(service, "QS3D.RebarFabrication.DetailingRevision", "project detailing revision", failures)
    require(service, "RebarFabricationStatus", "per-element approval status", failures)
    require(service, "RebarFabricationStandardCode", "per-element standard binding", failures)
    require(service, "RebarFabricationDetailingRevision", "per-element revision binding", failures)
    require(service, "GeneratedHandleOwnershipPolicy.RebarHandleKeys", "canonical generated-rebar discovery", failures)
    require(service, "ApprovedStatus = \"Approved\"", "explicit approval state", failures)
    require(service, "REBAR_FAB_STANDARD_MISSING", "fail-closed missing-standard issue", failures)
    require(service, "REBAR_FAB_REVISION_MISSING", "fail-closed missing-revision issue", failures)
    require(service, "REBAR_FAB_OUTPUT_MISSING", "fail-closed missing-output issue", failures)
    require(service, "REBAR_FAB_NOT_APPROVED", "fail-closed approval issue", failures)
    require(release, "new RebarFabricationQualificationHealthService().Inspect(project)", "release-check integration", failures)

    forbidden_claims = (
        "TCVN 5574 compliant",
        "ACI 318 compliant",
        "BS 8666 compliant",
        "automatically compliant",
    )
    for claim in forbidden_claims:
        if claim.lower() in service.lower():
            failures.append("fabrication gate must not claim automatic engineering compliance: " + claim)

    if failures:
        print("QS3D rebar fabrication qualification preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: opt-in rebar fabrication qualification is fail-closed, provenance-bound and wired into QS3DRELEASECHECK.")
    print("NOTE: this gate validates declared evidence consistency only; standard-specific engineering values still require approved local/project input.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
