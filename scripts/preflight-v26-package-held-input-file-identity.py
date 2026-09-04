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
        "GetFileInformationByHandle",
        "dwVolumeSerialNumber",
        "nFileIndexHigh",
        "nFileIndexLow",
        "SafeFileHandle",
        "Assert-HeldFileIdentityMatchesPath -Held $held",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 package held-input native identity marker missing: {token}")

    open_fn = text.find("function Open-HeldPackageInput")
    stream_open = text.find("[IO.File]::Open(", open_fn)
    held_record = text.find("FileIdentity = Get-HeldFileIdentity", stream_open)
    first_identity = text.find("Assert-HeldFileIdentityMatchesPath -Held $held", stream_open)
    return_pos = text.find("return $held", stream_open)
    if min(open_fn, stream_open, held_record, first_identity, return_pos) < 0 or not (
        open_fn < stream_open < held_record < first_identity < return_pos
    ):
        errors.append("Open-HeldPackageInput must capture native identity and prove current-path identity before granting the held input")

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
        "GetFileInformationByHandle",
        "FileIdentity = Get-HeldFileIdentity",
        "Assert-HeldFileIdentityMatchesPath -Held $held",
        "Assert-HeldFileIdentityMatchesPath -Held $Held",
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

    print("PASS: V26 package held inputs are bound to current admitted pathnames by Windows native file identity.")
    print(" - held stream identity is captured before authority is granted")
    print(" - current pathname is independently opened and identity-compared")
    print(" - later path-binding checks repeat the native identity proof")
    print(" - inability to prove identity remains fail-closed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
