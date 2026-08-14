#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
AUTO_DISPATCHER = "dispatch-v25-cloud-after-main-integration.yml"
RELEASE_WORKFLOWS = {"release-v25.yml", "release-v25-cloud.yml", "release-v26.yml"}
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


def is_hard_auto_dispatch_guard(expression):
    if expression is None or "||" in expression:
        return False
    return bool(re.fullmatch(
        r"github\.ref\s*==\s*'refs/heads/main'\s*&&\s*"
        r"github\.actor\s*!=\s*'github-actions\[bot\]'",
        expression,
    ))


def parse_trigger_name(line):
    match = re.match(
        r"^\s{2}(?:\"([A-Za-z0-9_-]+)\"|'([A-Za-z0-9_-]+)'|([A-Za-z0-9_-]+))\s*:",
        line,
    )
    if not match:
        return None
    return next(value for value in match.groups() if value is not None)


def extract_trigger_blocks(trigger_lines):
    blocks = {}
    current = None
    for line in trigger_lines:
        name = parse_trigger_name(line)
        if name is not None:
            current = name
            blocks[current] = [line]
            continue
        if current is not None:
            blocks[current].append(line)
    return blocks


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
            errors.append(f"manual guard parser regression ({name})")
        if actual_release != expected_release:
            errors.append(f"release guard parser regression ({name})")

    auto_good = extract_job_if_expression([
        "    if: ${{ github.ref == 'refs/heads/main' && github.actor != 'github-actions[bot]' }}"
    ])
    auto_bad = extract_job_if_expression([
        "    if: ${{ github.ref == 'refs/heads/main' || github.actor != 'github-actions[bot]' }}"
    ])
    if not is_hard_auto_dispatch_guard(auto_good) or is_hard_auto_dispatch_guard(auto_bad):
        errors.append("automatic dispatcher guard parser regression")

    if parse_trigger_name('  "push":') != "push" or parse_trigger_name("  workflow_dispatch:") != "workflow_dispatch":
        errors.append("trigger parser must support quoted/unquoted trigger keys")


validate_guard_parser()

if not WORKFLOWS.is_dir():
    errors.append("missing .github/workflows directory")
else:
    workflow_files = sorted(list(WORKFLOWS.glob("*.yml")) + list(WORKFLOWS.glob("*.yaml")))
    if not workflow_files:
        errors.append("no GitHub Actions workflows found")

    if not (WORKFLOWS / AUTO_DISPATCHER).is_file():
        errors.append(f"missing owner-approved automatic dispatcher: {AUTO_DISPATCHER}")

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
        trigger_blocks = extract_trigger_blocks(trigger_lines)
        trigger_names = set(trigger_blocks)
        job_blocks = collect_job_blocks(lines)

        if not job_blocks:
            errors.append(f"{path.name}: jobs: must contain at least one executable job")

        if path.name == AUTO_DISPATCHER:
            expected = {"workflow_dispatch", "push"}
            if trigger_names != expected:
                errors.append(
                    f"{path.name}: approved dispatcher must expose exactly workflow_dispatch + push; got {sorted(trigger_names)}"
                )

            push_block = "\n".join(trigger_blocks.get("push", []))
            for token in ("branches:", "- main", "paths:"):
                if token not in push_block:
                    errors.append(f"{path.name}: push trigger missing required main/path scope token: {token}")
            if "docs/**" in push_block or "README" in push_block or "AGENTS.md" in push_block:
                errors.append(f"{path.name}: docs/claim-only changes must not trigger automatic V25 cloud CI")

            for token in (
                "contents: read",
                "actions: write",
                "cancel-in-progress: true",
                "github.actor != 'github-actions[bot]'",
                "gh workflow run release-v25-cloud.yml",
                "--ref main",
                "confirm_release=RELEASE",
                "git fetch --force --tags origin",
                'series_prefix="v0.1.0-preview."',
                'git tag --list "${series_prefix}*"',
                "ordinal > 65535",
                "max_preview >= 65535",
                "preview=$((max_preview + 1))",
            ):
                if token not in text:
                    errors.append(f"{path.name}: approved dispatcher contract missing token: {token}")
            for forbidden_token in ("GITHUB_RUN_NUMBER", "10000 +"):
                if forbidden_token in text:
                    errors.append(
                        f"{path.name}: dispatcher must not derive public preview ordinals from Actions run numbering: {forbidden_token}"
                    )
            if "contents: write" in text:
                errors.append(f"{path.name}: dispatcher must not have contents: write")
            if re.search(r"gh\s+workflow\s+run\s+(?!release-v25-cloud\.yml)", text):
                errors.append(f"{path.name}: dispatcher may target only release-v25-cloud.yml")

            dispatch_job = next((block for name, block in job_blocks if name == "dispatch"), None)
            expression = extract_job_if_expression(dispatch_job) if dispatch_job is not None else None
            if not is_hard_auto_dispatch_guard(expression):
                errors.append(
                    f"{path.name}/dispatch: job must hard-require main and reject github-actions[bot] pushes"
                )
            for job_name, _ in job_blocks:
                if job_name != "dispatch":
                    errors.append(f"{path.name}: unexpected automatic dispatcher job: {job_name}")
        else:
            if trigger_names != {"workflow_dispatch"}:
                errors.append(
                    f"{path.name}: only {AUTO_DISPATCHER} may use an automatic trigger; got {sorted(trigger_names)}"
                )
            for job_name, job_lines in job_blocks:
                expression = extract_job_if_expression(job_lines)
                if not is_hard_manual_dispatch_guard(expression):
                    errors.append(
                        f"{path.name}/{job_name}: job must hard-guard github.event_name == 'workflow_dispatch'"
                    )

        if path.name in RELEASE_WORKFLOWS:
            for token in ("confirm_release", "contents: write"):
                if token not in text:
                    errors.append(f"{path.name} missing explicit publish guard: {token}")
            release_job = next((block for name, block in job_blocks if name == "release"), None)
            release_expression = extract_job_if_expression(release_job) if release_job is not None else None
            if not is_hard_release_confirmation_guard(release_expression):
                errors.append(
                    f"{path.name}/release: publish job must hard-require workflow_dispatch + RELEASE confirmation"
                )

policy_path = ROOT / "CI_POLICY.md"
policy = policy_path.read_text(encoding="utf-8") if policy_path.is_file() else ""
for token in (
    "manual-only by default",
    "automatic post-integration",
    AUTO_DISPATCHER,
    "release-v25-cloud.yml",
    "integration/<batch-id>",
    "ALL MERGED TO MAIN",
):
    if token not in policy:
        errors.append("CI_POLICY.md missing integration/CI policy token: " + token)

registration_path = ROOT / "docs/AGENT-WORK-REGISTRATION.md"
registration = registration_path.read_text(encoding="utf-8") if registration_path.is_file() else ""
for token in (
    "agent/<agent-id>/<scope>",
    "integration/<batch-id>",
    "Stop before merge unless",
    "No implicit `main` authorization",
    "never update the `main` ref directly",
    "There is no docs-only exception",
    "ALL MERGED TO MAIN",
    AUTO_DISPATCHER,
):
    if token not in registration:
        errors.append("AGENT-WORK-REGISTRATION.md missing work-registration/main-authorization token: " + token)

print("QS3D GitHub Actions policy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: Actions are manual-only by default, exactly one owner-approved post-integration main dispatcher is allowed, "
    "that dispatcher can target only release-v25-cloud.yml, and release workflows retain explicit RELEASE confirmation."
)
