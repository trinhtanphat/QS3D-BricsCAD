#!/usr/bin/env python3
"""Fail closed unless V26 update-manifest temp cleanup is generation-owned."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def fail(message: str) -> None:
    print(f"ERROR: V26 manifest temp-generation cleanup preflight failed closed: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    try:
        source = TARGET.read_text(encoding="utf-8")
    except Exception as exc:
        fail(f"cannot read {TARGET.relative_to(ROOT)}: {exc}")

    required = {
        "generated-script identity capture": "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream",
        "exact script cleanup": "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity",
        "held workspace acquisition": "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot",
        "held workspace deletion": "Remove-HeldManifestWorkspace -Handle $workspaceHandle",
        "native identity proof": "GetFileInformationByHandle(handle",
        "native delete disposition": "SetFileInformationByHandle(handle",
        "delete access": "GENERIC_READ | DELETE",
        "directory share closes delete/rename": "FILE_SHARE_READ | FILE_SHARE_WRITE",
        "directory backup semantics": "FILE_FLAG_BACKUP_SEMANTICS",
        "reparse-open suppression": "FILE_FLAG_OPEN_REPARSE_POINT",
        "reparse attribute rejection": "FILE_ATTRIBUTE_REPARSE_POINT",
    }
    for label, token in required.items():
        if token not in source:
            fail(f"missing {label}: {token}")

    forbidden = (
        "Remove-Item -LiteralPath $ScriptPath",
        "Remove-Item -LiteralPath $RootPath",
        "Remove-Item -LiteralPath $tempScript",
        "Remove-Item -LiteralPath $tempRoot",
        "FILE_SHARE_DELETE",
    )
    for token in forbidden:
        if token in source:
            fail(f"unsafe temp-generation cleanup primitive remains: {token}")

    workspace_open = source.find("$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot")
    generated_open = source.find("$generatedStream = [IO.File]::Open(")
    capture = source.find("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream")
    invoke = source.find("& $tempScript @forward")
    post_assert = source.find("Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript", invoke + 1)
    dispose = source.find("$generatedStream.Dispose()", post_assert + 1)
    script_cleanup = source.find("Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity", dispose + 1)
    workspace_cleanup = source.find("Remove-HeldManifestWorkspace -Handle $workspaceHandle", script_cleanup + 1)
    if min(workspace_open, generated_open, capture, invoke, post_assert, dispose, script_cleanup, workspace_cleanup) < 0:
        fail("could not prove held-workspace/script identity lifecycle")
    if not (workspace_open < generated_open < capture < invoke < post_assert < dispose < script_cleanup < workspace_cleanup):
        fail("workspace must stay held while exact script generation is executed, validated, and safely deleted")

    print("PASS: V26 update-manifest script/workspace cleanup is exact non-reparse generation owned.")


if __name__ == "__main__":
    main()
