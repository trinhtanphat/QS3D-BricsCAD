#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WRAPPER = ROOT / "scripts" / "package-v25-release.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def provenance_valid(*, dirty_before: bool, dirty_after: bool, head_before: str, head_after: str, metadata_commit: str) -> bool:
    if dirty_before or dirty_after:
        return False
    if not head_before or len(head_before) != 40 or head_before.lower() != head_after.lower():
        return False
    return metadata_commit.lower() == head_before.lower()


def main() -> int:
    require(WRAPPER.is_file(), "missing scripts/package-v25-release.ps1")
    require(WORKFLOW.is_file(), "missing .github/workflows/release-v25.yml")

    wrapper = WRAPPER.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")

    required_wrapper_tokens = (
        "git -C $root @Arguments",
        "rev-parse', '--verify', 'HEAD",
        "status', '--porcelain=v1', '--untracked-files=all",
        "Assert-CleanRepository -Phase 'before package creation'",
        "& $packer",
        "$headAfter = Get-ExactHeadSha",
        "Repository HEAD changed during release packaging",
        "Assert-CleanRepository -Phase 'after package creation'",
        "PACKAGE-METADATA.json",
        "QS3D-BricsCAD-V25.zip",
        "$metadata.gitCommit",
        "does not match the exact clean package source HEAD",
    )
    for token in required_wrapper_tokens:
        require(token in wrapper, "release package provenance wrapper missing token: " + token)

    before_status = wrapper.find("Assert-CleanRepository -Phase 'before package creation'")
    package_call = wrapper.find("& $packer")
    after_head = wrapper.find("$headAfter = Get-ExactHeadSha")
    after_status = wrapper.find("Assert-CleanRepository -Phase 'after package creation'")
    metadata_read = wrapper.find("$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json")
    metadata_compare = wrapper.find("does not match the exact clean package source HEAD")
    positions = (before_status, package_call, after_head, after_status, metadata_read, metadata_compare)
    require(min(positions) >= 0, "release package provenance ordering token is missing")
    require(
        before_status < package_call < after_head < after_status < metadata_read < metadata_compare,
        "release package provenance must verify clean source before packaging and stable clean HEAD plus metadata after packaging",
    )

    workflow_wrapper = "run: .\\scripts\\package-v25-release.ps1"
    require(workflow_wrapper in workflow, "V25 release workflow must route package creation through package-v25-release.ps1")
    require(
        "- name: Build V25 release package" in workflow,
        "V25 release workflow missing package step",
    )

    clean = "a" * 40
    cases = (
        (False, False, clean, clean, clean, True, "clean stable exact provenance"),
        (True, False, clean, clean, clean, False, "dirty before packaging"),
        (False, True, clean, clean, clean, False, "dirty after packaging"),
        (False, False, clean, "b" * 40, clean, False, "HEAD changed during packaging"),
        (False, False, clean, clean, "b" * 40, False, "metadata commit mismatch"),
    )
    for dirty_before, dirty_after, head_before, head_after, metadata_commit, expected, label in cases:
        actual = provenance_valid(
            dirty_before=dirty_before,
            dirty_after=dirty_after,
            head_before=head_before,
            head_after=head_after,
            metadata_commit=metadata_commit,
        )
        require(actual is expected, f"package provenance model mismatch for {label}: expected {expected}, got {actual}")

    print(
        "PASS: official V25 release packaging is wrapped by a clean-source provenance gate that binds a stable exact HEAD to PACKAGE-METADATA gitCommit before downstream release steps."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
