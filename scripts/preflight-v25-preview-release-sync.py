#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
SYNC = ROOT / "scripts" / "sync-preview-release-version.ps1"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"


def fail(message):
    print("ERROR:", message)
    return 1


def require_tokens(text, tokens, label):
    for token in tokens:
        if token not in text:
            raise ValueError(f"{label} lost required release-sync guard: {token}")


def main():
    try:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        prepare = PREPARE.read_text(encoding="utf-8")
        sync = SYNC.read_text(encoding="utf-8")
        package = PACKAGE.read_text(encoding="utf-8")
    except OSError as exc:
        return fail(str(exc))

    try:
        require_tokens(
            workflow,
            [
                "workflow_dispatch:",
                "Prepare exact release source commit",
                ".\\scripts\\prepare-v25-cloud-release.ps1",
                "-ReleaseTag $env:RELEASE_TAG -DispatchSha $env:GITHUB_SHA",
                '"RELEASE_COMMIT_SHA=$releaseCommit"',
                "Package source HEAD must equal RELEASE_COMMIT_SHA",
                "PACKAGE-METADATA gitCommit must match exact release source commit",
                "target_commitish = $env:RELEASE_COMMIT_SHA",
                "Publish source HEAD must equal RELEASE_COMMIT_SHA",
                "Draft prerelease target commit mismatch",
            ],
            "V25 cloud workflow",
        )
        if "target_commitish = $env:GITHUB_SHA" in workflow:
            raise ValueError("V25 cloud workflow regressed to publishing the stale dispatch SHA")

        validate_pos = workflow.find("- name: Validate cloud prerelease request")
        prepare_pos = workflow.find("- name: Prepare exact release source commit")
        manual_gate_pos = workflow.find("- name: Manual-only CI policy gate")
        package_pos = workflow.find("- name: Build V25 preview package")
        publish_pos = workflow.find("- name: Publish GitHub prerelease")
        if min(validate_pos, prepare_pos, manual_gate_pos, package_pos, publish_pos) < 0:
            raise ValueError("V25 cloud workflow lost one or more release stage anchors")
        if not (validate_pos < prepare_pos < manual_gate_pos < package_pos < publish_pos):
            raise ValueError("V25 cloud workflow release preparation/order regressed")

        require_tokens(
            prepare,
            [
                "sync-preview-release-version.ps1",
                "preflight-runtime-product-version-identity.py",
                "git diff --name-only --",
                "git diff --check",
                "git fetch --no-tags origin main",
                "main moved after this workflow was dispatched",
                "git commit -m \"chore(release): prepare $tag\"",
                "git push origin 'HEAD:refs/heads/main'",
                "Could not fast-forward main with the release-preparation commit",
                "Release-preparation push was not read back exactly",
            ],
            "V25 release preparation helper",
        )
        if "--force" in prepare or "git push -f" in prepare:
            raise ValueError("V25 release preparation helper must never force-push main")

        expected_projects = [
            "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
            "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
            "src/QS3D.Core/QS3D.Core.csproj",
        ]
        for path in expected_projects:
            if path not in prepare:
                raise ValueError(f"V25 release preparation helper lost allowed path: {path}")
            if path not in sync:
                raise ValueError(f"Preview version sync helper lost project path: {path}")

        require_tokens(
            sync,
            [
                "-preview\\.(?<preview>0|[1-9][0-9]*)$",
                "Replace-SingleProjectValue",
                "-Element 'Version'",
                "-Element 'FileVersion'",
                "-Element 'InformationalVersion'",
                "New-Object Text.UTF8Encoding($false)",
            ],
            "preview version sync helper",
        )

        require_tokens(
            package,
            [
                "$expectedTag = 'v' + $productVersion",
                "RELEASE_TAG must exactly match the source product version",
                "gitCommit = $gitCommit",
            ],
            "V25 package guard",
        )
    except ValueError as exc:
        return fail(str(exc))

    print("PASS: V25 preview release input automatically synchronizes exact source identity with fail-closed provenance guards")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
