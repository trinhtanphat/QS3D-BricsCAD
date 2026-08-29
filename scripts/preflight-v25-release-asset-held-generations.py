#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_order(text: str, tokens: tuple[str, ...], label: str) -> None:
    cursor = -1
    for token in tokens:
        position = text.find(token, cursor + 1)
        require(position > cursor, f"{label} missing/out-of-order token: {token}")
        cursor = position


def validate_workflow(workflow: str) -> None:
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

    candidate_start = workflow.find("- name: Verify candidate after job boundary")
    candidate_end = workflow.find("- name: Create draft, verify uploaded bytes, then publish", candidate_start)
    require(candidate_start >= 0 and candidate_end > candidate_start, "candidate verification block not found")
    candidate = workflow[candidate_start:candidate_end]
    require_order(
        candidate,
        (
            "$heldZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
            "-Operation Copy -Path $zip -Destination $heldZip",
            "-Operation Hash -Path $heldZip",
            "if ($zipHash -ne $Matches[1])",
            "Expand-Archive -LiteralPath $heldZip",
        ),
        "candidate stable-copy verification",
    )
    require("-Operation Hash -Path $zip" not in candidate, "candidate must not hash original ZIP before reopening it for copy")

    draft_start = workflow.find("- name: Create draft, verify uploaded bytes, then publish")
    require(draft_start >= 0, "draft verification block not found")
    draft = workflow[draft_start:]
    require_order(
        draft,
        (
            "$heldRemoteZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
            "-Operation Copy -Path $remoteZip -Destination $heldRemoteZip",
            "-Operation Hash -Path $heldRemoteZip",
            "if ($remoteZipHash -ne $Matches[1])",
            "Expand-Archive -LiteralPath $heldRemoteZip",
        ),
        "downloaded draft stable-copy verification",
    )
    require("-Operation Hash -Path $remoteZip" not in draft, "downloaded draft must not hash original ZIP before reopening it for copy")


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

    validate_workflow(workflow)

    open_pos = helper.find("[IO.File]::Open($canonical")
    rebound_pos = helper.find("$rebound = Get-Item -LiteralPath $canonical", open_pos)
    hash_pos = helper.find("$sha.ComputeHash($held.Stream)", rebound_pos)
    copy_pos = helper.find("$held.Stream.CopyTo($output)", rebound_pos)
    require(open_pos >= 0 and rebound_pos > open_pos, "V25 held generation must rebind pathname immediately after open")
    require(hash_pos > rebound_pos and copy_pos > rebound_pos, "V25 held generation must bind before hash/copy consumption")

    helper_mutations = (
        (helper.replace("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", 1), "read-only sharing"),
        (helper.replace("[int64]$stream.Length -ne $admittedLength", "[int64]$stream.Length -lt 0", 1), "held length binding"),
        (helper.replace("$sha.ComputeHash($held.Stream)", "(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash", 1), "stream hashing"),
    )
    for mutated, label in helper_mutations:
        require(mutated != helper, "mutation setup failed for " + label)
        require(any(token not in mutated for token in helper_tokens), "mutation probe failed to invalidate " + label)

    workflow_mutations = (
        (workflow.replace("-Operation Hash -Path $heldZip", "-Operation Hash -Path $zip", 1), "candidate split generation"),
        (workflow.replace("-Operation Hash -Path $heldRemoteZip", "-Operation Hash -Path $remoteZip", 1), "draft split generation"),
    )
    for mutated, label in workflow_mutations:
        require(mutated != workflow, "workflow mutation setup failed for " + label)
        rejected = False
        try:
            validate_workflow(mutated)
        except AssertionError:
            rejected = True
        require(rejected, "workflow mutation probe failed to reject " + label)

    print("PASS: V25 commercial release asset verification copies admitted ZIP generations into private stable files before digest verification and expands those same stable copies; split Hash(original)->Copy(original) regressions are rejected.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
