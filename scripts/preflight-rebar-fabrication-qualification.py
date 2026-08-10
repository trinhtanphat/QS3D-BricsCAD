#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "RebarFabricationQualificationHealthService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarFabricationQualificationSmoke.cs"
SMOKE_REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"
REMOTE_SCOPE = ROOT / "docs" / "REMOTE-AGENT-SCOPE.md"


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
    smoke = read(SMOKE, "fabrication qualification smoke", failures)
    smoke_registration = read(SMOKE_REGISTRATION, "smoke registration", failures)
    remote_scope = read(REMOTE_SCOPE, "remote/local agent scope", failures)

    require(service, "QS3D.RebarFabrication.RequireQualification", "opt-in project gate", failures)
    require(service, "QS3D.RebarFabrication.StandardCode", "project standard provenance", failures)
    require(service, "QS3D.RebarFabrication.DetailingRevision", "project detailing revision", failures)
    require(service, "RebarFabricationStatus", "per-element approval status", failures)
    require(service, "RebarFabricationStandardCode", "per-element standard binding", failures)
    require(service, "RebarFabricationDetailingRevision", "per-element revision binding", failures)
    require(service, "GeneratedHandleOwnershipPolicy.RebarHandleKeys", "canonical generated-rebar discovery", failures)
    require(service, "ApprovedStatus = \"Approved\"", "explicit approval state", failures)
    require(service, "REBAR_FAB_REQUIREMENT_INVALID", "fail-closed invalid qualification switch", failures)
    require(service, "ParseRequirement(requirement, out var validRequirement)", "explicit qualification switch parser", failures)
    require(service, "required = true;", "invalid switch remains release-blocking", failures)
    require(service, "REBAR_FAB_STANDARD_MISSING", "fail-closed missing-standard issue", failures)
    require(service, "REBAR_FAB_REVISION_MISSING", "fail-closed missing-revision issue", failures)
    require(service, "REBAR_FAB_OUTPUT_MISSING", "fail-closed missing-output issue", failures)
    require(service, "REBAR_FAB_NOT_APPROVED", "fail-closed approval issue", failures)
    require(service, "REBAR_FAB_STANDARD_MISMATCH", "fail-closed standard-mismatch issue", failures)
    require(service, "REBAR_FAB_REVISION_MISMATCH", "fail-closed revision-mismatch issue", failures)
    require(service, ".Where(x => x != null && HasGeneratedRebarOutput(x))", "standalone null-safe rebar discovery", failures)

    require(smoke, "DisabledQualificationDoesNotBlockOrdinaryProjects", "disabled-gate regression", failures)
    require(smoke, "ExplicitFalseQualificationDoesNotBlockOrdinaryProjects", "explicit-false regression", failures)
    require(smoke, "InvalidQualificationSwitchFailsClosed", "invalid-switch regression", failures)
    require(smoke, "REBAR_FAB_REQUIREMENT_INVALID", "invalid-switch issue regression", failures)
    require(smoke, "EnabledQualificationFailsClosedWithoutEvidence", "missing-evidence regression", failures)
    require(smoke, "MismatchedElementEvidenceIsRejected", "mismatch regression", failures)
    require(smoke, "ApprovedMatchingEvidencePasses", "approved-evidence regression", failures)
    require(smoke, "NonRebarGeneratedOutputDoesNotSatisfyFabricationGate", "non-rebar ownership regression", failures)
    require(smoke, "GeneratedFoundationMeshHandles", "canonical mesh ownership regression", failures)
    require(smoke_registration, "RebarFabricationQualificationSmoke.Run();", "smoke registration", failures)

    require(remote_scope, "LOCAL_ONLY", "remote/local boundary marker", failures)
    require(remote_scope, "V25", "V25 local-only boundary", failures)

    forbidden_v25_paths = (
        "QS3D.BricsCAD" + ".V25",
        "ReleaseReadiness" + "Commands.cs",
        "HealthAll" + "Commands.cs",
        "RebarHealthAll" + "Commands.cs",
    )
    source = Path(__file__).read_text(encoding="utf-8")
    for token in forbidden_v25_paths:
        if token in source:
            failures.append("remote fabrication preflight must not inspect V25/native source: " + token)

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
        print("QS3D Core rebar fabrication qualification preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Core rebar fabrication qualification is fail-closed, provenance-bound, malformed-switch safe and regression-covered.")
    print("LOCAL_ONLY: V25 command/health/release integration and runtime proof are intentionally not inspected by this remote preflight.")
    print("NOTE: this gate validates declared evidence consistency only; standard-specific engineering values still require approved local/project input.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
