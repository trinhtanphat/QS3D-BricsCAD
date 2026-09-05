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

    # The generated finalizer must be admitted as one ordinary generation and
    # held against write/delete replacement while validation and execution use
    # its pathname to preserve the canonical scripts-directory $PSScriptRoot.
    require(source, "function Resolve-OrdinaryNonReparseFile", "ordinary generated-script admission")
    require(source, "function Assert-NoReparseDirectoryChain", "ancestor reparse rejection")
    require(source, "$generatedItem = Resolve-OrdinaryNonReparseFile -Path $tempScript", "post-generation leaf admission")
    require(source, "[IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", "held generated-script open")
    require(source, "$generatedStream", "held generated-script stream")
    require(source, "Read-HeldStrictUtf8", "bounded strict UTF-8 held read")
    require(source, "$maxGeneratedScriptBytes = 1MB", "generated-script byte bound")
    require(source, "[Text.UTF8Encoding]::new($false, $true)", "strict generated-script UTF-8 decoder")
    require(source, "$generated = Read-HeldStrictUtf8 -Stream $generatedStream", "validation from held generation")
    forbid(source, "Get-Content -LiteralPath $tempScript -Raw", "pathname validation reopen")

    # The same handle must remain alive across invocation, and path metadata is
    # checked on both sides so an unexpected name/generation change fails closed.
    require(source, "Assert-HeldGeneratedScript -Stream $generatedStream", "held generation revalidation")
    require(source, "& $tempScript @forward", "canonical-path invocation")
    require(source, "$generatedStream.Dispose()", "held stream disposal")
    open_index = source.index("[IO.File]::Open($generatedItem.FullName")
    read_index = source.index("$generated = Read-HeldStrictUtf8 -Stream $generatedStream")
    invoke_index = source.index("& $tempScript @forward")
    dispose_index = source.index("$generatedStream.Dispose()", invoke_index)
    if not open_index < read_index < invoke_index < dispose_index:
        raise SystemExit("ERROR: V26 finalizer held generation ordering is not open -> held read -> invoke -> dispose")

    # Cleanup has two simultaneous invariants. On success, re-admit the transient
    # leaf, perform the repository-root-compatible unlink, and prove absence so
    # SilentlyContinue cannot hide a release-safety failure. On an already-failed
    # operation, cleanup is secondary and must not replace the primary evidence.
    require(source, "$primaryFailure = $null", "primary failure sentinel")
    require(source, "$primaryFailure = $_", "primary failure capture")
    success_cleanup = "if ($null -eq $primaryFailure) {"
    require(source, success_cleanup, "successful-path cleanup branch")
    require(source, "Resolve-OrdinaryNonReparseFile -Path $tempScript -Label 'Generated V26 finalizer cleanup script'", "successful-path cleanup leaf revalidation")
    cleanup = "if (Test-Path -LiteralPath $tempScript) { Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue }"
    require(source, cleanup, "repository-root-compatible temp-script cleanup")
    require(source, "if (Test-Path -LiteralPath $tempScript) { throw 'Generated V26 finalizer cleanup did not remove the admitted transient script.' }", "successful-path cleanup absence proof")
    require(source, "else {", "primary-failure cleanup branch")
    require(source, "Preserve the primary transformer/finalizer failure", "primary failure preservation rationale")
    forbid(source, "Remove-Item -LiteralPath $tempScript -Force -ErrorAction Stop", "primary-error-masking temp-script cleanup")

    success_index = source.index(success_cleanup, dispose_index)
    admission_index = source.index("Resolve-OrdinaryNonReparseFile -Path $tempScript -Label 'Generated V26 finalizer cleanup script'", success_index)
    cleanup_index = source.index(cleanup, admission_index)
    absence_index = source.index("Generated V26 finalizer cleanup did not remove the admitted transient script.", cleanup_index)
    if not invoke_index < dispose_index < success_index < admission_index < cleanup_index < absence_index:
        raise SystemExit("ERROR: V26 success cleanup must be invoke -> dispose -> strict admission -> unlink -> absence proof")

    print("PASS V26 generated finalizer held-generation guard")


if __name__ == "__main__":
    main()
