#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")

    helper_tokens = (
        "[ValidateSet('Hash', 'Copy')]",
        "[IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "[int64]$rebound.Length -ne $admittedLength",
        "[int64]$rebound.LastWriteTimeUtc.Ticks -ne $admittedWriteTicks",
        "[int64]$stream.Length -ne $admittedLength",
        "[Security.Cryptography.SHA256]::Create()",
        "$sha.ComputeHash($held.Stream)",
        "$held.Stream.CopyTo($output)",
        "$output.Flush($true)",
        "$held.Stream.Dispose()",
        "FileAttributes]::ReparsePoint",
    )
    for token in helper_tokens:
        require(token in helper, "V25 held-generation helper missing token: " + token)

    workflow_tokens = (
        "scripts\\verify-v25-held-file.ps1",
        "-Operation Hash",
        "-Operation Copy",
    )
    for token in workflow_tokens:
        require(token in workflow, "V25 commercial release workflow missing held-generation token: " + token)

    forbidden = (
        "$localHash = (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash",
        "$remoteHash = (Get-FileHash -LiteralPath (Join-Path $downloadRoot $name) -Algorithm SHA256).Hash",
        "if ((Get-FileHash -LiteralPath $remoteZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Matches[1])",
        "$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()",
        "$updateHash = (Get-FileHash -LiteralPath $update -Algorithm SHA256).Hash.ToLowerInvariant()",
    )
    for token in forbidden:
        require(token not in workflow, "V25 commercial release regressed to pathname-only hashing: " + token)

    open_pos = helper.find("[IO.File]::Open($canonical")
    rebound_pos = helper.find("$rebound = Get-Item -LiteralPath $canonical", open_pos)
    hash_pos = helper.find("$sha.ComputeHash($held.Stream)", rebound_pos)
    copy_pos = helper.find("$held.Stream.CopyTo($output)", rebound_pos)
    require(open_pos >= 0 and rebound_pos > open_pos, "V25 held generation must rebind pathname immediately after open")
    require(hash_pos > rebound_pos and copy_pos > rebound_pos, "V25 held generation must bind before hash/copy consumption")

    mutations = (
        (helper.replace("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", 1), "read-only sharing"),
        (helper.replace("[int64]$stream.Length -ne $admittedLength", "[int64]$stream.Length -lt 0", 1), "held length binding"),
        (helper.replace("$sha.ComputeHash($held.Stream)", "(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash", 1), "stream hashing"),
    )
    for mutated, label in mutations:
        require(mutated != helper, "mutation setup failed for " + label)
        require(mutated != helper and any(token not in mutated for token in helper_tokens), "mutation probe failed to invalidate " + label)

    print("PASS: V25 commercial release asset verification binds admitted path metadata to held read-only generations before hash/copy consumption and rejects pathname-only hashing regressions.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
