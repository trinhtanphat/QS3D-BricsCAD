#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
errors = []

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

        if "github.event_name == 'workflow_dispatch'" not in text:
            errors.append(f"{path.name}: every executable workflow must hard-guard jobs to workflow_dispatch event")

        if path.name == "release-v25.yml":
            for token in ("confirm_release", "inputs.confirm_release == 'RELEASE'", "contents: write"):
                if token not in text:
                    errors.append("release-v25.yml missing explicit manual publish guard: " + token)

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

print("PASS: every GitHub Actions workflow is workflow_dispatch-only, job-guarded to the manual event, and release publication requires explicit RELEASE confirmation.")
