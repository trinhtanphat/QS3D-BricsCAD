#!/usr/bin/env python3
"""Fail closed if V26 generated-script publication is not generation-bound."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"


def before(text: str, first: str, second: str, label: str, errors: list[str]) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        errors.append(f"{label}: expected {first!r} before {second!r}")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "CreateFile",
        "FILE_FLAG_BACKUP_SEMANTICS",
        "function Open-AdmittedOutputParent",
        "function Assert-AdmittedOutputParentBinding",
        "V26 output parent held generation",
        "V26 generation output must be fresh",
        "$outputParentHandle = Open-AdmittedOutputParent",
        "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
        "Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information",
        "$pathAdmission.Handle.Dispose()",
        "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "[IO.File]::Move($stagePath, $outputFull)",
        "$outputParentHandle.Handle.Dispose()",
    )
    for token in required:
        if token not in text:
            errors.append(f"generator missing output-generation contract: {token}")

    forbidden = (
        "[IO.File]::Replace($stagePath, $outputFull, $null)",
        "New-Item -ItemType Directory -Path $parent -Force",
        "[IO.File]::OpenHandle(",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"generator retains generation-unsafe publication token: {token}")

    before(
        text,
        "$outputParentHandle = Open-AdmittedOutputParent",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "parent handle admission before staging",
        errors,
    )
    before(
        text,
        "V26 generation output must be fresh",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "fresh destination refusal before staging",
        errors,
    )
    before(
        text,
        "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
        "Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information",
        "native pathname re-admission before identity comparison",
        errors,
    )
    before(
        text,
        "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
        "[IO.File]::Move($stagePath, $outputFull)",
        "held parent binding before publication",
        errors,
    )
    return errors


def main() -> int:
    text = GENERATOR.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("\n".join(errors))

    probes = {
        "parent handle admission": text.replace("$outputParentHandle = Open-AdmittedOutputParent", "$outputParentHandle = $null", 1),
        "native pathname re-admission": text.replace(
            "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
            "$pathAdmission = $Admission",
            1,
        ),
        "fresh destination refusal": text.replace("V26 generation output must be fresh", "V26 output may be reused"),
        "binding before publish": text.replace("Assert-AdmittedOutputParentBinding -Admission $outputParentHandle", "# binding removed"),
        "atomic move": text.replace("[IO.File]::Move($stagePath, $outputFull)", "Move-Item -LiteralPath $stagePath -Destination $outputFull -Force", 1),
    }
    for label, mutated in probes.items():
        if not validate(mutated):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 generated-script output generation binding")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
