#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
RELEASE_WORKFLOWS = {"release-v25.yml", "release-v25-cloud.yml"}
errors = []


def collect_job_blocks(lines):
    jobs_index = next((i for i, line in enumerate(lines) if re.match(r"^jobs\s*:\s*(?:#.*)?$", line)), None)
    if jobs_index is None:
        return []
    blocks = []
    current_name = None
    current_lines = []
    for line in lines[jobs_index + 1:]:
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        match = re.match(r"^\s{2}([A-Za-z0-9_-]+)\s*:\s*(?:#.*)?$", line)
        if match:
            if current_name is not None:
                blocks.append((current_name, current_lines))
            current_name = match.group(1)
            current_lines = []
            continue
        if current_name is not None:
            current_lines.append(line)
    if current_name is not None:
        blocks.append((current_name, current_lines))
    return blocks


def strip_yaml_inline_comment(value):
    quote = None
    index = 0
    while index < len(value):
        char = value[index]
        if quote == "'":
            if char == "'":
                if index + 1 < len(value) and value[index + 1] == "'":
                    index += 2
                    continue
                quote = None
            index += 1
            continue
        if quote == '"':
            if char == "\\" and index + 1 < len(value):
                index += 2
                continue
            if char == '"':
                quote = None
            index += 1
            continue
        if char in ("'", '"'):
            quote = char
            index += 1
            continue
        if char == "#":
            return value[:index].rstrip()
        index += 1
    return value.rstrip()


def extract_job_if_expression(job_lines):
    expressions = []
    for line in job_lines:
        match = re.match(r"^\s{4}if\s*:\s*(.*)$", line)
        if match:
            expressions.append(strip_yaml_inline_comment(match.group(1)).strip())
    if len(expressions) != 1:
        return None

    expression = expressions[0]
    if not expression:
        return None
    if expression.startswith("${{"):
        if not expression.endswith("}}"):
            return None
        expression = expression[3:-2].strip()
    return expression or None


def is_hard_manual_dispatch_guard(expression):
    if expression is None or "||" in expression:
        return False
    return bool(re.fullmatch(
        r"github\.event_name\s*==\s*'workflow_dispatch'(?:\s*&&\s*.+)?",
        expression,
    ))


def is_hard_release_confirmation_guard(expression):
    if not is_hard_manual_dispatch_guard(expression):
        return False
    return bool(re.fullmatch(
        r"github\.event_name\s*==\s*'workflow_dispatch'\s*&&\s*"
        r"inputs\.confirm_release\s*==\s*'RELEASE'(?:\s*&&\s*.+)?",
        expression,
    ))


def validate_guard_parser():
    cases = (
        ("manual equality", ["    if: ${{ github.event_name == 'workflow_dispatch' }}"], True, False),
        (
            "release conjunction",
            ["    if: ${{ github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE' }}"],
            True,
            True,
        ),
        ("comment-only equality", ["    if: # github.event_name == 'workflow_dispatch'"], False, False),
        (
            "not-equal with comment decoy",
            ["    if: github.event_name != 'workflow_dispatch' # github.event_name == 'workflow_dispatch'"],
            False,
            False,
        ),
        (
            "negated equality",
            ["    if: ${{ !(github.event_name == 'workflow_dispatch') }}"],
            False,
            False,
        ),
        (
            "OR bypass",
            ["    if: ${{ github.event_name == 'workflow_dispatch' || github.ref == 'refs/heads/main' }}"],
            False,
            False,
        ),
        (
            "release comment decoy",
            ["    if: ${{ github.event_name == 'workflow_dispatch' }} # inputs.confirm_release == 'RELEASE'"],
            True,
            False,
        ),
    )
    for name, lines, expected_manual, expected_release in cases:
        expression = extract_job_if_expression(lines)
        actual_manual = is_hard_manual_dispatch_guard(expression)
        actual_release = is_hard_release_confirmation_guard(expression)
        if actual_manual != expected_manual:
            errors.append(
                f"manual guard parser regression ({name}): expected {expected_manual}, got {actual_manual}"
            )
        if actual_release != expected_release:
            errors.append(
                f"release guard parser regression ({name}): expected {expected_release}, got {actual_release}"
            )


