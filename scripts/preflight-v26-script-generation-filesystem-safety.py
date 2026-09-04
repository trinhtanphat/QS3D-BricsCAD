#!/usr/bin/env python3
"""Fail closed if V26 script generation loses filesystem/output safety."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"
WRAPPER = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def before(text: str, first: str, second: str, label: str, errors: list[str]) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        errors.append(f"{label}: expected {first!r} before {second!r}")


def validate(generator: str, wrapper: str) -> list[str]:
    errors: list[str] = []

    generator_tokens = (
        "function Assert-OrdinaryPathItem",
        "function Assert-DirectoryAncestorChain",
        "Assert-OrdinaryPathItem -Path $sourceFull -Label 'V25 template script' -Directory $false",
        "Assert-DirectoryAncestorChain -Path $parent -Label 'V26 output ancestor'",
        "Assert-SafeExistingOutputLeaf -Path $outputFull",
        "$stagePath = Join-Path $parent",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "[IO.File]::Replace($stagePath, $outputFull, $null)",
        "[IO.File]::Move($stagePath, $outputFull)",
        "Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false",
    )
    for token in generator_tokens:
        if token not in generator:
            errors.append(f"generator missing safety contract: {token}")

    if "[IO.File]::WriteAllText($outputFull" in generator:
        errors.append("generator must not write generated content directly into the final output leaf")

    before(
        generator,
        "Assert-OrdinaryPathItem -Path $sourceFull -Label 'V25 template script' -Directory $false",
        "[IO.File]::Open($sourceFull",
        "source ordinary-file validation before admitted handle open",
        errors,
    )
    before(
        generator,
        "Assert-DirectoryAncestorChain -Path $parent -Label 'V26 output ancestor'",
        "New-Item -ItemType Directory -Path $parent -Force",
        "output ancestor validation before creation",
        errors,
    )
    before(
        generator,
        "Assert-SafeExistingOutputLeaf -Path $outputFull",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "existing output validation before staging",
        errors,
    )
    before(
        generator,
        "Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false",
        "[IO.File]::Replace($stagePath, $outputFull, $null)",
        "staging validation before publication",
        errors,
    )

    wrapper_tokens = (
        "Assert-DirectoryAncestorChain -Path $tempParent -Label 'V26 manifest temporary ancestor'",
        "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true",
        "if (Test-Path -LiteralPath $tempRoot) { throw",
        "New-Item -ItemType Directory -Path $tempRoot",
        "Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true",
        "Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false",
        "$residue = @(Get-ChildItem -LiteralPath $tempRoot -Force)",
        "refusing recursive cleanup",
        "Remove-Item -LiteralPath $tempScript -Force",
        "Remove-Item -LiteralPath $tempRoot -Force",
    )
    for token in wrapper_tokens:
        if token not in wrapper:
            errors.append(f"wrapper missing workspace safety contract: {token}")

    if "Remove-Item -LiteralPath $tempRoot -Recurse" in wrapper:
        errors.append("wrapper must not recursively delete its temporary root")

    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true",
        "New-Item -ItemType Directory -Path $tempRoot",
        "temporary parent validation",
        errors,
    )
    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false",
        "Remove-Item -LiteralPath $tempScript -Force",
        "temporary script validation before cleanup",
        errors,
    )
    before(
        wrapper,
        "$residue = @(Get-ChildItem -LiteralPath $tempRoot -Force)",
        "Remove-Item -LiteralPath $tempRoot -Force",
        "empty-workspace proof before root cleanup",
        errors,
    )

    return errors


def main() -> int:
    generator = GENERATOR.read_text(encoding="utf-8")
    wrapper = WRAPPER.read_text(encoding="utf-8")
    errors = validate(generator, wrapper)
    if errors:
        raise SystemExit("\n".join(errors))

    mutations = {
        "source ordinary-file guard": (
            generator.replace(
                "Assert-OrdinaryPathItem -Path $sourceFull -Label 'V25 template script' -Directory $false | Out-Null\n",
                "",
                1,
            ),
            wrapper,
        ),
        "atomic final publication": (
            generator.replace("[IO.File]::Replace($stagePath, $outputFull, $null)", "Move-Item -LiteralPath $stagePath -Destination $outputFull -Force", 1),
            wrapper,
        ),
        "temporary parent validation": (
            generator,
            wrapper.replace(
                "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true | Out-Null\n",
                "",
                1,
            ),
        ),
        "non-recursive temporary cleanup": (
            generator,
            wrapper.replace("Remove-Item -LiteralPath $tempRoot -Force", "Remove-Item -LiteralPath $tempRoot -Recurse -Force", 1),
        ),
    }
    for label, (mutated_generator, mutated_wrapper) in mutations.items():
        if not validate(mutated_generator, mutated_wrapper):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 script-generation filesystem/output safety")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
