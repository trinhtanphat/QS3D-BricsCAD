#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "RebarFabricationQualificationHealthService.cs"
RELEASE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReleaseReadinessCommands.cs"
HEALTH_ALL = ROOT / "src" / "QS3D.BricsCAD.V25" / "HealthAllCommands.cs"
REBAR_HEALTH_ALL = ROOT / "src" / "QS3D.BricsCAD.V25" / "RebarHealthAllCommands.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarFabricationQualificationSmoke.cs"
SMOKE_REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def read(path, label, failures):
    if not path.is_file():
        failures.append("missing " + label + ": " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label, failures):
    if token not in text:
        failures.append(label + ": missing `" + token + "`")


def main():
    failures = []
    service = read(SERVICE, "fabrication qualification health service", failures)
    release = read(RELEASE, "ReleaseReadinessCommands.cs", failures)
    health_all = read(HEALTH_ALL, "HealthAllCommands.cs", failures)
    rebar_health_all = read(REBAR_HEALTH_ALL, "RebarHealthAllCommands.cs", failures)
    smoke = read(SMOKE, "fabrication qualification smoke", failures)
    smoke_registration = read(SMOKE_REGISTRATION, "smoke registration", failures)

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
    require(service, "REBAR_FAB_STANDARD_MISMATCH", "fail-closed standard-mismatch issue", failures)
    require(service, "REBAR_FAB_REVISION_MISMATCH", "fail-closed revision-mismatch issue", failures)

    integration = "new RebarFabricationQualificationHealthService().Inspect(project)"
    require(release, integration, "release-check integration", failures)
    require(health_all, integration, "health-all integration", failures)
    require(rebar_health_all, integration, "rebar-health-all integration", failures)
    require(health_all, "GeneratedHandleOwnershipPolicy.RebarHandleKeys", "health-all canonical rebar locator", failures)
    require(rebar_health_all, "GeneratedHandleOwnershipPolicy.RebarHandleKeys", "rebar-health-all canonical locator", failures)

    require(smoke, "DisabledQualificationDoesNotBlockOrdinaryProjects", "disabled-gate regression", failures)
    require(smoke, "EnabledQualificationFailsClosedWithoutEvidence", "missing-evidence regression", failures)
    require(smoke, "MismatchedElementEvidenceIsRejected", "mismatch regression", failures)
    require(smoke, "ApprovedMatchingEvidencePasses", "approved-evidence regression", failures)
    require(smoke, "NonRebarGeneratedOutputDoesNotSatisfyFabricationGate", "non-rebar ownership regression", failures)
    require(smoke, "GeneratedFoundationMeshHandles", "canonical mesh ownership regression", failures)
    require(smoke_registration, "RebarFabricationQualificationSmoke.Run();", "smoke registration", failures)

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

    print("PASS: opt-in rebar fabrication qualification is fail-closed, provenance-bound, regression-covered and wired into all/rebar/release health paths.")
    print("NOTE: this gate validates declared evidence consistency only; standard-specific engineering values still require approved local/project input.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
