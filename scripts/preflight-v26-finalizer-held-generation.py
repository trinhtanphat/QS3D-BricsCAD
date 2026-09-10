#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "finalize-v26-signed-package.ps1"


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        raise SystemExit(f"ERROR: V26 finalizer held-generation guard missing {label}: {token}")


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        raise SystemExit(f"ERROR: V26 finalizer held-generation guard found forbidden {label}: {token}")


def main() -> None:
    source = TARGET.read_text(encoding="utf-8")

    # The generated finalizer is admitted as one ordinary generation and held
    # against write/delete replacement while validation and execution use its
    # canonical scripts-directory pathname to preserve inherited $PSScriptRoot.
    require(source, "function Resolve-OrdinaryNonReparseFile", "ordinary generated-script admission")
    require(source, "function Assert-NoReparseDirectoryChain", "ancestor reparse rejection")
    require(source, "$generatedItem = Resolve-OrdinaryNonReparseFile -Path $tempScript", "post-generation leaf admission")
    require(source, "[IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", "held generated-script open")
    require(source, "$maxGeneratedScriptBytes = 1MB", "generated-script byte bound")
    require(source, "[Text.UTF8Encoding]::new($false, $true)", "strict generated-script UTF-8 decoder")
    require(source, "$generated = Read-HeldStrictUtf8 -Stream $generatedStream", "validation from held generation")
    require(source, "$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream", "held generation identity capture")
    forbid(source, "Get-Content -LiteralPath $tempScript -Raw", "pathname validation reopen")

    # Execution and post-validation stay on the held generation. Cleanup then
    # reopens the name without following a reparse point, proves the same native
    # file identity, and marks exactly that generation for deletion by handle.
    require(source, "Assert-HeldGeneratedScript -Stream $generatedStream", "held generation revalidation")
    require(source, "& $tempScript @forward", "canonical-path invocation")
    require(source, "$generatedStream.Dispose()", "held stream disposal")
    require(source, "function Remove-ExactGeneratedScriptGeneration", "exact-generation cleanup helper")
    require(source, "DeleteIfSameGeneration", "native exact-generation cleanup")
    require(source, "GENERIC_READ | DELETE", "delete-capable cleanup handle")
    require(source, "FILE_SHARE_READ", "write/delete sharing remains closed")
    require(source, "FILE_FLAG_OPEN_REPARSE_POINT", "cleanup reparse-open suppression")
    require(source, "FILE_ATTRIBUTE_REPARSE_POINT", "cleanup reparse rejection")
    require(source, "GetFileInformationByHandle(handle", "same-handle identity proof")
    require(source, "SetFileInformationByHandle(handle", "same-handle delete disposition")
    cleanup = "Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity"
    require(source, cleanup, "exact-generation cleanup call")
    forbid(source, "Remove-Item -LiteralPath $tempScript", "pathname temp-script cleanup")
    forbid(source, "Remove-Item $tempScript", "unqualified pathname temp-script cleanup")

    open_index = source.index("[IO.File]::Open($generatedItem.FullName")
    capture_index = source.index("$generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream", open_index)
    read_index = source.index("$generated = Read-HeldStrictUtf8 -Stream $generatedStream", capture_index)
    invoke_index = source.index("& $tempScript @forward", read_index)
    post_assert_index = source.index(
        "Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript",
        invoke_index + 1,
    )
    dispose_index = source.index("$generatedStream.Dispose()", post_assert_index)
    cleanup_index = source.index(cleanup, dispose_index)
    if not open_index < capture_index < read_index < invoke_index < post_assert_index < dispose_index < cleanup_index:
        raise SystemExit(
            "ERROR: V26 finalizer held generation ordering must be open -> identity -> held read -> invoke -> post-validate -> dispose -> exact cleanup"
        )

    # Success cleanup remains fail-closed. When a transformer/finalizer failure
    # is already propagating, cleanup is secondary and must never replace it.
    require(source, "$primaryFailure = $null", "primary failure sentinel")
    require(source, "$primaryFailure = $_", "primary failure capture")
    success_branch = "if ($null -eq $primaryFailure) {"
    require(source, success_branch, "successful-path cleanup branch")
    require(source, "Preserve the primary transformer/finalizer failure", "primary failure preservation rationale")

    success_index = source.index(success_branch, dispose_index)
    success_cleanup_index = source.index(cleanup, success_index)
    primary_else_index = source.index("else {", success_cleanup_index)
    secondary_cleanup_index = source.index(cleanup, primary_else_index)
    if not dispose_index < success_index < success_cleanup_index < primary_else_index < secondary_cleanup_index:
        raise SystemExit(
            "ERROR: V26 exact-generation cleanup must be strict on success and secondary after a primary failure"
        )

    print("PASS V26 generated finalizer held-generation guard")


if __name__ == "__main__":
    main()
