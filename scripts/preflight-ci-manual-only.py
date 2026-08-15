#!/usr/bin/env python3
"""Validate the QS3D GitHub Actions ownership model.

The filename is retained for compatibility with existing workflows. The policy is no
longer "manual-only": exactly one validation workflow may run automatically on task
branches/PRs/main, while release workflows remain manual and the V25 cloud release
may be dispatched automatically only by the approved post-main dispatcher.
"""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
TASK_CI = "ci.yml"
AUTO_DISPATCHER = "dispatch-v25-cloud-after-main-integration.yml"
RELEASE_WORKFLOWS = {"release-v25.yml", "release-v25-cloud.yml", "release-v26.yml"}
errors = []


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


def extract_job_if_expression(job_lines):
    expressions = []
    for line in job_lines:
        match = re.match(r"^\s{4}if\s*:\s*(.*)$", line)
        if match:
            expressions.append(strip_yaml_inline_comment(match.group(1)).strip())
    if len(expressions) != 1:
        return None
    expression = expressions[0]
    if expression.startswith("${{") and expression.endswith("}}"):
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
    if expression is None or "||" in expression:
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


def workflow_contract(path):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    on_index = next((i for i, line in enumerate(lines) if re.match(r"^on\s*:\s*(?:#.*)?$", line)), None)
    if on_index is None:
        errors.append(f"{path.name}: top-level on: block is required")
        return text, {}, []

    trigger_lines = []
    for line in lines[on_index + 1:]:
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        trigger_lines.append(line)
    return text, extract_trigger_blocks(trigger_lines), collect_job_blocks(lines)


def require_tokens(label, text, tokens):
    for token in tokens:
        if token not in text:
            errors.append(f"{label}: missing required token: {token}")


if not WORKFLOWS.is_dir():
    errors.append("missing .github/workflows directory")
else:
    workflow_files = sorted(list(WORKFLOWS.glob("*.yml")) + list(WORKFLOWS.glob("*.yaml")))
    names = {path.name for path in workflow_files}
    for required in (TASK_CI, AUTO_DISPATCHER):
        if required not in names:
            errors.append(f"missing required workflow: {required}")

    for path in workflow_files:
        text, trigger_blocks, job_blocks = workflow_contract(path)
        trigger_names = set(trigger_blocks)
        if not job_blocks:
            errors.append(f"{path.name}: jobs: must contain at least one executable job")

        if path.name == TASK_CI:
            expected = {"workflow_dispatch", "push", "pull_request"}
            if trigger_names != expected:
                errors.append(f"{path.name}: task CI must expose exactly {sorted(expected)}; got {sorted(trigger_names)}")
            push_block = "\n".join(trigger_blocks.get("push", []))
            pr_block = "\n".join(trigger_blocks.get("pull_request", []))
            require_tokens(path.name + "/push", push_block, ("branches:", "- main", '"agent/**"', '"recovery/**"', '"integration/**"'))
            require_tokens(path.name + "/pull_request", pr_block, ("branches:", "- main"))
            require_tokens(path.name, text, (
                "contents: read",
                "cancel-in-progress: true",
                "github.event.pull_request.head.ref || github.ref_name",
                "github.event.pull_request.head.sha || github.sha",
                "Verify exact checkout SHA",
                "dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release",
                "QS3D.Core.SmokeTests.csproj",
            ))
            if "contents: write" in text:
                errors.append(f"{path.name}: task validation CI must never have contents: write")

        elif path.name == AUTO_DISPATCHER:
            expected = {"workflow_dispatch", "push"}
            if trigger_names != expected:
                errors.append(f"{path.name}: approved dispatcher must expose exactly workflow_dispatch + push; got {sorted(trigger_names)}")
            push_block = "\n".join(trigger_blocks.get("push", []))
            require_tokens(path.name + "/push", push_block, ("branches:", "- main", "paths:"))
            if "docs/**" in push_block or "README" in push_block or "AGENTS.md" in push_block:
                errors.append(f"{path.name}: docs/claim-only changes must not trigger automatic V25 cloud release")
            require_tokens(path.name, text, (
                "contents: read",
                "actions: write",
                "cancel-in-progress: true",
                "github.actor != 'github-actions[bot]'",
                "gh workflow run release-v25-cloud.yml",
                "--ref main",
                "confirm_release=RELEASE",
            ))
            if "contents: write" in text:
                errors.append(f"{path.name}: dispatcher must not have contents: write")
            dispatch_job = next((block for name, block in job_blocks if name == "dispatch"), None)
            expression = extract_job_if_expression(dispatch_job) if dispatch_job is not None else None
            if not is_hard_auto_dispatch_guard(expression):
                errors.append(f"{path.name}/dispatch: job must hard-require main and reject github-actions[bot] pushes")

        else:
            if trigger_names != {"workflow_dispatch"}:
                errors.append(
                    f"{path.name}: only {TASK_CI} and {AUTO_DISPATCHER} may use automatic triggers; got {sorted(trigger_names)}"
                )
            for job_name, job_lines in job_blocks:
                expression = extract_job_if_expression(job_lines)
                if not is_hard_manual_dispatch_guard(expression):
                    errors.append(f"{path.name}/{job_name}: manual workflow job must hard-guard workflow_dispatch")

        if path.name in RELEASE_WORKFLOWS:
            require_tokens(path.name, text, ("confirm_release", "contents: write"))
            release_job = next((block for name, block in job_blocks if name == "release"), None)
            release_expression = extract_job_if_expression(release_job) if release_job is not None else None
            if not is_hard_release_confirmation_guard(release_expression):
                errors.append(f"{path.name}/release: publish job must hard-require workflow_dispatch + RELEASE confirmation")

        if path.name == "release-v25-cloud.yml":
            require_tokens(path.name, text, (
                "source_sha:",
                "SOURCE_SHA: ${{ inputs.source_sha || github.sha }}",
                "ref: ${{ inputs.source_sha || github.sha }}",
                "refs/heads/main",
                "git merge-base --is-ancestor $sourceSha origin/main",
                "-DispatchSha $env:SOURCE_SHA",
            ))

policy_path = ROOT / "CI_POLICY.md"
policy = policy_path.read_text(encoding="utf-8") if policy_path.is_file() else ""
for token in (
    "per-agent task CI",
    "exact head SHA",
    "agent/**",
    "recovery/**",
    "integration/**",
    "release-v25-cloud.yml",
    "main-only release",
    "must not report the task completed",
):
    if token not in policy:
        errors.append("CI_POLICY.md missing task-CI policy token: " + token)

registration_path = ROOT / "docs/AGENT-WORK-REGISTRATION.md"
registration = registration_path.read_text(encoding="utf-8") if registration_path.is_file() else ""
for token in (
    "agent/<agent-id>/<scope>",
    "integration/<batch-id>",
    "exact head SHA",
    "CI_GREEN",
    "must not stop as completed",
    AUTO_DISPATCHER,
):
    if token not in registration:
        errors.append("AGENT-WORK-REGISTRATION.md missing task-CI token: " + token)

print("QS3D GitHub Actions policy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: per-agent/task validation CI is automatic on task branches, PRs and main; "
    "release workflows remain manual/main-scoped, with only the approved post-main V25 dispatcher allowed to publish automatically."
)
