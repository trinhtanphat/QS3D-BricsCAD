#!/usr/bin/env python3
from pathlib import Path
import os
import re
import stat
import sys

ROOT = Path(__file__).resolve().parents[1]

REQUIRED = (
    ".github/pull_request_template.md",
    ".github/CODEOWNERS",
    ".github/dependabot.yml",
    ".github/ISSUE_TEMPLATE/bug_report.yml",
    ".github/ISSUE_TEMPLATE/feature_request.yml",
    ".github/ISSUE_TEMPLATE/config.yml",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "CI_POLICY.md",
    "docs/MAIN-WRITE-AUTHORIZATION.md",
    ".github/workflows/ci.yml",
)

FORBIDDEN_EXTERNAL_ORCHESTRATION_PATHS = {
    "docs/hourly-agent-control.md",
    "scripts/preflight-hourly-agent-control.py",
}
FORBIDDEN_EXTERNAL_ORCHESTRATION_PATH_MARKERS = (
    "hourly-agent-control",
    "scheduled-agent-control",
    "agent-orchestration",
    "controller-worker-pool",
)
ORCHESTRATION_SCAN_SUFFIXES = {".md", ".txt", ".yml", ".yaml", ".json", ".toml", ".py", ".ps1", ".sh"}
MAX_REPOSITORY_TEXT_BYTES = 1024 * 1024
MAX_ORCHESTRATION_SCAN_BYTES = MAX_REPOSITORY_TEXT_BYTES
MAX_OPEN_IDENTITY_ATTEMPTS = 2
WINDOWS_REPARSE_POINT_ATTRIBUTE = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x0400)
SELF_PATH = "scripts/preflight-repository-professionalism.py"
HYBRID_COORDINATOR = "hybrid-pr-coordinator.yml"


def _metadata_type_error(metadata) -> str | None:
    if stat.S_ISLNK(metadata.st_mode):
        return "must not be a symlink"
    if getattr(metadata, "st_file_attributes", 0) & WINDOWS_REPARSE_POINT_ATTRIBUTE:
        return "must not be a Windows reparse point"
    if not stat.S_ISREG(metadata.st_mode):
        return "must be a regular file"
    return None


def _same_opened_file(before, opened) -> bool:
    before_dev = getattr(before, "st_dev", 0)
    before_ino = getattr(before, "st_ino", 0)
    opened_dev = getattr(opened, "st_dev", 0)
    opened_ino = getattr(opened, "st_ino", 0)
    if before_dev and before_ino and opened_dev and opened_ino:
        return (before_dev, before_ino) == (opened_dev, opened_ino)
    return True


def read_repository_text(path: Path, root: Path = ROOT, maximum_bytes: int = MAX_REPOSITORY_TEXT_BYTES) -> tuple[str | None, str | None]:
    if maximum_bytes < 0:
        return None, "invalid negative text-size bound"

    try:
        root_resolved = root.resolve(strict=True)
    except OSError as exc:
        return None, f"cannot inspect repository text input: {exc}"

    payload = None
    identity_changed = False
    for attempt in range(MAX_OPEN_IDENTITY_ATTEMPTS):
        try:
            metadata = path.lstat()
        except OSError as exc:
            return None, f"cannot inspect repository text input: {exc}"

        type_error = _metadata_type_error(metadata)
        if type_error is not None:
            return None, type_error
        if metadata.st_size > maximum_bytes:
            return None, f"exceeds {maximum_bytes} byte safety bound ({metadata.st_size} bytes)"

        try:
            resolved = path.resolve(strict=True)
            resolved.relative_to(root_resolved)
        except (OSError, ValueError) as exc:
            return None, f"escapes repository root or cannot be resolved safely: {exc}"

        flags = os.O_RDONLY | getattr(os, "O_BINARY", 0) | getattr(os, "O_NOFOLLOW", 0)
        fd = None
        try:
            fd = os.open(path, flags)
            opened_metadata = os.fstat(fd)
            opened_type_error = _metadata_type_error(opened_metadata)
            if opened_type_error is not None:
                return None, opened_type_error
            if not _same_opened_file(metadata, opened_metadata):
                identity_changed = True
                if attempt + 1 < MAX_OPEN_IDENTITY_ATTEMPTS:
                    continue
                return None, "changed identity between filesystem validation and open after bounded retry"
            if opened_metadata.st_size > maximum_bytes:
                return None, f"exceeds {maximum_bytes} byte safety bound ({opened_metadata.st_size} bytes)"

            chunks: list[bytes] = []
            total = 0
            while total <= maximum_bytes:
                chunk = os.read(fd, min(64 * 1024, maximum_bytes + 1 - total))
                if not chunk:
                    break
                chunks.append(chunk)
                total += len(chunk)
            if total > maximum_bytes:
                return None, f"exceeds {maximum_bytes} byte safety bound while reading"
            payload = b"".join(chunks)
            identity_changed = False
            break
        except OSError as exc:
            return None, f"cannot open/read repository text input safely: {exc}"
        finally:
            if fd is not None:
                os.close(fd)

    if payload is None:
        if identity_changed:
            return None, "changed identity between filesystem validation and open after bounded retry"
        return None, "repository text input could not be read safely"

    try:
        text = payload.decode("utf-8")
    except UnicodeDecodeError as exc:
        return None, f"is not valid UTF-8: {exc}"

    return text.replace("\r\n", "\n").replace("\r", "\n"), None


