#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "bricscad-v26.yml"
GENERATOR = ROOT / "scripts" / "new-v26-qualification-artifact-manifest.ps1"
VALIDATOR = ROOT / "scripts" / "assert-v26-qualification-artifact-manifest.ps1"


def require(path: Path, tokens: list[str]) -> None:
    text = path.read_text(encoding="utf-8")
    missing = [token for token in tokens if token not in text]
    if missing:
        raise SystemExit(f"{path.relative_to(ROOT)} missing provenance contract token(s): {missing}")


require(
    GENERATOR,
    [
        "SourceCommit must be exactly 40 lowercase hexadecimal characters",
        "EvidenceClass = 'V26_SOURCE_BUILD_QUALIFICATION'",
        "RuntimeEvidence = $runtimeState",
        "QS3D.BricsCAD.V26.dll",
        "QS3D.Core.dll",
        "HostReferences = @($hostRecords)",
        "bricscad.exe",
        "TD_MgdBrep.dll",
    ],
)
require(
    VALIDATOR,
    [
        "SourceCommit does not match expected source",
        "RuntimeEvidence must be exactly absent or present",
        "Assert-UniqueNames -Records $manifest.Payload",
        "Assert-UniqueNames -Records $manifest.HostReferences",
        "does not match the manifest payload identity",
        "V26_SOURCE_BUILD_QUALIFICATION",
    ],
)
require(
    WORKFLOW,
    [
        "Create and validate V26 qualification artifact provenance",
        "new-v26-qualification-artifact-manifest.ps1",
        "assert-v26-qualification-artifact-manifest.ps1",
        "-ExpectedSourceCommit '${{ github.sha }}'",
        "artifacts/bricscad-v26-qualification-manifest.json",
        "assert-v26-host-reference-safety.ps1 -BricsCadDir $env:BRICSCAD_V26_DIR -VerifyStatePath $env:V26_HOST_REFERENCE_STATE",
    ],
)

# Mutation controls: the guard must fail if any critical source/package binding is removed.
mutations = {
    "workflow manifest creation": (WORKFLOW, "new-v26-qualification-artifact-manifest.ps1"),
    "workflow expected SHA": (WORKFLOW, "-ExpectedSourceCommit '${{ github.sha }}'"),
    "generator evidence class": (GENERATOR, "EvidenceClass = 'V26_SOURCE_BUILD_QUALIFICATION'"),
    "validator duplicate payload defense": (VALIDATOR, "Assert-UniqueNames -Records $manifest.Payload"),
}
for label, (path, token) in mutations.items():
    text = path.read_text(encoding="utf-8")
    if token not in text:
        raise SystemExit(f"mutation control unavailable: {label}")
    mutated = text.replace(token, "", 1)
    if token in mutated:
        raise SystemExit(f"mutation control did not remove guarded token: {label}")

print("PASS V26 qualification artifact provenance source guard")
