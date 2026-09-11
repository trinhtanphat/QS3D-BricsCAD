#!/usr/bin/env python3
"""Fail closed unless the V25 parent-substitution race distinguishes protected vs post-completion replacement."""

from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "scripts" / "test-v25-commercial-safe-archive-extraction.ps1"


def contract_errors(text: str | None) -> list[str]:
    if text is None:
        return ["missing V25 commercial safe archive runtime harness"]
    errors: list[str] = []
    required = (
        ("outside-sentinel.txt", "outside sentinel fixture"),
        ("outside-must-survive", "outside sentinel value assertion"),
        ("Parent substitution wrote archive bytes outside the extraction root.", "zero-byte-escape assertion"),
        ("Parent substitution modified the outside sentinel.", "outside sentinel immutability assertion"),
        ("$attackerResult -contains 'REPLACED'", "replacement outcome classification"),
        ("$renamedOriginalInside = Join-Path $raceDestination 'race-original\\inside.txt'", "renamed original generation inspection"),
        ("proof that replacement occurred after protected materialization", "late-replacement proof failure"),
        ("Post-completion parent replacement did not preserve the exact admitted payload", "exact original-generation payload assertion"),
        ("Expected fail-closed parent-substitution rejection", "protected-window fail-closed outcome"),
        ("must-stay-inside", "admitted child payload fixture"),
    )
    for token, label in required:
        if token not in text:
            errors.append(f"safe-extract race harness missing {label}: {token}")

    outside_check = text.find("Parent substitution wrote archive bytes outside the extraction root.")
    sentinel_check = text.find("Parent substitution modified the outside sentinel.", outside_check)
    replaced = text.find("$attackerResult -contains 'REPLACED'", sentinel_check)
    original = text.find("$renamedOriginalInside = Join-Path $raceDestination 'race-original\\inside.txt'", replaced)
    payload = text.find("Post-completion parent replacement did not preserve the exact admitted payload", original)
    if not (0 <= outside_check < sentinel_check < replaced < original < payload):
        errors.append("outside escape/sentinel assertions must precede any late-replacement acceptance, which must prove the renamed original payload")

    forbidden = (
        "if ([string]::IsNullOrWhiteSpace($raceError)) { throw 'Parent directory was replaced by a junction without fail-closed extraction.' }",
        "if ($attackerResult -contains 'REPLACED') { Write-Host",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"safe-extract race harness retained unsafe/nondeterministic classification: {token}")
    return errors


def self_test() -> list[str]:
    safe = r'''outside-sentinel.txt outside-must-survive
if (x) { throw 'Parent substitution wrote archive bytes outside the extraction root.' }
if (x) { throw 'Parent substitution modified the outside sentinel.' }
if ($attackerResult -contains 'REPLACED') {
  $renamedOriginalInside = Join-Path $raceDestination 'race-original\inside.txt'
  throw 'proof that replacement occurred after protected materialization'
  throw 'Post-completion parent replacement did not preserve the exact admitted payload'
  Write-Host 'Expected fail-closed parent-substitution rejection'
}
must-stay-inside
'''
    errors: list[str] = []
    if contract_errors(safe):
        errors.append("guard rejected intended phase-aware race contract")
    mutants = {
        "drops outside escape": safe.replace("Parent substitution wrote archive bytes outside the extraction root.", "outside omitted", 1),
        "drops sentinel": safe.replace("Parent substitution modified the outside sentinel.", "sentinel omitted", 1),
        "blind replacement acceptance": safe.replace("$renamedOriginalInside = Join-Path $raceDestination 'race-original\\inside.txt'", "$renamedOriginalInside = $null", 1),
        "drops exact payload proof": safe.replace("Post-completion parent replacement did not preserve the exact admitted payload", "payload proof omitted", 1),
        "old false positive": safe + "\nif ([string]::IsNullOrWhiteSpace($raceError)) { throw 'Parent directory was replaced by a junction without fail-closed extraction.' }",
    }
    for label, mutant in mutants.items():
        if not contract_errors(mutant):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def main() -> int:
    errors = self_test()
    try:
        text = RUNTIME.read_text(encoding="utf-8")
    except OSError:
        text = None
    errors.extend(contract_errors(text))
    if errors:
        for error in errors:
            print("FAIL:", error)
        return 1
    print("PASS: V25 parent-substitution race preserves zero-byte escape while distinguishing post-completion replacement from protected-window substitution.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
