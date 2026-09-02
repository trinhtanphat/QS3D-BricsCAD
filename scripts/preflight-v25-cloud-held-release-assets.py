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

    for marker in (verify_marker, artifact_marker, publish_marker):
        require(text, marker, f"missing expected cloud V25 release step: {marker.strip()}")
    require(
        text,
        hold_marker,
        "cloud V25 release must seal verified ZIP/checksum bytes into a private held-release directory before any upload",
    )

    verify_index = text.index(verify_marker)
    hold_index = text.index(hold_marker)
    artifact_index = text.index(artifact_marker)
    publish_index = text.index(publish_marker)
    if not (verify_index < hold_index < artifact_index < publish_index):
        fail("held-release snapshot must occur after package verification and before every upload/publish step")

    hold_block = text[hold_index:artifact_index]
    for needle, message in (
        ("$holdDir = Join-Path $env:RUNNER_TEMP", "held-release directory must live outside mutable dist workspace paths"),
        ("Copy-Item -LiteralPath $sourcePath -Destination $heldPath", "verified assets must be copied by literal path into the hold directory"),
        ("Get-FileHash -LiteralPath $heldPath -Algorithm SHA256", "held copies must be hashed after copying"),
        ("Get-Item -LiteralPath $heldPath", "held copies must record byte size after copying"),
        ("RELEASE_ASSET_HOLD_DIR=$holdDir", "held-release directory must be exported for later steps"),
        ("RELEASE_ZIP_SHA256=", "held ZIP SHA-256 identity must be exported"),
        ("RELEASE_CHECKSUM_SHA256=", "held checksum SHA-256 identity must be exported"),
        ("RELEASE_ZIP_SIZE=", "held ZIP byte size must be exported"),
        ("RELEASE_CHECKSUM_SIZE=", "held checksum byte size must be exported"),
    ):
        require(hold_block, needle, message)

    artifact_block = text[artifact_index:publish_index]
    require(
        artifact_block,
        "${{ env.RELEASE_ASSET_HOLD_DIR }}/QS3D-BricsCAD-V25.zip",
        "workflow artifact upload must use the held ZIP rather than reopening dist",
    )
    require(
        artifact_block,
        "${{ env.RELEASE_ASSET_HOLD_DIR }}/QS3D-BricsCAD-V25.zip.sha256",
        "workflow artifact upload must use the held checksum rather than reopening dist",
    )
    if "dist/QS3D-BricsCAD-V25.zip" in artifact_block:
        fail("workflow artifact upload must not reopen the mutable dist ZIP after verification")

    publish_block = text[publish_index:]
    for needle, message in (
        ("$holdDir = (Resolve-Path -LiteralPath $env:RELEASE_ASSET_HOLD_DIR).Path", "publish must resolve the previously held release directory"),
        ("Join-Path $holdDir 'QS3D-BricsCAD-V25.zip'", "publish must source the ZIP from the held directory"),
        ("Join-Path $holdDir 'QS3D-BricsCAD-V25.zip.sha256'", "publish must source the checksum from the held directory"),
        ("Get-FileHash -LiteralPath $assetPath -Algorithm SHA256", "publish must revalidate held asset hashes before upload"),
        ("Get-Item -LiteralPath $assetPath", "publish must revalidate held asset sizes before upload"),
        ("RELEASE_ZIP_SHA256", "publish must bind the held ZIP to its recorded SHA-256"),
        ("RELEASE_CHECKSUM_SHA256", "publish must bind the held checksum to its recorded SHA-256"),
        ("RELEASE_ZIP_SIZE", "publish must bind the held ZIP to its recorded byte size"),
        ("RELEASE_CHECKSUM_SIZE", "publish must bind the held checksum to its recorded byte size"),
    ):
        require(publish_block, needle, message)

    if "Resolve-Path (Join-Path 'dist' $expectedAsset)" in publish_block:
        fail("publish verification must not reopen mutable dist assets after the held snapshot")
    if "@('dist\\QS3D-BricsCAD-V25.zip', 'dist\\QS3D-BricsCAD-V25.zip.sha256')" in publish_block:
        fail("GitHub release upload must not source assets from mutable dist paths")

    print("PASS: cloud V25 release uploads are bound to held post-verification asset bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
