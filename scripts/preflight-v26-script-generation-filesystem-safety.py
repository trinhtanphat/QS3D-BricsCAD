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

    for token in (
        "[IO.File]::WriteAllText($outputFull",
        "[IO.File]::Replace($stagePath, $outputFull, $null)",
        "New-Item -ItemType Directory -Path $parent -Force",
        "[IO.File]::OpenHandle(",
    ):
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

    exact_cleanup = "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity"
    held_workspace_cleanup = "Remove-HeldManifestWorkspace -Handle $workspaceHandle"
    wrapper_tokens = (
        "Assert-DirectoryAncestorChain -Path $tempParent -Label 'V26 manifest temporary ancestor'",
        "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true",
        "if (Test-Path -LiteralPath $tempRoot) { throw",
        "New-Item -ItemType Directory -Path $tempRoot",
        "Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true",
        "function Open-HeldManifestWorkspace",
        "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "Assert-OrdinaryPathItem -Path $tempScript -Label 'Generated V26 update-manifest script' -Directory $false",
        "[IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
        exact_cleanup,
        held_workspace_cleanup,
        "GENERIC_READ | DELETE",
        "FILE_SHARE_READ | FILE_SHARE_WRITE",
        "FILE_FLAG_BACKUP_SEMANTICS",
        "FILE_FLAG_OPEN_REPARSE_POINT",
        "FILE_ATTRIBUTE_REPARSE_POINT",
        "GetFileInformationByHandle(handle",
        "SetFileInformationByHandle(handle",
    )
    for token in wrapper_tokens:
        if token not in wrapper:
            errors.append(f"wrapper missing workspace safety contract: {token}")

    for token in (
        "function Remove-V26ManifestTemporaryWorkspaceStrict",
        "function Remove-V26ManifestTemporaryWorkspaceBestEffort",
        "Remove-Item -LiteralPath $tempRoot",
        "Remove-Item -LiteralPath $RootPath",
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $ScriptPath",
        "FILE_SHARE_DELETE",
    ):
        if token in wrapper:
            errors.append(f"wrapper retains unsafe pathname/generation cleanup contract: {token}")

    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true",
        "New-Item -ItemType Directory -Path $tempRoot",
        "temporary parent validation",
        errors,
    )
    before(
        wrapper,
        "Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true",
        "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "workspace ordinary-file validation before held admission",
        errors,
    )
    before(
        wrapper,
        "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "[IO.File]::Open($generatedItem.FullName",
        "held workspace admission before generated-script hold",
        errors,
    )
    before(
        wrapper,
        "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
        "& $tempScript @forward",
        "generated-script identity capture before invocation",
        errors,
    )

    workspace_open = wrapper.find("$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot")
    generated_open = wrapper.find("[IO.File]::Open($generatedItem.FullName", workspace_open + 1 if workspace_open >= 0 else 0)
    identity = wrapper.find("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream", generated_open + 1 if generated_open >= 0 else 0)
    invoke = wrapper.find("& $tempScript @forward", identity + 1 if identity >= 0 else 0)
    post_assert = wrapper.find(
        "Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript",
        invoke + 1 if invoke >= 0 else 0,
    )
    dispose = wrapper.find("$generatedStream.Dispose()", post_assert + 1 if post_assert >= 0 else 0)
    script_cleanup = wrapper.find(exact_cleanup, dispose + 1 if dispose >= 0 else 0)
    workspace_cleanup = wrapper.find(held_workspace_cleanup, script_cleanup + 1 if script_cleanup >= 0 else 0)
    workspace_dispose = wrapper.find("$workspaceHandle.Dispose()", workspace_cleanup + 1 if workspace_cleanup >= 0 else 0)
    if min(workspace_open, generated_open, identity, invoke, post_assert, dispose, script_cleanup, workspace_cleanup, workspace_dispose) < 0:
        errors.append("wrapper held-generation lifecycle is incomplete")
    elif not (
        workspace_open
        < generated_open
        < identity
        < invoke
        < post_assert
        < dispose
        < script_cleanup
        < workspace_cleanup
        < workspace_dispose
    ):
        errors.append(
            "wrapper lifecycle is not workspace hold -> script hold/identity -> invoke/post-validate -> dispose -> exact script cleanup -> held workspace cleanup -> handle dispose"
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
        "held workspace admission": (
            generator,
            wrapper.replace(
                "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
                "$workspaceHandle = $null",
                1,
            ),
        ),
        "exact generated-script cleanup": (
            generator,
            wrapper.replace(
                "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity",
                "Remove-Item -LiteralPath $tempScript -Force",
            ),
        ),
        "held workspace cleanup": (
            generator,
            wrapper.replace("Remove-HeldManifestWorkspace -Handle $workspaceHandle", "# held workspace cleanup removed"),
        ),
        "reparse-open suppression": (
            generator,
            wrapper.replace("FILE_FLAG_OPEN_REPARSE_POINT", "0"),
        ),
    }
    for label, (mutated_generator, mutated_wrapper) in mutations.items():
        if not validate(mutated_generator, mutated_wrapper):
            raise SystemExit(f"mutation probe was not rejected: {label}")

    print("PASS V26 script-generation filesystem/output safety")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