def require(text: str, tokens: tuple[str, ...], label: str, failures: list[str]) -> None:
    for token in tokens:
        if token not in text:
            failures.append(f"{label} missing required contract marker: {token}")


def reject_external_orchestration_artifacts(failures: list[str]) -> None:
    for path in ROOT.rglob("*"):
        if ".git" in path.parts:
            continue

        relative = path.relative_to(ROOT).as_posix()
        lowered_relative = relative.lower()
        if lowered_relative in FORBIDDEN_EXTERNAL_ORCHESTRATION_PATHS or any(
            marker in lowered_relative for marker in FORBIDDEN_EXTERNAL_ORCHESTRATION_PATH_MARKERS
        ):
            failures.append(
                f"external scheduler/orchestration artifact must stay outside the QS3D source tree: {relative}"
            )
            continue

        if relative == SELF_PATH or path.suffix.lower() not in ORCHESTRATION_SCAN_SUFFIXES:
            continue

        try:
            metadata = path.lstat()
        except OSError as exc:
            failures.append(f"cannot inspect orchestration-scanned repository path metadata: {relative}: {exc}")
            continue
        if stat.S_ISDIR(metadata.st_mode) and not (
            getattr(metadata, "st_file_attributes", 0) & WINDOWS_REPARSE_POINT_ATTRIBUTE
        ):
            continue

        text, error = read_repository_text(path, ROOT, MAX_ORCHESTRATION_SCAN_BYTES)
        if error is not None:
            failures.append(f"unsafe orchestration-scanned repository file: {relative}: {error}")
            continue

        if "QS3D-CONTROL" in text and "QS3D-WORKER-" in text:
            failures.append(
                f"external scheduler topology leaked into repository content: {relative}; keep it in automation configuration/coordination state"
            )


