#!/usr/bin/env python3
"""Guard the V26 MSI administrative-extraction command-line contract."""

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v26-compile-references.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    match = re.search(
        r"\$arguments\s*=\s*@\((?P<body>.*?)\)\s*\n\s*Write-Host 'Starting BricsCAD V26 MSI administrative extraction",
        text,
        flags=re.DOTALL,
    )
    if not match:
        return ["could not locate the bounded V26 MSI administrative extraction argument list"]

    body = match.group("body")
    required = (
        "'/a'",
        "'/qn'",
        "('TARGETDIR=\"' + $extract + '\"')",
        "'REBOOT=ReallySuppress'",
        "'/L*v'",
        "$msiLog",
    )
    for token in required:
        if token not in body:
            errors.append(f"V26 MSI admin argument list missing required token: {token}")

    if re.search(r"['\"]?/norestart['\"]?", body, flags=re.IGNORECASE):
        errors.append("V26 MSI administrative extraction must not combine /a with /norestart; REBOOT=ReallySuppress owns restart suppression")

    if "Start-Process -FilePath msiexec.exe -ArgumentList $arguments -PassThru" not in text:
        errors.append("V26 MSI extraction no longer consumes the guarded argument list")

    return errors


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("\n".join(errors))

    mutated = text.replace("'/qn',", "'/qn', '/norestart',", 1)
    if not validate(mutated):
        raise SystemExit("mutation probe was not rejected: /norestart reintroduced into /a extraction")

    print("PASS: V26 MSI administrative extraction uses /a-compatible restart suppression and preserves bounded logging/target arguments.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
