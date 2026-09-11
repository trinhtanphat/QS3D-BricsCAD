#!/usr/bin/env python3
"""Fail closed when a zero-argument Core smoke Run() is not registered exactly once."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SMOKE_DIR = ROOT / "tests" / "QS3D.Core.SmokeTests"
REGISTRATION = SMOKE_DIR / "SmokeTestRegistration.cs"
ENTRYPOINT = SMOKE_DIR / "SmokeTestEntryPoint.cs"


class ContractError(RuntimeError):
    pass


def discover_runnables() -> list[str]:
    names: list[str] = []
    for path in sorted(SMOKE_DIR.glob("*.cs"), key=lambda p: p.name):
        if path.name in {REGISTRATION.name, ENTRYPOINT.name, "Program.cs"}:
            continue
        source = path.read_text(encoding="utf-8")
        if re.search(r"\binternal\s+static\s+void\s+Run\s*\(\s*\)", source) is None:
            continue
        name = path.stem
        if re.search(rf"\b(?:internal|public)\s+static\s+class\s+{re.escape(name)}\b", source) is None:
            raise ContractError(
                f"runnable smoke file {path.name} does not expose matching static class {name}; "
                "registration discovery would be ambiguous"
            )
        names.append(name)
    if not names:
        raise ContractError("no runnable Core smoke classes discovered")
    return names


def validate(runnables: list[str], registration_text: str, entrypoint_text: str) -> None:
    combined = registration_text + "\n" + entrypoint_text
    missing: list[str] = []
    duplicates: list[str] = []
    for name in runnables:
        count = len(re.findall(rf"\b{re.escape(name)}\s*\.\s*Run\s*\(\s*\)\s*;", combined))
        if count == 0:
            missing.append(name)
        elif count != 1:
            duplicates.append(f"{name} ({count} registrations)")
    if missing:
        raise ContractError("unregistered Core smoke Run(): " + ", ".join(missing))
    if duplicates:
        raise ContractError("duplicate Core smoke registration: " + ", ".join(duplicates))


def expect_rejected(label: str, runnables: list[str], registration_text: str, entrypoint_text: str) -> None:
    try:
        validate(runnables, registration_text, entrypoint_text)
    except ContractError:
        return
    raise ContractError(f"preflight self-test failed to reject {label}")


def main() -> int:
    if not REGISTRATION.is_file() or not ENTRYPOINT.is_file():
        raise ContractError("missing deterministic smoke registration surface")

    runnables = discover_runnables()
    registration = REGISTRATION.read_text(encoding="utf-8")
    entrypoint = ENTRYPOINT.read_text(encoding="utf-8")
    validate(runnables, registration, entrypoint)

    # Prove the contract catches both silent skip and accidental double execution.
    first = runnables[0]
    call_pattern = re.compile(rf"\b{re.escape(first)}\s*\.\s*Run\s*\(\s*\)\s*;")
    if call_pattern.search(registration):
        missing_registration = call_pattern.sub("", registration, count=1)
        missing_entrypoint = entrypoint
    elif call_pattern.search(entrypoint):
        missing_registration = registration
        missing_entrypoint = call_pattern.sub("", entrypoint, count=1)
    else:
        raise ContractError(f"self-test could not locate registered smoke call for {first}")
    expect_rejected("missing registration mutant", runnables, missing_registration, missing_entrypoint)

    duplicate_registration = registration + f"\n// mutant\n{first}.Run();\n"
    expect_rejected("duplicate registration mutant", runnables, duplicate_registration, entrypoint)

    print(f"OK: {len(runnables)} Core smoke Run() entry points are registered exactly once")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ContractError as exc:
        print("ERROR: Core smoke registration preflight failed:", exc)
        raise SystemExit(1)