validate_guard_parser()

if not WORKFLOWS.is_dir():
    errors.append("missing .github/workflows directory")
else:
    workflow_files = sorted(list(WORKFLOWS.glob("*.yml")) + list(WORKFLOWS.glob("*.yaml")))
    if not workflow_files:
        errors.append("no GitHub Actions workflows found")

    for path in workflow_files:
        text = path.read_text(encoding="utf-8")
        lines = text.splitlines()
        on_index = next((i for i, line in enumerate(lines) if re.match(r"^on\s*:\s*(?:#.*)?$", line)), None)
        if on_index is None:
            errors.append(f"{path.name}: top-level on: block is required")
            continue

        trigger_lines = []
        for line in lines[on_index + 1:]:
            if line.strip() and not line.startswith((" ", "\t", "#")):
                break
            trigger_lines.append(line)
        trigger_block = "\n".join(trigger_lines)

        if not re.search(r"(?m)^\s{2}workflow_dispatch\s*:", trigger_block):
            errors.append(f"{path.name}: workflow_dispatch must be the only trigger")

        trigger_names = []
        for line in trigger_lines:
            match = re.match(r"^\s{2}([A-Za-z0-9_-]+)\s*:", line)
            if match:
                trigger_names.append(match.group(1))
        disallowed = sorted({name for name in trigger_names if name != "workflow_dispatch"})
        if disallowed:
            errors.append(f"{path.name}: automatic/non-owner trigger(s) forbidden: {', '.join(disallowed)}")

        forbidden_anywhere = (
            "push", "pull_request", "pull_request_target", "schedule", "workflow_run",
            "workflow_call", "repository_dispatch", "release", "create", "delete",
            "deployment", "deployment_status", "check_run", "check_suite", "status",
            "page_build", "public", "issues", "issue_comment", "discussion",
            "discussion_comment", "fork", "gollum", "label", "merge_group",
            "milestone", "project", "project_card", "project_column", "registry_package",
            "watch"
        )
        for trigger in forbidden_anywhere:
            if re.search(rf"(?m)^\s{{2}}{re.escape(trigger)}\s*:", trigger_block):
                errors.append(f"{path.name}: forbidden trigger in on: block: {trigger}")

        job_blocks = collect_job_blocks(lines)
        if not job_blocks:
            errors.append(f"{path.name}: jobs: must contain at least one executable job")
        for job_name, job_lines in job_blocks:
            expression = extract_job_if_expression(job_lines)
            if not is_hard_manual_dispatch_guard(expression):
                errors.append(
                    f"{path.name}/{job_name}: job must hard-guard "
                    "github.event_name == 'workflow_dispatch' as the leading conjunction"
                )

        if path.name in RELEASE_WORKFLOWS:
            for token in ("confirm_release", "contents: write"):
                if token not in text:
                    errors.append(f"{path.name} missing explicit manual publish guard: {token}")
            release_job = next((block for name, block in job_blocks if name == "release"), None)
            release_expression = extract_job_if_expression(release_job) if release_job is not None else None
            if not is_hard_release_confirmation_guard(release_expression):
                errors.append(
                    f"{path.name}/release: publish job must hard-require "
                    "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'"
                )

policy = (ROOT / "CI_POLICY.md").read_text(encoding="utf-8") if (ROOT / "CI_POLICY.md").is_file() else ""
for token in ("workflow_dispatch", "manual-only", "explicitly requests", "MANUAL-BUILD-RELEASE"):
    if token not in policy:
        errors.append("CI_POLICY.md missing manual-only policy token: " + token)

readme = (ROOT / "README.md").read_text(encoding="utf-8") if (ROOT / "README.md").is_file() else ""
if "manual-only" not in readme or "workflow_dispatch" not in readme or "release-v25.yml" not in readme:
    errors.append("README.md must document manual-only Actions and the owner-approved release workflow")

print("QS3D manual-only GitHub Actions preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: every GitHub Actions workflow is workflow_dispatch-only, every job is independently "
    "hard-guarded to the manual event, and both release workflows require explicit RELEASE confirmation."
)
