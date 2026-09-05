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

    # Preserve existing residue-safe cleanup: no recursive temp-root deletion.
    forbid(source, "Remove-Item -LiteralPath $tempRoot -Recurse", "recursive temp-root cleanup")
    require(source, "$residue = @(Get-ChildItem -LiteralPath $tempRoot -Force)", "residue enumeration before temp-root cleanup")
    print("PASS V26 update-manifest held-generation guard")


if __name__ == "__main__":
    main()
