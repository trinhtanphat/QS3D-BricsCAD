#!/usr/bin/env python3
"""Guard primary-failure preservation across graph-wide V26 generated dependency cleanup."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "$primaryFailure = $null",
        "catch {\n    $primaryFailure = $_\n    throw\n}",
        "$heldGenerations = [Collections.Generic.List[object]]::new()",
        "for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)",
        "$identity = [string]$held.Admission.Identity",
        "$held.Admission.Stream.Dispose()",
        "$held.Admission.Stream = $null",
        "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
        "Secondary V26 manifest exact-generation cleanup failed while preserving the primary failure",
        "Remove-HeldManifestWorkspace -Handle $workspaceHandle",
        "Secondary V26 manifest held-workspace cleanup failed while preserving the primary failure",
        "if ($null -eq $primaryFailure) { throw }",
    )
    for token in required:
        if token not in text:
            errors.append(f"missing cleanup contract: {token}")
    if text.count("if ($null -eq $primaryFailure) { throw }") < 2:
        errors.append("dependency and workspace cleanup must both be strict when no primary failure exists")
    for token in (
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $tempRoot",
        "Remove-Item -LiteralPath $held.Path",
    ):
        if token in text:
            errors.append(f"pathname cleanup fallback remains: {token}")
    finally_index = text.find("finally {")
    reverse_loop = text.find("for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)", finally_index)
    dispose = text.find("$held.Admission.Stream.Dispose()", reverse_loop)
    exact = text.find("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", dispose)
    workspace = text.find("Remove-HeldManifestWorkspace -Handle $workspaceHandle", exact)
    workspace_dispose = text.find("$workspaceHandle.Dispose()", workspace)
    if min(finally_index, reverse_loop, dispose, exact, workspace, workspace_dispose) < 0 or not (
        finally_index < reverse_loop < dispose < exact < workspace < workspace_dispose
    ):
        errors.append("cleanup ordering must be reverse graph stream disposal -> exact generation cleanup -> workspace cleanup -> handle disposal")
    return errors


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    errors = validate(text)
    if errors:
        raise SystemExit("V26 cleanup primary-failure guard: " + "; ".join(errors))
    for label, token in {
        "primary capture": "$primaryFailure = $_",
        "reverse cleanup": "for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)",
        "exact cleanup": "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
    }.items():
        if not validate(text.replace(token, "# removed", 1)):
            raise SystemExit(f"mutation probe was not rejected: {label}")
    print("PASS V26 graph cleanup preserves the primary failure and stays strict on success")


if __name__ == "__main__":
    main()
