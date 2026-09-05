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
        "function Open-AdmittedOutputParent",
        "function Assert-AdmittedOutputParentBinding",
        "$outputParentHandle = Open-AdmittedOutputParent -Path $parent",
        "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
        "Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information",
        "$pathAdmission.Handle.Dispose()",
        "V26 generation output must be fresh",
        "$stagePath = Join-Path $parent",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "[IO.File]::Move($stagePath, $outputFull)",
        "Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false",
        "$outputParentHandle.Handle.Dispose()",
    )
    for token in generator_tokens:
        if token not in generator:
            errors.append(f"generator missing safety contract: {token}")

    forbidden = (
        "[IO.File]::WriteAllText($outputFull",
        "[IO.File]::Replace($stagePath, $outputFull, $null)",
        "New-Item -ItemType Directory -Path $parent -Force",
        "[IO.File]::OpenHandle(",
    )
    for token in forbidden:
        if token in generator:
            errors.append(f"generator retains unsafe output contract: {token}")

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
        "$outputParentHandle = Open-AdmittedOutputParent -Path $parent",
        "output ancestor validation before held-parent admission",
        errors,
    )
    before(
        generator,
        "$outputParentHandle = Open-AdmittedOutputParent -Path $parent",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "held-parent admission before staging",
        errors,
    )
    before(
        generator,
        "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
        "Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information",
        "native pathname re-admission before held-generation identity comparison",
        errors,
    )
    before(
        generator,
        "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
        "[IO.File]::Move($stagePath, $outputFull)",
        "held-parent revalidation before publication",
        errors,
    )
    before(
        generator,
        "Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false",
        "[IO.File]::Move($stagePath, $outputFull)",
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
        "function Remove-V26ManifestTemporaryWorkspaceStrict",
        "function Remove-V26ManifestTemporaryWorkspaceBestEffort",
        "Assert-OrdinaryPathItem -Path $ScriptPath -Label 'Generated V26 update-manifest script' -Directory $false",
        "Assert-OrdinaryPathItem -Path $RootPath -Label 'V26 manifest temporary workspace' -Directory $true",
        "$residue = @(Get-ChildItem -LiteralPath $RootPath -Force)",
        "refusing recursive cleanup",
        "Remove-Item -LiteralPath $ScriptPath -Force",
        "Remove-Item -LiteralPath $RootPath -Force",
        "Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot",
        "Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot",
    )
    for token in wrapper_tokens:
        if token not in wrapper:
            errors.append(f"wrapper missing workspace safety contract: {token}")

    for token in (
        "Remove-Item -LiteralPath $tempRoot -Recurse",
        "Remove-Item -LiteralPath $RootPath -Recurse",
        "Remove-Item -LiteralPath $tempScript -Recurse",
        "Remove-Item -LiteralPath $ScriptPath -Recurse",
    ):
        if token in wrapper:
            errors.append(f"wrapper must not recursively delete temporary content: {token}")

    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true",
        "New-Item -ItemType Directory -Path $tempRoot",
        "temporary parent validation",
        errors,
    )
    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $ScriptPath -Label 'Generated V26 update-manifest script' -Directory $false",
        "Remove-Item -LiteralPath $ScriptPath -Force",
        "strict temporary script validation before cleanup",
        errors,
    )
    before(
        wrapper,
        "$residue = @(Get-ChildItem -LiteralPath $RootPath -Force)",
        "Remove-Item -LiteralPath $RootPath -Force",
        "strict empty-workspace proof before root cleanup",
        errors,
    )
    before(
        wrapper,
        "if ($null -eq $primaryFailure)",
        "Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot",
        "strict cleanup selected on successful generation",
        errors,
    )
    before(
        wrapper,
        "Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot",
        "Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot",
        "best-effort cleanup reserved for primary-failure branch",
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
        "held parent binding": (
            generator.replace("Assert-AdmittedOutputParentBinding -Admission $outputParentHandle", "# binding removed"),
            wrapper,
        ),
        "native pathname re-admission": (
            generator.replace(
                "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
                "$pathAdmission = $Admission",
                1,
            ),
            wrapper,
        ),
        "fresh-only publication": (
            generator.replace("V26 generation output must be fresh", "V26 existing output may be replaced"),
            wrapper,
        ),
        "atomic final publication": (
            generator.replace("[IO.File]::Move($stagePath, $outputFull)", "Move-Item -LiteralPath $stagePath -Destination $outputFull -Force", 1),
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
        "strict temporary script validation": (
            generator,
            wrapper.replace(
                "Assert-OrdinaryPathItem -Path $ScriptPath -Label 'Generated V26 update-manifest script' -Directory $false | Out-Null\n",
                "",
                1,
            ),
        ),
        "non-recursive temporary cleanup": (
            generator,
            wrapper.replace(
                "Remove-Item -LiteralPath $RootPath -Force",
                "Remove-Item -LiteralPath $RootPath -Recurse -Force",
                1,
            ),
        ),
        "strict success cleanup dispatch": (
            generator,
            wrapper.replace(
                "Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot",
                "Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot",
                1,
            ),
        ),
    }
    for label, (mutated_generator, mutated_wrapper) in mutations.items():
        if not validate(mutated_generator, mutated_wrapper):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 script-generation filesystem/output safety")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