def main() -> int:
    failures: list[str] = []
    texts: dict[str, str] = {}

    for relative in REQUIRED:
        text, error = read_repository_text(ROOT / relative)
        if error is not None:
            failures.append(f"unsafe or missing repository professionalism file: {relative}: {error}")
        else:
            texts[relative] = text

    reject_external_orchestration_artifacts(failures)

    if failures:
        print("Repository professionalism preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    pr = texts[".github/pull_request_template.md"]
    require(
        pr,
        (
            "## Scope", "Issue:", "Baseline `main` SHA:", "Head SHA:", "## Validation",
            "PENDING_LOCAL", "## Release impact", "## Merge authorization",
            "does **not** authorize its own merge", "docs/MAIN-WRITE-AUTHORIZATION.md",
        ),
        "pull request template",
        failures,
    )

    codeowners = texts[".github/CODEOWNERS"]
    require(
        codeowners,
        (
            "/.github/workflows/ @trinhtanphat", "/.github/CODEOWNERS @trinhtanphat",
            "/AGENTS.md @trinhtanphat", "/CI_POLICY.md @trinhtanphat",
            "/docs/MAIN-WRITE-AUTHORIZATION.md @trinhtanphat",
            "/scripts/preflight-ci-manual-only.py @trinhtanphat",
            "/scripts/*release* @trinhtanphat", "/scripts/*sign* @trinhtanphat",
            "does not grant merge authority",
        ),
        "CODEOWNERS",
        failures,
    )

    dependabot = texts[".github/dependabot.yml"]
    require(
        dependabot,
        (
            "version: 2", 'package-ecosystem: "github-actions"', 'package-ecosystem: "nuget"',
            'interval: "weekly"', 'interval: "monthly"', 'timezone: "Asia/Ho_Chi_Minh"',
            'prefix: "chore(deps)"',
        ),
        "Dependabot configuration",
        failures,
    )
    if "registries:" in dependabot or "target-branch:" in dependabot:
        failures.append("Dependabot must not introduce private registries or a non-default integration target without an explicit repository decision")

    contributing = texts["CONTRIBUTING.md"]
    require(
        contributing,
        (
            "docs/MAIN-WRITE-AUTHORIZATION.md", "AGENTS.md", "CI_POLICY.md",
            "docs/AGENT-WORK-REGISTRATION.md", "`main` is read-only for normal agents and contributors",
            "PENDING_LOCAL", "Do not weaken assertions", "does not self-authorize merge",
        ),
        "CONTRIBUTING.md",
        failures,
    )

    security = texts["SECURITY.md"]
    require(
        security,
        (
            "do **not** report", "GitHub private vulnerability reporting", "@trinhtanphat",
            "Never include private keys", "fail closed",
            "Licensed BricsCAD runtime binaries and signing credentials",
        ),
        "SECURITY.md",
        failures,
    )

    bug = texts[".github/ISSUE_TEMPLATE/bug_report.yml"]
    require(
        bug,
        (
            "name: Bug report", "id: target", "id: release", "id: reproduction", "id: expected",
            "id: actual", "id: evidence", "id: safety", "confidential customer data",
        ),
        "bug issue form",
        failures,
    )

    feature = texts[".github/ISSUE_TEMPLATE/feature_request.yml"]
    require(
        feature,
        ("name: Feature request", "id: target", "id: problem", "id: acceptance", "id: coordination", "existing issue/PR"),
        "feature issue form",
        failures,
    )

    issue_config = texts[".github/ISSUE_TEMPLATE/config.yml"]
    if not re.search(r"(?m)^blank_issues_enabled:\s*true\s*$", issue_config):
        failures.append("issue-template config must keep blank issues enabled for coordination/integration work")

    main_auth = texts["docs/MAIN-WRITE-AUTHORIZATION.md"]
    require(
        main_auth,
        (
            "Default rule: agents treat `main` as read-only", "Explicit authorization required",
            "A normal agent must never use a direct ref update",
        ),
        "main write authorization",
        failures,
    )

    ci_policy = texts["CI_POLICY.md"]
    require(
        ci_policy,
        (
            "## Automatic branch CI and canonical PR lifecycle",
            "its completion timestamp is not a permanent PR-admission identity",
            "required `preflight` and `core` must be terminal `SUCCESS`",
            "### Dependabot generated-PR boundary",
            "GitHub Dependabot may create dependency-update PRs directly",
            "does **not** authorize Dependabot to merge",
            "Repository-wide blind auto-merge remains intentionally disabled",
            "repository-metadata tier",
            "policy/source-guard tier",
            "full build tier",
            "every** pull request targeting `main`",
            "samples/generated/**",
            "persist-credentials: false",
        ),
        "CI_POLICY.md professionalism contract",
        failures,
    )

    ci = texts[".github/workflows/ci.yml"]
    require(
        ci,
        (
            "permissions:\n  contents: read", "persist-credentials: false", '"pull_request":',
            "Classify validation scope", "source_validation:", "build_validation:",
            "steps.scope.outputs.source_validation", "needs.preflight.outputs.build_validation",
            "Lightweight non-build candidate", "samples/generated/",
            "python scripts/preflight-repository-professionalism.py",
            ".\\scripts\\build-v25-with-stable-references.ps1",
        ),
        "shared CI",
        failures,
    )
    if "contents: write" in ci:
        failures.append("shared branch/PR CI must not gain contents:write")
    push_trigger = ci.split('  "push":', 1)[1].split('  "pull_request":', 1)[0] if '  "push":' in ci and '  "pull_request":' in ci else ""
    if "paths:" in push_trigger or "paths-ignore:" in push_trigger:
        failures.append("shared CI push trigger must remain unfiltered so docs-only or ancestry-only reconciliation heads still receive exact-head branch CI")
    pr_trigger = ci.split('  "pull_request":', 1)[1].split("\npermissions:", 1)[0] if '  "pull_request":' in ci else ""
    if "paths:" in pr_trigger or "paths-ignore:" in pr_trigger:
        failures.append("shared CI pull_request trigger must always emit protected-main required contexts and must not use path filters")

    globally_forbidden_merge_tokens = (
        "pull_request_target:", "gh pr merge", "enable-pull-request-auto-merge",
    )
    native_automerge_token = "enablepullrequestautomerge"
    workflows_dir = ROOT / ".github" / "workflows"
    workflow_paths = sorted(
        (path for path in workflows_dir.iterdir() if path.suffix.lower() in {".yml", ".yaml"}),
        key=lambda path: (path.name.casefold(), path.name),
    )
    for workflow in workflow_paths:
        text, error = read_repository_text(workflow)
        if error is not None:
            failures.append(f"unsafe workflow source for autonomous-merge scan: {workflow.name}: {error}")
            continue
        lowered = text.lower()
        for token in globally_forbidden_merge_tokens:
            if token in lowered:
                failures.append(f"{workflow.name}: autonomous main-merge primitive is forbidden by repository governance: {token}")
        if native_automerge_token in lowered and workflow.name != HYBRID_COORDINATOR:
            failures.append(
                f"{workflow.name}: GitHub native auto-merge arming is reserved for {HYBRID_COORDINATOR}"
            )
        if re.search(r"repos/[^\s\"']+/pulls/[^\s\"']+/merge(?:[\s\"']|$)", lowered):
            failures.append(f"{workflow.name}: direct pull-request merge API call is forbidden by repository governance")

        if workflow.name == HYBRID_COORDINATOR:
            require(
                text,
                (
                    "name: QS3D Hybrid PR Coordinator",
                    '  "pull_request":', '  "push":',
                    "enablePullRequestAutoMerge", "QS3D_AUTOMERGE_TOKEN",
                    "/update-branch", "expected_head_sha", "no-automerge",
                    "head.repo.full_name", "base.ref", "draft",
                    "group: qs3d-hybrid-pr-coordinator", "cancel-in-progress: false",
                ),
                "hybrid PR coordinator",
                failures,
            )
            if "contents: write" in text or "actions: write" in text:
                failures.append(f"{HYBRID_COORDINATOR}: workflow-level write permissions must stay narrow")

    if failures:
        print("Repository professionalism preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: repository professionalism contracts are present and fail-closed.")
    print(" - contributor, PR and issue surfaces require auditable task/evidence metadata")
    print(" - security reporting avoids public disclosure of sensitive material")
    print(" - critical governance/release surfaces have explicit ownership")
    print(" - dependency maintenance is bounded and its generated-PR boundary cannot grant merge/release authority")
    print(" - branch CI provides early exact-head defect evidence without turning PR-creation timing into a permanent admission blocker")
    print(" - every task/integration branch push and every PR can emit stable required contexts while non-build changes avoid redundant Core/V25 builds")
    print(" - synthetic generated fixtures are treated as build-relevant validation inputs")
    print(" - external scheduler/controller-worker orchestration artifacts are kept out of the QS3D source tree")
    print(f" - only {HYBRID_COORDINATOR} may arm GitHub native auto-merge; direct PR merge primitives remain forbidden")
    return 0


if __name__ == "__main__":
    sys.exit(main())