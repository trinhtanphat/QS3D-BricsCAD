#!/usr/bin/env python3
"""Guard the graph-wide held-generation contract for generated V26 manifest dependencies."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def validate(source: str) -> list[str]:
    errors: list[str] = []
    required = (
        "$maxGeneratedScriptBytes = 2MB",
        "function Read-HeldStrictUtf8",
        "function Assert-HeldGeneratedTemplate",
        "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "$heldGenerations = [Collections.Generic.List[object]]::new()",
        "$admission = & $generator -SourceScript $entry.Source -OutputPath $entry.Output -PassThruHeldGeneration",
        "Assert-HeldGeneratedTemplate -Admission $admission -ExpectedPath $entry.Output -Label $entry.Label",
        "$generatedText = Read-HeldStrictUtf8 -Stream $admission.Stream",
        "$heldGenerations.Add([pscustomobject]@{ Admission = $admission; Path = $entry.Output; Label = $entry.Label })",
        "& $tempScript @forward",
        "$identity = [string]$held.Admission.Identity",
        "$held.Admission.Stream.Dispose()",
        "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
        "Remove-HeldManifestWorkspace -Handle $workspaceHandle",
    )
    for token in required:
        if token not in source:
            errors.append(f"missing graph held-generation contract: {token}")
    for token in (
        "Get-Content -LiteralPath $tempScript -Raw",
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $tempRoot",
        "FILE_SHARE_DELETE",
    ):
        if token in source:
            errors.append(f"forbidden pathname/reopen contract remains: {token}")
    hold = source.find("$heldGenerations.Add(")
    invoke = source.find("& $tempScript @forward", hold)
    post = source.find("foreach ($held in $heldGenerations)", invoke + 1)
    cleanup_loop = source.find("for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--)", post)
    dispose = source.find("$held.Admission.Stream.Dispose()", cleanup_loop)
    cleanup = source.find("Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity", dispose)
    if min(hold, invoke, post, cleanup_loop, dispose, cleanup) < 0 or not (hold < invoke < post < cleanup_loop < dispose < cleanup):
        errors.append("dependencies must stay held through invocation/post-validation before reverse exact-generation cleanup")
    return errors


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        raise SystemExit("ERROR: V26 update-manifest held-generation guard: " + "; ".join(errors))
    for label, token in {
        "held admission": "$heldGenerations.Add(",
        "post-validation": "foreach ($held in $heldGenerations)",
        "exact cleanup": "Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity",
    }.items():
        if not validate(source.replace(token, "# removed")):
            raise SystemExit(f"ERROR: mutation probe was not rejected: {label}")
    print("PASS V26 update-manifest holds wrapper/core/native generations through execution and exact cleanup")


if __name__ == "__main__":
    main()
