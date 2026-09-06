#!/usr/bin/env python3
"""Fail closed if V26 compile-reference MSI publication is not bound to held admitted bytes."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "acquire-v26-compile-references.ps1"


def before(text: str, first: str, second: str, label: str, errors: list[str]) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        errors.append(f"{label}: expected {first!r} before {second!r}")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "function Publish-AdmittedV26Installer",
        "rejected cached V26 MSI is left untouched because safe replacement requires a fresh canonical destination",
        "$Destination,\n            [IO.FileMode]::CreateNew",
        "$Candidate.Stream.Position = 0",
        "$Candidate.Stream.CopyTo($destinationStream)",
        "$destinationStream.Flush($true)",
        "Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256",
        "published V26 MSI digest does not match admitted staged generation",
        "published V26 MSI product identity does not match admitted staged generation",
        "published V26 MSI signer does not match admitted staged generation",
        "V26 MSI publication failed after canonical destination creation; leaving the destination untouched for fail-closed re-admission",
        "$admission = Publish-AdmittedV26Installer -Candidate $candidateAdmission -Destination $msi",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 MSI publication contract missing token: {token}")

    forbidden = (
        "$candidateAdmission.Stream.Dispose()\n                $candidateAdmission = $null\n                [IO.File]::Move($staging, $msi)",
        "Remove-Item -LiteralPath $existing.FullName -Force",
        "Remove-Item -LiteralPath $cached.FullName -Force",
        "Remove-Item -LiteralPath $ordinary.FullName -Force",
        "[IO.File]::Move($staging, $msi)",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"V26 MSI publication retains unbound/destructive pathname token: {token}")

    before(text, "$Candidate.Stream.Position = 0", "$Candidate.Stream.CopyTo($destinationStream)", "rewind held admitted stream before publication copy", errors)
    before(text, "$Candidate.Stream.CopyTo($destinationStream)", "$destinationStream.Flush($true)", "held byte copy before durable flush", errors)
    before(text, "$destinationStream.Flush($true)", "Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256", "durable publication before destination re-admission", errors)
    return errors


def main() -> int:
    text = SCRIPT.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("\n".join(errors))

    probes = {
        "fresh-only destination": text.replace("$Destination,\n            [IO.FileMode]::CreateNew", "$Destination,\n            [IO.FileMode]::Create", 1),
        "held admitted source": text.replace("$Candidate.Stream.CopyTo($destinationStream)", "[IO.File]::OpenRead($Candidate.Path).CopyTo($destinationStream)", 1),
        "durable flush": text.replace("$destinationStream.Flush($true)", "$destinationStream.Flush()", 1),
        "post-publication admission": text.replace("Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256", "$null", 1),
        "digest parity": text.replace("published V26 MSI digest does not match admitted staged generation", "digest ignored", 1),
        "rejected-cache fail closed": text.replace("rejected cached V26 MSI is left untouched because safe replacement requires a fresh canonical destination", "cached MSI removed", 1),
        "failed-publication fail closed": text.replace("V26 MSI publication failed after canonical destination creation; leaving the destination untouched for fail-closed re-admission", "failed destination removed", 1),
    }
    for label, mutated in probes.items():
        if not validate(mutated):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 compile-reference MSI held-byte publication")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
