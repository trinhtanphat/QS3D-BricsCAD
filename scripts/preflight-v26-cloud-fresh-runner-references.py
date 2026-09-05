#!/usr/bin/env python3
"""Fail-closed source guard for fresh-runner V26 reference extraction recovery."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"

FALLBACK_JOB_GUARD = (
    "if: ${{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' "
    "&& always() && needs.v26-reference-primary.outputs.ready != 'true' }}"
)
QUALIFY_READY_GUARD = (
    "!(needs.v26-reference-primary.outputs.ready != 'true' "
    "&& needs.v26-reference-fallback.outputs.ready != 'true')"
)


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "v26-reference-primary:",
        "v26-reference-fallback:",
        "runs-on: windows-latest",
        "continue-on-error: true",
        "needs:\n      - installer-cache\n      - v26-reference-primary",
        FALLBACK_JOB_GUARD,
        QUALIFY_READY_GUARD,
        "qs3d-v26-hostrefs-v1-${{ github.run_id }}-primary",
        "qs3d-v26-hostrefs-v1-${{ github.run_id }}-fallback",
        "bricscad-v26.2.07-x64-en-us-${{ needs.installer-cache.outputs.msi_sha256 }}",
        "-ExtractReferences | Select-Object -Last 1",
        "assert-v26-host-reference-safety.ps1",
        "foreach ($name in @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll'))",
        "Restore run-bound V26 host-reference handoff",
        "fail-on-cache-miss: true",
        "Bind run-bound BricsCAD V26 compile references",
    )
    for literal in required:
        if literal not in text:
            errors.append(f"missing fresh-runner V26 reference contract: {literal}")

    primary = text.split("  v26-reference-primary:", 1)
    fallback = text.split("  v26-reference-fallback:", 1)
    qualify = text.split("  qualify:", 1)
    if len(primary) != 2 or len(fallback) != 2 or len(qualify) != 2:
        return errors

    primary_body = primary[1].split("  v26-reference-fallback:", 1)[0]
    fallback_body = fallback[1].split("  qualify:", 1)[0]
    qualify_body = qualify[1].split("  release:", 1)[0]

    for label, body in (("primary", primary_body), ("fallback", fallback_body)):
        if body.count("-ExtractReferences | Select-Object -Last 1") != 1:
            errors.append(f"{label} must perform exactly one MSI reference extraction attempt")
        if "Restore exact admitted BricsCAD V26.2.07 installer" not in body:
            errors.append(f"{label} must restore the exact admitted MSI")
        if "assert-v26-host-reference-safety.ps1" not in body:
            errors.append(f"{label} must validate the extracted host generation")

    if FALLBACK_JOB_GUARD not in fallback_body:
        errors.append("fallback must run only after the primary fresh runner fails to produce a ready handoff")
    if "continue-on-error: true" in fallback_body:
        errors.append("fallback must remain fail-closed and must not continue on error")
    if QUALIFY_READY_GUARD not in qualify_body:
        errors.append("qualify must require a ready primary or fallback handoff")
    if "-ExtractReferences" in qualify_body:
        errors.append("qualify must consume a run-bound handoff and must not execute MSI extraction")
    if "BricsCAD-V26.2.07-x64.msi" in qualify_body:
        errors.append("qualify must not restore or consume the MSI directly")

    return errors


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("\n".join(errors))

    mutations = (
        text.replace(
            FALLBACK_JOB_GUARD,
            "if: ${{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' && always() }}",
            1,
        ),
        text.replace(
            "qs3d-v26-hostrefs-v1-${{ github.run_id }}-fallback",
            "qs3d-v26-hostrefs-v1-static-fallback",
            1,
        ),
        text.replace(
            "  v26-reference-fallback:\n",
            "  v26-reference-fallback:\n    continue-on-error: true\n",
            1,
        ),
        text.replace(
            "-ExtractReferences | Select-Object -Last 1",
            "| Select-Object -Last 1",
            1,
        ),
        text.replace(QUALIFY_READY_GUARD, "true", 1),
    )
    for index, mutated in enumerate(mutations, start=1):
        if not validate(mutated):
            raise SystemExit(f"mutation probe {index} was not rejected")

    print(
        "PASS: V26 cloud reference extraction recovers once on a distinct fresh runner "
        "and qualify consumes only a run-bound validated handoff."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
