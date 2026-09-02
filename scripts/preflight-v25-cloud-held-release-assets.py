#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def fail(message: str) -> None:
    print(f"ERROR: {message}")
    raise SystemExit(1)


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        fail(message)


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    verify_marker = "      - name: Verify V25 package integrity before upload"
    hold_marker = "      - name: Hold verified V25 release assets"
    artifact_marker = "      - name: Upload cloud preview artifacts"
    publish_marker = "      - name: Publish GitHub prerelease"
    for marker in (verify_marker, hold_marker, artifact_marker, publish_marker):
        require(text, marker, f"missing expected cloud V25 release step: {marker.strip()}")

    verify_index = text.index(verify_marker)
    hold_index = text.index(hold_marker)
    artifact_index = text.index(artifact_marker)
    publish_index = text.index(publish_marker)
    if not (verify_index < hold_index < artifact_index < publish_index):
        fail("held-release snapshot must occur after package verification and before every upload/publish step")

    verify_block = text[verify_index:hold_index]
    for needle, message in (
        ("Get-FileHash -LiteralPath $zipPath -Algorithm SHA256", "verification must capture verified ZIP SHA-256"),
        ("Get-FileHash -LiteralPath $checksumPath -Algorithm SHA256", "verification must capture checksum SHA-256"),
        ("Get-Item -LiteralPath $zipPath", "verification must capture ZIP size"),
        ("Get-Item -LiteralPath $checksumPath", "verification must capture checksum size"),
        ("RELEASE_ZIP_SHA256=$zipHash", "verified ZIP SHA-256 must become fixed release identity"),
        ("RELEASE_CHECKSUM_SHA256=$checksumHash", "verified checksum SHA-256 must become fixed release identity"),
        ("RELEASE_ZIP_SIZE=$($zipInfo.Length)", "verified ZIP size must become fixed release identity"),
        ("RELEASE_CHECKSUM_SIZE=$($checksumInfo.Length)", "verified checksum size must become fixed release identity"),
    ):
        require(verify_block, needle, message)

    hold_block = text[hold_index:artifact_index]
    for needle, message in (
        ("qs3d-v25-release-held-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT", "held directory must be unique to the run attempt"),
        ("$holdDir = Join-Path $env:RUNNER_TEMP $holdName", "held directory must live outside mutable workspace paths"),
        ("Copy-Item -LiteralPath $sourcePath -Destination $heldPath", "verified assets must be copied by literal path"),
        ("Get-FileHash -LiteralPath $heldPath -Algorithm SHA256", "held copies must be hashed"),
        ("Get-Item -LiteralPath $heldPath", "held copies must be sized"),
        ("$heldInfo.Length -ne $spec.Size", "held size must match pre-hold identity"),
        ("[string]::Equals($heldHash, $spec.Hash", "held hash must match pre-hold identity"),
        ("RELEASE_ASSET_HOLD_DIR=$holdDir", "held directory must be exported to runner environment for publish"),
    ):
        require(hold_block, needle, message)

    artifact_block = text[artifact_index:publish_index]
    if "dist/QS3D-BricsCAD-V25.zip" in artifact_block:
        fail("workflow artifact upload must not reopen mutable dist assets after verification")
    require(artifact_block, "${{ runner.temp }}/qs3d-v25-release-held-${{ github.run_id }}-${{ github.run_attempt }}/QS3D-BricsCAD-V25.zip", "artifact upload must address the held ZIP through stable workflow contexts")
    require(artifact_block, "${{ runner.temp }}/qs3d-v25-release-held-${{ github.run_id }}-${{ github.run_attempt }}/QS3D-BricsCAD-V25.zip.sha256", "artifact upload must address the held checksum through stable workflow contexts")
    if "${{ env.RELEASE_ASSET_HOLD_DIR }}" in artifact_block:
        fail("artifact upload must not rely on env context for a value created dynamically through GITHUB_ENV")

    publish_block = text[publish_index:]
    for needle, message in (
        ("$holdDir = (Resolve-Path -LiteralPath $env:RELEASE_ASSET_HOLD_DIR).Path", "publish must resolve the held directory from the runner environment"),
        ("Join-Path $holdDir 'QS3D-BricsCAD-V25.zip'", "publish must source ZIP from held directory"),
        ("Join-Path $holdDir 'QS3D-BricsCAD-V25.zip.sha256'", "publish must source checksum from held directory"),
        ("$env:RELEASE_ZIP_SHA256 -notmatch '^[0-9A-Fa-f]{64}$'", "publish must validate fixed hash identity before draft creation"),
        ("[int64]::TryParse($env:RELEASE_ZIP_SIZE", "publish must validate fixed size identity before draft creation"),
        ("Get-FileHash -LiteralPath $spec.Path -Algorithm SHA256", "publish must revalidate held hash before draft creation"),
        ("Get-Item -LiteralPath $spec.Path", "publish must revalidate held size before draft creation"),
        ("$preUploadHash = (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash", "publish must revalidate immediately before each upload"),
        ("$preUploadInfo = Get-Item -LiteralPath $asset", "publish must revalidate size immediately before each upload"),
        ("$downloadInfo.Length -ne $spec.Size", "downloaded release asset must match fixed verified size"),
        ("[string]::Equals($spec.Hash, $downloadHash", "downloaded release asset must match fixed verified hash"),
    ):
        require(publish_block, needle, message)

    for needle in (
        "Resolve-Path (Join-Path 'dist' $expectedAsset)",
        "@('dist\\QS3D-BricsCAD-V25.zip', 'dist\\QS3D-BricsCAD-V25.zip.sha256')",
        "$localHash = (Get-FileHash -LiteralPath $localPath",
    ):
        if needle in publish_block:
            fail("publish must never derive admission identity from mutable dist paths after verification")

    print("PASS: cloud V25 publication is bound to fixed post-verification asset identity")
    return 0


if __name__ == "__main__":
    sys.exit(main())
