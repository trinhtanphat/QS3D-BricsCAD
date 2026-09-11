#!/usr/bin/env python3
"""Fail closed unless V25 commercial archive consumption stays on admitted file generations."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"
EXTRACTOR = ROOT / "scripts" / "expand-v25-commercial-candidate.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def replace_after(text: str, marker: str, old: str, new: str, label: str) -> str:
    marker_pos = text.find(marker)
    require(marker_pos >= 0, f"{label} marker not found: {marker}")
    old_pos = text.find(old, marker_pos)
    require(old_pos >= 0, f"{label} token not found after marker: {old}")
    return text[:old_pos] + new + text[old_pos + len(old):]


def require_order(text: str, tokens: tuple[str, ...], label: str) -> None:
    cursor = -1
    for token in tokens:
        position = text.find(token, cursor + 1)
        require(position > cursor, f"{label} missing/out-of-order token: {token}")
        cursor = position


def validate_helper(helper: str) -> None:
    tokens = (
        "[ValidateSet('Hash', 'Copy')]",
        "[IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "[int64]$rebound.Length -ne $admittedLength",
        "[int64]$rebound.LastWriteTimeUtc.Ticks -ne $admittedWriteTicks",
        "[int64]$stream.Length -ne $admittedLength",
        "Get-HeldStreamSha256",
        "$Stream.Position = 0",
        "$sha.ComputeHash($Stream)",
        "$Stream.Position = $priorPosition",
        "Publish-CommercialZipDigest",
        "'QS3D-BricsCAD-V25.zip'",
        "$env:QS3D_V25_COMMERCIAL_ZIP_SHA256 = $Digest.ToLowerInvariant()",
        "$held.Stream.CopyTo($output)",
        "$output.Flush($true)",
        "$held.Stream.Dispose()",
        "FileAttributes]::ReparsePoint",
    )
    for token in tokens:
        require(token in helper, "V25 held-generation helper missing token: " + token)
    require("Get-FileHash -LiteralPath $Path" not in helper, "held-generation helper regressed to pathname hashing")

    open_pos = helper.find("[IO.File]::Open($canonical")
    rebound_pos = helper.find("$rebound = Get-Item -LiteralPath $canonical", open_pos)
    digest_pos = helper.find("Get-HeldStreamSha256 -Stream $held.Stream", rebound_pos)
    copy_pos = helper.find("$held.Stream.CopyTo($output)", rebound_pos)
    dispose_pos = helper.rfind("$held.Stream.Dispose()")
    require(open_pos >= 0 and rebound_pos > open_pos, "V25 held generation must rebind pathname immediately after open")
    require(digest_pos > rebound_pos and copy_pos > rebound_pos, "V25 held generation must bind before digest/copy consumption")
    require(dispose_pos > max(digest_pos, copy_pos), "held file stream must remain alive through digest/copy consumption")


def validate_extractor(extractor: str) -> None:
    tokens = (
        "$expectedZipSha256 = [string]$env:QS3D_V25_COMMERCIAL_ZIP_SHA256",
        "$zipSha = [Security.Cryptography.SHA256]::Create()",
        "$parsedDigestBytes = $zipSha.ComputeHash($zipStream)",
        "[string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase)",
        "$zipStream.Position = 0",
        "$archive = [IO.Compression.ZipArchive]::new($zipStream",
    )
    for token in tokens:
        require(token in extractor, "safe extractor missing exact-generation binding token: " + token)
    require("Get-FileHash" not in extractor, "safe extractor must hash the exact held stream, never reopen by pathname for digest")
    open_pos = extractor.find("$zipStream = [IO.File]::Open($zipFull")
    compute_pos = extractor.find("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", open_pos)
    compare_pos = extractor.find("[string]::Equals($parsedDigest, $expectedZipSha256", compute_pos)
    rewind_pos = extractor.find("$zipStream.Position = 0", compare_pos)
    archive_pos = extractor.find("$archive = [IO.Compression.ZipArchive]::new($zipStream", rewind_pos)
    require(0 <= open_pos < compute_pos < compare_pos < rewind_pos < archive_pos, "exact ZIP stream must be hashed, compared, rewound and only then parsed")


def validate_workflow(workflow: str) -> None:
    for token in ("scripts\\verify-v25-held-file.ps1", "scripts\\expand-v25-commercial-candidate.ps1", "-Operation Hash", "-Operation Copy"):
        require(token in workflow, "V25 commercial release workflow missing held-generation token: " + token)
    for forbidden in (
        "$localHash = (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash",
        "$remoteHash = (Get-FileHash -LiteralPath (Join-Path $downloadRoot $name) -Algorithm SHA256).Hash",
        "if ((Get-FileHash -LiteralPath $remoteZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Matches[1])",
        "$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()",
    ):
        require(forbidden not in workflow, "V25 commercial release regressed to pathname-only hashing: " + forbidden)

    candidate_start = workflow.find("- name: Verify candidate after job boundary")
    candidate_end = workflow.find("- name: Create draft, verify uploaded bytes, then publish", candidate_start)
    require(candidate_start >= 0 and candidate_end > candidate_start, "candidate verification block not found")
    candidate = workflow[candidate_start:candidate_end]
    require_order(candidate, (
        "$heldZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
        "-Operation Copy -Path $zip -Destination $heldZip",
        "-Operation Hash -Path $heldZip",
        "if ($zipHash -ne $Matches[1])",
        ".\\scripts\\expand-v25-commercial-candidate.ps1",
        "-ZipPath $heldZip",
        "-DestinationRoot $extract",
    ), "candidate stable-copy verification")
    require("-Operation Hash -Path $zip" not in candidate, "candidate must not hash original ZIP before reopening it for copy")
    require("Expand-Archive" not in candidate, "candidate must not bypass bounded safe extraction with raw Expand-Archive")
    require("-ZipPath $zip" not in candidate, "candidate safe extractor must consume the admitted held ZIP generation")

    draft_start = workflow.find("- name: Create draft, verify uploaded bytes, then publish")
    require(draft_start >= 0, "draft verification block not found")
    draft = workflow[draft_start:]
    require_order(draft, (
        "$heldRemoteZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
        "-Operation Copy -Path $remoteZip -Destination $heldRemoteZip",
        "-Operation Hash -Path $heldRemoteZip",
        "if ($remoteZipHash -ne $Matches[1])",
        ".\\scripts\\expand-v25-commercial-candidate.ps1",
        "-ZipPath $heldRemoteZip",
        "-DestinationRoot $extract",
    ), "downloaded draft stable-copy verification")
    require("-Operation Hash -Path $remoteZip" not in draft, "downloaded draft must not hash original ZIP before reopening it for copy")
    require("Expand-Archive" not in draft, "downloaded draft must not bypass bounded safe extraction with raw Expand-Archive")
    require("-ZipPath $remoteZip" not in draft, "downloaded draft safe extractor must consume the admitted held ZIP generation")


def main() -> int:
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")
    extractor = EXTRACTOR.read_text(encoding="utf-8")
    validate_helper(helper)
    validate_extractor(extractor)
    validate_workflow(workflow)

    helper_mutations = (
        (helper.replace("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", 1), "read-only source sharing"),
        (helper.replace("$sha.ComputeHash($Stream)", "(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash", 1), "stream hashing"),
        (helper.replace("$env:QS3D_V25_COMMERCIAL_ZIP_SHA256 = $Digest.ToLowerInvariant()", "$null = $Digest", 1), "digest publication"),
        (helper.replace("'QS3D-BricsCAD-V25.zip'", "'any.zip'", 1), "exact ZIP asset scoping"),
    )
    for mutated, label in helper_mutations:
        rejected = False
        try:
            validate_helper(mutated)
        except AssertionError:
            rejected = True
        require(rejected, "helper mutation probe failed to reject " + label)

    extractor_mutations = (
        (extractor.replace("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", "$parsedDigestBytes = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash", 1), "pathname digest reopen"),
        (extractor.replace("if (-not [string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), "missing digest comparison"),
        (extractor.replace("$zipStream.Position = 0\n\n    $archive", "$archive", 1), "missing rewind before parse"),
    )
    for mutated, label in extractor_mutations:
        rejected = False
        try:
            validate_extractor(mutated)
        except AssertionError:
            rejected = True
        require(rejected, "extractor mutation probe failed to reject " + label)

    workflow_mutations = (
        (workflow.replace("-Operation Hash -Path $heldZip", "-Operation Hash -Path $zip", 1), "candidate split generation"),
        (workflow.replace("-Operation Hash -Path $heldRemoteZip", "-Operation Hash -Path $remoteZip", 1), "draft split generation"),
        (replace_after(workflow, "- name: Verify candidate after job boundary", "-ZipPath $heldZip", "-ZipPath $zip", "candidate pathname extraction"), "candidate pathname extraction"),
        (replace_after(workflow, "- name: Create draft, verify uploaded bytes, then publish", "-ZipPath $heldRemoteZip", "-ZipPath $remoteZip", "draft pathname extraction"), "draft pathname extraction"),
    )
    for mutated, label in workflow_mutations:
        rejected = False
        try:
            validate_workflow(mutated)
        except AssertionError:
            rejected = True
        require(rejected, "workflow mutation probe failed to reject " + label)

    print("PASS: V25 commercial release asset verification binds held ZIP digest admission to the exact still-open stream parsed by the bounded extractor; split generation, pathname hashing/extraction, and raw Expand-Archive regressions are rejected.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
