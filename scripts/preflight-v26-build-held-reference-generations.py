#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WRAPPER = ROOT / "scripts/build-v26-with-stable-references.ps1"
WORKFLOWS = (
    ROOT / ".github/workflows/bricscad-v26.yml",
    ROOT / ".github/workflows/release-v26.yml",
)

VERIFY = "-VerifyStatePath $StatePath"
LOCK = "[IO.FileShare]::Read"
DOTNET = "& dotnet @arguments"
EXIT = "$buildExitCode = $LASTEXITCODE"
DISPOSE = "$locks[$index].Dispose()"
CALL = r".\scripts\build-v26-with-stable-references.ps1"
DIRECT = "dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"


def validate_wrapper(text: str) -> list[str]:
    failures: list[str] = []
    required = (
        "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "assert-v26-host-reference-safety.ps1",
        VERIFY,
        LOCK,
        "System.Collections.Generic.List[System.IO.FileStream]",
        "[Environment]::SetEnvironmentVariable('BRICSCAD_V26_DIR'",
        DOTNET,
        EXIT,
        DISPOSE,
    )
    for token in required:
        if token not in text:
            failures.append(f"wrapper missing contract marker: {token}")

    first_verify = text.find(VERIFY)
    lock = text.find(LOCK, first_verify)
    second_verify = text.find(VERIFY, lock)
    build = text.find(DOTNET, second_verify)
    exit_capture = text.find(EXIT, build)
    third_verify = text.find(VERIFY, exit_capture)
    dispose = text.find(DISPOSE, third_verify)
    if not (0 <= first_verify < lock < second_verify < build < exit_capture < third_verify < dispose):
        failures.append(
            "wrapper ordering must be verify -> lock all refs -> verify -> dotnet build -> capture exit -> verify -> dispose"
        )
    if text.count(VERIFY) < 3:
        failures.append("wrapper must verify admitted state before lock admission, after locks, and after build")
    return failures


def validate_workflow(text: str, label: str) -> list[str]:
    failures: list[str] = []
    if CALL not in text:
        failures.append(f"{label} does not route V26 plugin build through held-reference wrapper")
    if DIRECT in text:
        failures.append(f"{label} retains naked V26 plugin dotnet build")
    if "V26_HOST_REFERENCE_STATE" not in text:
        failures.append(f"{label} lost V26 host-reference state binding")
    return failures


def main() -> int:
    wrapper = WRAPPER.read_text(encoding="utf-8")
    failures = validate_wrapper(wrapper)
    for path in WORKFLOWS:
        failures.extend(validate_workflow(path.read_text(encoding="utf-8"), path.name))

    for token in (VERIFY, LOCK, DOTNET, EXIT, DISPOSE):
        mutated = wrapper.replace(token, "MUTATED-V26-BUILD-REFERENCE-GENERATION", 1)
        if not validate_wrapper(mutated):
            failures.append(f"mutation probe escaped wrapper guard: {token}")

    if failures:
        print("V26 held-reference build preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V26 plugin builds hold admitted managed reference generations for the full dotnet build interval.")
    print(" - both manual V26 workflows use the repository-owned held-reference wrapper")
    print(" - the wrapper verifies state before/after lock admission and after build before disposing locks")
    print(" - native build exit status is captured immediately after dotnet")
    return 0


if __name__ == "__main__":
    sys.exit(main())
