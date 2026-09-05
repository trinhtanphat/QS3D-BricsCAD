#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts/package-v26.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "function Get-HeldFileIdentity",
        "function Assert-HeldFileIdentityMatchesPath",
        "public static extern bool GetFileInformationByHandle(",
        "[QS3D.V26.PackageNativeFileIdentity]::GetFileInformationByHandle(",
        "dwVolumeSerialNumber",
        "nFileIndexHigh",
        "nFileIndexLow",
        "SafeFileHandle",
        "$admissionStream = [IO.File]::Open",
        "$admissionIdentity = Get-HeldFileIdentity -Stream $admissionStream",
        "$heldIdentity = Get-HeldFileIdentity -Stream $stream",
        "Assert-HeldFileIdentityMatchesPath -Held $admissionHeld",
        "Assert-HeldFileIdentityMatchesPath -Held $held",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 package held-input native identity marker missing: {token}")

    open_fn = text.find("function Open-HeldPackageInput")
    initial_safety = text.find("Assert-SafeInputFile -Path $Path", open_fn)
    admission_open = text.find("$admissionStream = [IO.File]::Open", initial_safety)
    admission_identity = text.find("$admissionIdentity = Get-HeldFileIdentity -Stream $admissionStream", admission_open)
    rebound_safety = text.find("Assert-SafeInputFile -Path $fullPath", admission_identity)
    admission_path_proof = text.find("Assert-HeldFileIdentityMatchesPath -Held $admissionHeld", rebound_safety)
    held_open = text.find("$stream = [IO.File]::Open", admission_path_proof)
    held_identity = text.find("$heldIdentity = Get-HeldFileIdentity -Stream $stream", held_open)
    bridge_compare = text.find("$admissionIdentity", held_identity)
    held_record = text.find("FileIdentity = $heldIdentity", bridge_compare)
    first_identity = text.find("Assert-HeldFileIdentityMatchesPath -Held $held", held_record)
    return_pos = text.find("return $held", first_identity)
    admission_dispose = text.find("$admissionStream.Dispose()", return_pos)
    ordered = (
        open_fn,
        initial_safety,
        admission_open,
        admission_identity,
        rebound_safety,
        admission_path_proof,
        held_open,
        held_identity,
        bridge_compare,
        held_record,
        first_identity,
        return_pos,
        admission_dispose,
    )
    if min(ordered) < 0 or not (
        open_fn < initial_safety < admission_open < admission_identity < rebound_safety <
        admission_path_proof < held_open < held_identity < bridge_compare < held_record <
        first_identity < return_pos < admission_dispose
    ):
        errors.append(
            "Open-HeldPackageInput must hold an admission handle, prove its current-path identity, "
            "bridge that identity to the long-lived stream, and keep admission locked until authority is established"
        )

    bind_fn = text.find("function Assert-HeldPathBinding")
    bind_identity = text.find("Assert-HeldFileIdentityMatchesPath -Held $Held", bind_fn)
    bind_return = text.find("}", bind_identity)
    if min(bind_fn, bind_identity, bind_return) < 0 or not (bind_fn < bind_identity < bind_return):
        errors.append("Assert-HeldPathBinding must re-prove held/current pathname native identity")

    helper_fn = text.find("function Assert-HeldFileIdentityMatchesPath")
    verifier_open = text.find("[IO.File]::Open(", helper_fn)
    verifier_identity = text.find("Get-HeldFileIdentity", verifier_open)
    mismatch = text.find("held file identity", verifier_identity)
    if min(helper_fn, verifier_open, verifier_identity, mismatch) < 0 or not (
        helper_fn < verifier_open < verifier_identity < mismatch
    ):
        errors.append("native identity helper must open the currently admitted pathname and compare its handle identity fail-closed")

    forbidden = (
        "# metadata-only identity is sufficient",
        "return $true # identity unavailable",
        "Write-Warning \"GetFileInformationByHandle",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"V26 package held-input identity must not downgrade fail-open: {token}")

    return errors


def main() -> int:
    if not TARGET.is_file():
        print("FAIL: missing scripts/package-v26.ps1")
        return 1
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)

    mutation_tokens = (
        "function Get-HeldFileIdentity",
        "function Assert-HeldFileIdentityMatchesPath",
        "public static extern bool GetFileInformationByHandle(",
        "[QS3D.V26.PackageNativeFileIdentity]::GetFileInformationByHandle(",
        "$admissionStream = [IO.File]::Open",
        "$admissionIdentity = Get-HeldFileIdentity -Stream $admissionStream",
        "Assert-HeldFileIdentityMatchesPath -Held $admissionHeld",
        "$heldIdentity = Get-HeldFileIdentity -Stream $stream",
        "FileIdentity = $heldIdentity",
        "Assert-HeldFileIdentityMatchesPath -Held $held",
        "Assert-HeldFileIdentityMatchesPath -Held $Held",
        "$admissionStream.Dispose()",
    )
    for token in mutation_tokens:
        if token not in source:
            continue
        mutated = source.replace(token, "MUTATED-V26-HELD-IDENTITY", 1)
        if not validate(mutated):
            failures.append(f"mutation probe escaped V26 held-input file-identity guard: {token}")

    if failures:
        print("V26 package held-input file-identity preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V26 package held inputs bridge an admission-handle identity to the long-lived held generation.")
    print(" - admission handle stays open while current pathname safety/identity is re-proved")
    print(" - long-lived stream must match the locked admission identity before authority is granted")
    print(" - later path-binding checks repeat the native identity proof")
    print(" - inability to prove identity remains fail-closed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
