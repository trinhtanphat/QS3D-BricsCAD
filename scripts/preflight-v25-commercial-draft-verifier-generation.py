#!/usr/bin/env python3
"""Fail closed if V25 commercial draft signature verification reopens verifier code by pathname."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = (ROOT / "scripts/assert-v25-commercial-draft-identity.ps1").read_text(encoding="utf-8")

REQUIRED = (
    "$MaxVerifierScriptBytes = 262144",
    "$verifyHeld = Open-HeldGeneration -LiteralPath $verifyScript -Label 'V25 Authenticode verifier'",
    "$verifyScriptText = Read-HeldStrictUtf8 -Held $verifyHeld -MaxBytes $MaxVerifierScriptBytes -Label 'V25 Authenticode verifier'",
    "$verifyScriptBlock = [ScriptBlock]::Create($verifyScriptText)",
    "& $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint",
    "$verifyHeld.Stream.Dispose()",
)
FORBIDDEN = (
    "& $verifyScript -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint",
    "& .\\scripts\\verify-v25-signatures.ps1 -Path $extracted.ToArray()",
)


def validate(source: str) -> list[str]:
    errors: list[str] = []
    for token in REQUIRED:
        if token not in source:
            errors.append(f"V25 held verifier generation contract missing token: {token}")
    for token in FORBIDDEN:
        if token in source:
            errors.append(f"V25 draft signature admission must not execute verifier by pathname: {token}")

    open_index = source.find(REQUIRED[1])
    read_index = source.find(REQUIRED[2])
    parse_index = source.find(REQUIRED[3])
    invoke_index = source.find(REQUIRED[4])
    dispose_index = source.find(REQUIRED[5])
    if min(open_index, read_index, parse_index, invoke_index, dispose_index) >= 0:
        if not (open_index < read_index < parse_index < invoke_index < dispose_index):
            errors.append("V25 verifier must remain held from admission/read through ScriptBlock execution and dispose only afterward")
    return errors


errors = validate(VALIDATOR)
if errors:
    raise SystemExit("V25 commercial draft verifier-generation preflight failed:\n - " + "\n - ".join(errors))

for token in REQUIRED:
    mutated = VALIDATOR.replace(token, "# removed verifier-generation token", 1)
    if not validate(mutated):
        raise SystemExit(f"V25 verifier-generation mutation escaped detection: removed {token}")

mutated = VALIDATOR.replace(REQUIRED[4], FORBIDDEN[0], 1)
if not validate(mutated):
    raise SystemExit("V25 verifier-generation mutation escaped detection: pathname execution restored")

print("PASS V25 commercial draft verifier executes one held strict-UTF8 generation")
