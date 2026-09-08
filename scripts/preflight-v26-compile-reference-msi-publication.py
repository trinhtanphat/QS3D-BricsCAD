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
        "$publishedByThisAttempt = $false",
        "$publishedByThisAttempt = $true",
        "$Candidate.Stream.Position = 0",
        "$Candidate.Stream.CopyTo($destinationStream)",
        "$destinationStream.Flush($true)",
        "Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256",
        "published V26 MSI digest does not match admitted staged generation",
        "published V26 MSI product identity does not match admitted staged generation",
        "published V26 MSI signer does not match admitted staged generation",
        "if ($publishedByThisAttempt)",
        "Assert-NoExistingReparseComponent -Path $Destination -Label 'Failed owned V26 canonical MSI publication'",
        "$failedPublication = Get-OrdinaryFileOrNull -Path $Destination -Label 'Failed owned V26 canonical MSI publication'",
        "[IO.File]::Delete($Destination)",
        "V26 canonical MSI pathname still exists after owned failed-publication cleanup.",
        "owned canonical MSI cleanup failed",
        "Assert-NoExistingReparseComponent -Path $msi -Label 'V26 MSI canonical path after source failure'",
        "left the canonical V26 MSI destination non-fresh",
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
        "V26 MSI publication failed after canonical destination creation; leaving the destination untouched for fail-closed re-admission",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"V26 MSI publication retains unbound/destructive/stale failure token: {token}")

    before(text, "$publishedByThisAttempt = $false", "$Destination,\n            [IO.FileMode]::CreateNew", "ownership flag initialized before destination creation", errors)
    before(text, "$Destination,\n            [IO.FileMode]::CreateNew", "$publishedByThisAttempt = $true", "ownership claimed only after CreateNew succeeds", errors)
    before(text, "$Candidate.Stream.Position = 0", "$Candidate.Stream.CopyTo($destinationStream)", "rewind held admitted stream before publication copy", errors)
    before(text, "$Candidate.Stream.CopyTo($destinationStream)", "$destinationStream.Flush($true)", "held byte copy before durable flush", errors)
    before(text, "$destinationStream.Flush($true)", "Get-SingleV26InstallerAdmission -Path $Destination -Expected $Candidate.Sha256", "durable publication before destination re-admission", errors)
    before(text, "if ($publishedByThisAttempt)", "[IO.File]::Delete($Destination)", "delete only inside attempt-owned cleanup", errors)
    before(text, "[IO.File]::Delete($Destination)", "V26 canonical MSI pathname still exists after owned failed-publication cleanup.", "owned deletion before absence verification", errors)
    before(text, "Assert-NoExistingReparseComponent -Path $msi -Label 'V26 MSI canonical path after source failure'", "left the canonical V26 MSI destination non-fresh", "outer fallback checks canonical freshness before warning/continue", errors)
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
        "attempt ownership": text.replace("$publishedByThisAttempt = $true", "$publishedByThisAttempt = $false", 1),
        "owned failed-publication cleanup": text.replace("[IO.File]::Delete($Destination)", "$null = $failedPublication", 1),
        "owned cleanup verification": text.replace("V26 canonical MSI pathname still exists after owned failed-publication cleanup.", "owned cleanup unchecked", 1),
        "outer non-fresh fail closed": text.replace("left the canonical V26 MSI destination non-fresh", "fallback continued with ambiguous canonical path", 1),
    }
    for label, mutated in probes.items():
        if not validate(mutated):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 compile-reference MSI held-byte publication with attempt-owned failed-publication rollback")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
