#!/usr/bin/env python3
"""Fail closed if Hybrid stale-Shared cancellation is not bound to Actions-write auth."""

from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"


def validate(text: str) -> list[str]:
    errors: list[str] = []

    permissions = re.search(
        r"(?ms)^permissions:\s*\n(?P<body>(?:^[ \t]+[^\n]*\n)+)", text
    )
    permissions_body = permissions.group("body") if permissions else ""
    if not re.search(r"(?m)^\s{2}actions:\s*read\s*$", permissions_body):
        errors.append("top-level workflow Actions permission must remain read-only")

    refresh = re.search(r"(?ms)^  refresh-branches:\s*\n(?P<body>.*)\Z", text)
    refresh_body = refresh.group("body") if refresh else ""
    if not refresh_body:
        errors.append("refresh-branches job is missing")
        return errors

    if not re.search(
        r"(?m)^    if:\s*\$\{\{\s*github\.event_name == 'push' && github\.ref == 'refs/heads/main'\s*\}\}\s*$",
        refresh_body,
    ):
        errors.append("Actions-write refresh job must remain restricted to protected-main push events")

    refresh_permissions = re.search(
        r"(?ms)^    permissions:\s*\n(?P<body>(?:^      [^\n]*\n)+)", refresh_body
    )
    refresh_permissions_body = (
        refresh_permissions.group("body") if refresh_permissions else ""
    )
    if not re.search(
        r"(?m)^      actions:\s*write\s*$", refresh_permissions_body
    ):
        errors.append("refresh-branches job must grant actions: write")
    unexpected_write = re.findall(
        r"(?m)^      ([A-Za-z-]+):\s*write\s*$", refresh_permissions_body
    )
    if any(name != "actions" for name in unexpected_write):
        errors.append("refresh-branches job must not broaden write permission beyond Actions")

    refresh_env = re.search(
        r"(?ms)^\s{8}env:\s*\n(?P<body>(?:^\s{10}[^\n]*\n)+)", refresh_body
    )
    refresh_env_body = refresh_env.group("body") if refresh_env else ""
    if not re.search(
        r"(?m)^\s{10}GH_TOKEN:\s*\$\{\{\s*secrets\.QS3D_AUTOMERGE_TOKEN\s*\}\}\s*$",
        refresh_env_body,
    ):
        errors.append("refresh PR mutations must remain bound to QS3D_AUTOMERGE_TOKEN")
    if not re.search(
        r"(?m)^\s{10}ACTIONS_TOKEN:\s*\$\{\{\s*github\.token\s*\}\}\s*$",
        refresh_env_body,
    ):
        errors.append("refresh job must expose github.token separately as ACTIONS_TOKEN")

    cancel = re.search(
        r"(?ms)for run_id in \"\$\{active_run_ids\[@\]\}\"; do(?P<body>.*?)^\s{14}done\s*$",
        refresh_body,
    )
    cancel_body = cancel.group("body") if cancel else ""
    if not cancel_body:
        errors.append("active Shared-CI cancellation loop is missing")
        return errors

    cancel_call = re.compile(
        r'GH_TOKEN="\$ACTIONS_TOKEN"\s+gh api --method POST '
        r'"repos/\$\{GITHUB_REPOSITORY\}/actions/runs/\$\{run_id\}/cancel" --silent'
    )
    if not cancel_call.search(cancel_body):
        errors.append("cancel POST must be executed with GH_TOKEN=$ACTIONS_TOKEN")

    if re.search(
        r'(?m)^\s*if cancel_output="\$\(gh api --method POST '
        r'"repos/\$\{GITHUB_REPOSITORY\}/actions/runs/\$\{run_id\}/cancel"',
        cancel_body,
    ):
        errors.append("cancel POST must not inherit the automerge PAT implicitly")

    if "HTTP (409|422)" not in cancel_body:
        errors.append("cancel race handling for 409/422 must remain fail-safe")
    if "exit 1" not in cancel_body:
        errors.append("unexpected cancellation failures must remain fail-closed")

    return errors


def self_test(good: str) -> list[str]:
    errors: list[str] = []
    if validate(good):
        return errors

    mutants = {
        "top-level-actions-write": good.replace("  actions: read", "  actions: write", 1),
        "job-actions-read": good.replace("      actions: write", "      actions: read", 1),
        "pr-event-write-scope": good.replace(
            "github.event_name == 'push' && github.ref == 'refs/heads/main'",
            "github.event_name == 'pull_request'",
            1,
        ),
        "cancel-inherits-pat": good.replace(
            'GH_TOKEN="$ACTIONS_TOKEN" gh api --method POST',
            "gh api --method POST",
            1,
        ),
        "decoy-actions-token": good.replace(
            'GH_TOKEN="$ACTIONS_TOKEN" gh api --method POST',
            'echo "GH_TOKEN=$ACTIONS_TOKEN gh api --method POST"; gh api --method POST',
            1,
        ),
        "wrong-actions-secret": good.replace(
            "ACTIONS_TOKEN: ${{ github.token }}",
            "ACTIONS_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
            1,
        ),
    }
    for name, mutant in mutants.items():
        if not validate(mutant):
            errors.append(f"mutation escaped guard: {name}")
    return errors


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    errors = validate(text)
    if not errors:
        errors.extend(self_test(text))
    if errors:
        print("ERROR: Hybrid Shared-CI cancellation token preflight failed closed:")
        for error in errors:
            print(f" - {error}")
        return 1
    print(
        "PASS: Hybrid stale-Shared cancellation is bound to a push-only, job-scoped "
        "github.token with actions: write and remains fail-closed."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
