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
    require(source, "$generatedItem = Assert-OrdinaryPathItem -Path $tempScript", "post-generation ordinary-file admission")
    require(source, "[IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", "held generated-script open")
    require(source, "$generated = Read-HeldStrictUtf8 -Stream $generatedStream", "validation from held bytes")
    forbid(source, "Get-Content -LiteralPath $tempScript -Raw", "pathname validation reopen")
    require(source, "& $tempScript @forward", "canonical-path invocation")
    require(source, "$generatedStream.Dispose()", "held stream disposal")

    open_index = source.index("[IO.File]::Open($generatedItem.FullName")
    read_index = source.index("$generated = Read-HeldStrictUtf8 -Stream $generatedStream")
    invoke_index = source.index("& $tempScript @forward")
    dispose_index = source.index("$generatedStream.Dispose()", invoke_index)
    if not open_index < read_index < invoke_index < dispose_index:
        raise SystemExit("ERROR: V26 update-manifest held generation ordering must be open -> held read -> invoke -> dispose")

    # Preserve residue-safe cleanup after the cleanup implementation moved behind
    # strict/best-effort helper parameters. The strict success path remains fail-closed;
    # the best-effort path is only allowed when a primary failure is already propagating.
    require(source, "function Remove-V26ManifestTemporaryWorkspaceStrict", "strict cleanup helper")
    require(source, "function Remove-V26ManifestTemporaryWorkspaceBestEffort", "primary-failure cleanup helper")
    require(source, "$residue = @(Get-ChildItem -LiteralPath $RootPath -Force)", "strict residue enumeration before temp-root cleanup")
    require(source, "Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot", "strict cleanup dispatch")
    require(source, "Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot", "primary-failure cleanup dispatch")
    forbid(source, "Remove-Item -LiteralPath $tempRoot -Recurse", "recursive temp-root cleanup")
    forbid(source, "Remove-Item -LiteralPath $RootPath -Recurse", "recursive helper-root cleanup")

    strict_call = source.index("Remove-V26ManifestTemporaryWorkspaceStrict -ScriptPath $tempScript -RootPath $tempRoot")
    best_effort_call = source.index("Remove-V26ManifestTemporaryWorkspaceBestEffort -ScriptPath $tempScript -RootPath $tempRoot")
    if not dispose_index < strict_call < best_effort_call:
        raise SystemExit("ERROR: V26 held generation cleanup must dispose after invocation, then use strict success cleanup before primary-failure best-effort cleanup")

    print("PASS V26 update-manifest held-generation guard")


if __name__ == "__main__":
    main()
