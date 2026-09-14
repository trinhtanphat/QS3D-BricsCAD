#!/usr/bin/env python3
"""Fail closed if V26 generation loses parent/path safety or graph-wide held-generation ownership."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GENERATOR = ROOT / "scripts" / "new-v26-script-from-v25.ps1"
WRAPPER = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def validate(generator: str, wrapper: str) -> list[str]:
    errors: list[str] = []
    generator_required = (
        "function Assert-OrdinaryPathItem",
        "function Assert-DirectoryAncestorChain",
        "function Open-AdmittedOutputParent",
        "$outputParentHandle = Open-AdmittedOutputParent -Path $parent",
        "Assert-AdmittedOutputParentBinding -Admission $outputParentHandle",
        "$pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path",
        "Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information",
        "[IO.File]::WriteAllText($stagePath, $generated",
        "[IO.File]::Move($stagePath, $outputFull)",
        "V26 generation output must be fresh",
    )
    for token in generator_required:
        if token not in generator:
            errors.append(f"generator missing filesystem contract: {token}")
    for token in ("[IO.File]::WriteAllText($outputFull", "Move-Item -LiteralPath $stagePath -Destination $outputFull -Force"):
        if token in generator:
            errors.append(f"generator retains unsafe publication primitive: {token}")

    wrapper_required = (
        "Assert-DirectoryAncestorChain -Path $tempParent -Label 'V26 manifest temporary ancestor'",
        "Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true",
        "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "$generationPlan = @(",
        "$admission = & $generator -SourceScript $entry.Source -OutputPath $entry.Output -PassThruHeldGeneration",
        "Assert-HeldGeneratedTemplate -Admission $admission -ExpectedPath $entry.Output -Label $entry.Label",
        "$generatedText = Read-HeldStrictUtf8 -Stream $admission.Stream",
        "$heldGenerations.Add([pscustomobject]@{ Admission = $admission; Path = $entry.Output; Label = $entry.Label })",
        "Assert-HeldGeneratedTemplate -Admission $held.Admission -ExpectedPath $held.Path -Label $held.Label",
        "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
        "Remove-HeldManifestWorkspace -Handle $workspaceHandle",
        "FILE_FLAG_OPEN_REPARSE_POINT",
        "FILE_ATTRIBUTE_REPARSE_POINT",
    )
    for token in wrapper_required:
        if token not in wrapper:
            errors.append(f"wrapper missing graph filesystem contract: {token}")
    for token in (
        "Remove-Item -LiteralPath $tempRoot",
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $held.Path",
        "FILE_SHARE_DELETE",
    ):
        if token in wrapper:
            errors.append(f"wrapper retains unsafe pathname/delete-share contract: {token}")

    workspace = wrapper.find("$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot")
    plan = wrapper.find("$generationPlan = @(", workspace)
    add = wrapper.find("$heldGenerations.Add(", plan)
    invoke = wrapper.find("& $tempScript @forward", add)
    post = wrapper.find("foreach ($held in $heldGenerations)", invoke + 1)
    cleanup_loop = wrapper.find("for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)", post)
    cleanup = wrapper.find("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", cleanup_loop)
    workspace_cleanup = wrapper.find("Remove-HeldManifestWorkspace -Handle $workspaceHandle", cleanup)
    if min(workspace, plan, add, invoke, post, cleanup_loop, cleanup, workspace_cleanup) < 0 or not (
        workspace < plan < add < invoke < post < cleanup_loop < cleanup < workspace_cleanup
    ):
        errors.append("wrapper lifecycle must hold workspace and all generated dependencies through execution/post-validation before exact cleanup")
    return errors


def main() -> int:
    generator = GENERATOR.read_text(encoding="utf-8")
    wrapper = WRAPPER.read_text(encoding="utf-8")
    errors = validate(generator, wrapper)
    if errors:
        raise SystemExit("\n".join(errors))
    probes = {
        "held parent binding": (generator.replace("Assert-AdmittedOutputParentBinding -Admission $outputParentHandle", "# removed"), wrapper),
        "fresh publication": (generator.replace("V26 generation output must be fresh", "existing output accepted"), wrapper),
        "graph admission": (generator, wrapper.replace("$heldGenerations.Add(", "# removed(", 1)),
        "graph exact cleanup": (generator, wrapper.replace("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", "# removed", 1)),
        "workspace cleanup": (generator, wrapper.replace("Remove-HeldManifestWorkspace -Handle $workspaceHandle", "# removed", 1)),
    }
    for label, (mutated_generator, mutated_wrapper) in probes.items():
        if not validate(mutated_generator, mutated_wrapper):
            raise SystemExit(f"mutation probe was not rejected: {label}")
    print("PASS V26 generation preserves held-parent publication and graph-wide filesystem/exact-generation safety")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
