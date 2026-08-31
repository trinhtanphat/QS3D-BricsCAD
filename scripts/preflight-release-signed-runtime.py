#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    if not WORKFLOW.is_file():
        raise AssertionError("missing release-v25.yml")
    text = WORKFLOW.read_text(encoding="utf-8")

    require(text, "Stable commercial releases require run_runtime=true.", "stable runtime requirement")
    require(text, "- name: Authenticode-sign commercial V25 payload", "commercial signing step")
    require(text, "- name: Verify Authenticode publisher and trusted timestamp", "signature verification step")
    require(text, "- name: Finalize signed V25 package", "signed package finalization")
    require(text, "- name: Remove ephemeral signing certificate and private key", "ephemeral key cleanup")
    require(text, "- name: Verify finalized package after private-key cleanup", "post-cleanup signature verification")
    require(text, "- name: Licensed V25 runtime validation for exact signed payload", "signed runtime gate")
    require(text, "if: ${{ inputs.run_runtime }}", "runtime condition")
    require(text, "dist\\QS3D-BricsCAD-V25\\QS3D.BricsCAD.V25.dll", "exact signed staged plugin runtime target")
    require(text, "artifacts\\bricscad-v25-runtime-signed", "signed runtime evidence folder")
    if "inputs.sign_package" in text or "sign_package:" in text:
        raise AssertionError("commercial release must remain signed-only; obsolete optional signing branch reappeared")

    sign = text.find("- name: Authenticode-sign commercial V25 payload")
    verify = text.find("- name: Verify Authenticode publisher and trusted timestamp")
    finalize = text.find("- name: Finalize signed V25 package")
    cleanup = text.find("- name: Remove ephemeral signing certificate and private key")
    reverify = text.find("- name: Verify finalized package after private-key cleanup")
    signed_runtime = text.find("- name: Licensed V25 runtime validation for exact signed payload")
    manifest = text.find("- name: Create signed auto-update manifest")
    provenance = text.find("- name: Create commercial checksum and provenance")
    upload = text.find("- name: Upload signed commercial candidate")
    ordered = (sign, verify, finalize, cleanup, reverify, signed_runtime, manifest, provenance, upload)
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        raise AssertionError("commercial release must sign -> verify -> finalize -> remove key -> reverify -> runtime-test exact signed dist payload -> manifest/provenance -> artifact handoff")

    signed_block = text[signed_runtime:manifest]
    if "src\\QS3D.BricsCAD.V25\\bin" in signed_block:
        raise AssertionError("signed runtime gate must not test the pre-sign build output")
    if "dist\\QS3D-BricsCAD-V25\\QS3D.BricsCAD.V25.dll" not in signed_block:
        raise AssertionError("signed runtime gate must test the finalized staged plugin")

    require(text, "Verify candidate after job boundary", "cross-job candidate verification")
    require(text, "Create draft, verify uploaded bytes, then publish", "draft-first publication gate")
    require(text, "$tagCreatedByThisRun = $true", "positive publication tag ownership")
    require(text, "$releaseId = [long]$release.id", "exact draft identity capture")
    require(text, "gh release download $env:RELEASE_TAG", "downloaded draft-byte verification")
    require(text, "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri", "exact-release publish transition")
    require(text, "if ($published.draft -ne $false)", "published-state confirmation")
    require(text, "rollback-v25-draft-release.ps1", "bounded failure rollback")

    publication = text.find("- name: Create draft, verify uploaded bytes, then publish")
    tag_owned = text.find("$tagCreatedByThisRun = $true", publication)
    draft_identity = text.find("$releaseId = [long]$release.id", tag_owned)
    download = text.find("gh release download $env:RELEASE_TAG", draft_identity)
    signature = text.find("verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", download)
    publish = text.find("$published = Invoke-RestMethod -Method Patch -Uri $releaseUri", signature)
    rollback = text.find("rollback-v25-draft-release.ps1", publish)
    publish_order = (publication, tag_owned, draft_identity, download, signature, publish, rollback)
    if any(index < 0 for index in publish_order) or list(publish_order) != sorted(publish_order):
        raise AssertionError("commercial publication must own exact tag -> capture exact draft -> verify downloaded bytes/signatures -> publish exact release -> retain bounded rollback")

    print("PASS: commercial V25 releases are signed-only, remove the ephemeral private key before re-verification, runtime-test the exact finalized signed plugin, verify draft bytes before exact-release publication, and retain bounded restart-safe rollback.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
