#!/usr/bin/env python3
from pathlib import Path
import re
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
    "docs/MAIN-WRITE-AUTHORIZATION.md",
    ".github/workflows/ci.yml",
)


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require(text: str, tokens: tuple[str, ...], label: str, failures: list[str]) -> None:
    for token in tokens:
        if token not in text:
            failures.append(f"{label} missing required contract marker: {token}")


def main() -> int:
    failures: list[str] = []

    for relative in REQUIRED:
        path = ROOT / relative
        if not path.is_file():
            failures.append(f"missing repository professionalism file: {relative}")

    if failures:
        print("Repository professionalism preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    pr = read(".github/pull_request_template.md")
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

    codeowners = read(".github/CODEOWNERS")
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

    dependabot = read(".github/dependabot.yml")
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

    contributing = read("CONTRIBUTING.md")
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

    security = read("SECURITY.md")
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

    bug = read(".github/ISSUE_TEMPLATE/bug_report.yml")
    require(
        bug,
        (
            "name: Bug report", "id: target", "id: release", "id: reproduction", "id: expected",
            "id: actual", "id: evidence", "id: safety", "confidential customer data",
        ),
        "bug issue form",
        failures,
    )

    feature = read(".github/ISSUE_TEMPLATE/feature_request.yml")
    require(
        feature,
        ("name: Feature request", "id: target", "id: problem", "id: acceptance", "id: coordination", "existing issue/PR"),
        "feature issue form",
        failures,
    )

    issue_config = read(".github/ISSUE_TEMPLATE/config.yml")
    if not re.search(r"(?m)^blank_issues_enabled:\s*true\s*$", issue_config):
        failures.append("issue-template config must keep blank issues enabled for coordination/integration work")

    main_auth = read("docs/MAIN-WRITE-AUTHORIZATION.md")
    require(
        main_auth,
        (
            "Default rule: agents treat `main` as read-only", "Explicit authorization required",
            "A normal agent must never use a direct ref update",
        ),
        "main write authorization",
        failures,
    )

    ci = read(".github/workflows/ci.yml")
    require(
        ci,
        (
            "permissions:\n  contents: read", "persist-credentials: false", '"pull_request":',
            "Classify validation scope", "full_validation:", "steps.scope.outputs.full_validation",
            "needs.preflight.outputs.full_validation", "Lightweight governance PR",
            "python scripts/preflight-repository-professionalism.py",
            "dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64",
        ),
        "shared CI",
        failures,
    )
    if "contents: write" in ci:
        failures.append("shared branch/PR CI must not gain contents:write")
    pr_trigger = ci.split('  "pull_request":', 1)[1].split("\npermissions:", 1)[0] if '  "pull_request":' in ci else ""
    if "paths:" in pr_trigger or "paths-ignore:" in pr_trigger:
        failures.append("shared CI pull_request trigger must always emit protected-main required contexts; path filters belong only on branch pushes")

    forbidden_merge_tokens = (
        "pull_request_target:", "gh pr merge", "enablepullrequestautomerge", "enable-pull-request-auto-merge",
    )
    for workflow in sorted((ROOT / ".github" / "workflows").glob("*.y*ml")):
        text = workflow.read_text(encoding="utf-8")
        lowered = text.lower()
        for token in forbidden_merge_tokens:
            if token in lowered:
                failures.append(f"{workflow.name}: autonomous main-merge primitive is forbidden by repository governance: {token}")
        if re.search(r"repos/[^\s\"']+/pulls/[^\s\"']+/merge", lowered):
            failures.append(f"{workflow.name}: direct pull-request merge API call is forbidden by repository governance")

    if failures:
        print("Repository professionalism preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: repository professionalism contracts are present and fail-closed.")
    print(" - contributor, PR and issue surfaces require auditable task/evidence metadata")
    print(" - security reporting avoids public disclosure of sensitive material")
    print(" - critical governance/release surfaces have explicit ownership")
    print(" - dependency maintenance is bounded and low-noise")
    print(" - every PR emits stable required contexts while docs-only candidates avoid redundant Core/V25 builds")
    print(" - no workflow implements autonomous PR-to-main merging")
    return 0


if __name__ == "__main__":
    sys.exit(main())
