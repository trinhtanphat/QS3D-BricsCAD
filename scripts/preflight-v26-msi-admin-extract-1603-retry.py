#!/usr/bin/env python3
"""Fail-closed source guard for bounded V26 MSI 1603 administrative-extraction retry."""

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v26-compile-references.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required_literals = (
        "$maxAdminExtractionAttempts = 2",
        "for ($attempt = 1; $attempt -le $maxAdminExtractionAttempts; $attempt++)",
        "Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction Stop",
        "$process.ExitCode -eq 1603",
        "[string]::IsNullOrWhiteSpace([string]$completeReferenceDirAfter1603)",
        "$attempt -lt $maxAdminExtractionAttempts",
        "retrying once with a clean extraction directory",
        "Assert-HeldInstallerStable -Held $admission -Phase \"after incomplete administrative extraction attempt $attempt\"",
        "Start-Sleep -Seconds 5",
        "Get-CompleteV26ReferenceDirectory -Root $extract",
        "BrxMgd.dll, TD_Mgd.dll, and TD_MgdBrep.dll were not found together in one extracted V26 runtime directory.",
    )
    for literal in required_literals:
        if literal not in text:
            errors.append(f"missing bounded-retry contract literal: {literal}")

    retry = re.search(
        r"catch\s*\{\s*if \(\$null -ne \$process -and \$process\.HasExited -and "
        r"\$process\.ExitCode -eq 1603 -and \[string\]::IsNullOrWhiteSpace\(\[string\]\$completeReferenceDirAfter1603\) -and "
        r"\$attempt -lt \$maxAdminExtractionAttempts\) \{(?P<body>.*?)continue\s*\}\s*throw\s*\}",
        text,
        flags=re.DOTALL,
    )
    if not retry:
        errors.append("could not locate fail-closed incomplete-1603-only retry catch block")
    else:
        body = retry.group("body")
        for literal in (
            "retrying once with a clean extraction directory",
            "Assert-HeldInstallerStable",
            "Start-Sleep -Seconds 5",
        ):
            if literal not in body:
                errors.append(f"1603 retry catch block missing: {literal}")

    if "if ($process.ExitCode -eq 1603 -and -not [string]::IsNullOrWhiteSpace([string]$completeReferenceDirAfter1603))" not in text:
        errors.append("late 1603 acceptance is no longer gated by complete reference payload validation")

    return errors


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("\n".join(errors))

    mutations = (
        text.replace("$maxAdminExtractionAttempts = 2", "$maxAdminExtractionAttempts = 3", 1),
        text.replace(
            "$process.ExitCode -eq 1603 -and [string]::IsNullOrWhiteSpace([string]$completeReferenceDirAfter1603) -and",
            "$process.ExitCode -eq 1605 -and [string]::IsNullOrWhiteSpace([string]$completeReferenceDirAfter1603) -and",
            1,
        ),
        text.replace("Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction Stop", "Write-Host 'reuse dirty extraction tree'", 1),
    )
    for index, mutated in enumerate(mutations, start=1):
        if not validate(mutated):
            raise SystemExit(f"mutation probe {index} was not rejected")

    print("PASS: V26 MSI incomplete 1603 extraction retries exactly once on a clean tree and remains fail-closed for other outcomes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
