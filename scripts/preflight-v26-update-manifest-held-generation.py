#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-update-manifest.ps1"


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        raise SystemExit(f"ERROR: V26 update-manifest held-generation guard missing {label}: {token}")


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        raise SystemExit(f"ERROR: V26 update-manifest held-generation guard found forbidden {label}: {token}")


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    require(source, "$maxGeneratedScriptBytes = 1MB", "generated-script byte bound")
    require(source, "function Read-HeldStrictUtf8", "held strict UTF-8 reader")
    require(source, "function Assert-HeldGeneratedScript", "held generation revalidation")
    require(source, "$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot", "held workspace generation")
    require(source, "$generatedItem = Assert-OrdinaryPathItem -Path $tempScript", "post-generation ordinary-file admission")
    require(source, "[IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", "held generated-script open")
    require(source, "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream", "held generated-script identity")
    require(source, "$generated = Read-HeldStrictUtf8 -Stream $generatedStream", "validation from held bytes")
    forbid(source, "Get-Content -LiteralPath $tempScript -Raw", "pathname validation reopen")
    require(source, "& $tempScript @forward", "canonical-path invocation")
    require(source, "$generatedStream.Dispose()", "held stream disposal")

    exact_cleanup = "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity"
    workspace_cleanup = "Remove-HeldManifestWorkspace -Handle $workspaceHandle"
    require(source, exact_cleanup, "exact generated-script cleanup")
    require(source, workspace_cleanup, "held workspace cleanup")
    require(source, "GetFileInformationByHandle(handle", "native generation identity proof")
    require(source, "SetFileInformationByHandle(handle", "native handle deletion")
    require(source, "FILE_FLAG_OPEN_REPARSE_POINT", "reparse-open suppression")
    require(source, "FILE_ATTRIBUTE_REPARSE_POINT", "reparse rejection")
    forbid(source, "function Remove-V26ManifestTemporaryWorkspaceStrict", "legacy pathname cleanup helper")
    forbid(source, "function Remove-V26ManifestTemporaryWorkspaceBestEffort", "legacy best-effort pathname cleanup helper")
    forbid(source, "Remove-Item -LiteralPath $tempScript", "pathname temp-script cleanup")
    forbid(source, "Remove-Item -LiteralPath $tempRoot", "pathname temp-root cleanup")

    workspace_open = source.index("$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot")
    open_index = source.index("[IO.File]::Open($generatedItem.FullName", workspace_open)
    capture_index = source.index("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream", open_index)
    read_index = source.index("$generated = Read-HeldStrictUtf8 -Stream $generatedStream", capture_index)
    invoke_index = source.index("& $tempScript @forward", read_index)
    post_assert_index = source.index(
        "Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript",
        invoke_index + 1,
    )
    dispose_index = source.index("$generatedStream.Dispose()", post_assert_index)
    script_cleanup_index = source.index(exact_cleanup, dispose_index)
    workspace_cleanup_index = source.index(workspace_cleanup, script_cleanup_index)
    if not (
        workspace_open
        < open_index
        < capture_index
        < read_index
        < invoke_index
        < post_assert_index
        < dispose_index
        < script_cleanup_index
        < workspace_cleanup_index
    ):
        raise SystemExit(
            "ERROR: V26 held generation lifecycle must be workspace hold -> script hold/identity/read -> invoke/post-validate -> dispose -> exact script cleanup -> held workspace cleanup"
        )

    print("PASS V26 update-manifest held-generation guard")


if __name__ == "__main__":
    main()
