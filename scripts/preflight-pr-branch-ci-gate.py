#!/usr/bin/env python3
"""Validate internal PR identity without turning branch-CI timing into a permanent merge blocker.

Automatic branch CI remains useful early evidence, but protected PR `preflight` and `core`
are the authoritative merge-candidate gates. A PR must not be invalidated merely because
an automatic branch run is queued/running or completes after the PR was opened.
"""

from __future__ import annotations

import json
import os
from pathlib import Path
import sys
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
CI_WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
ALLOWED_INTERNAL_PREFIXES = ("agent/", "integration/")
DEPENDABOT_LOGIN = "dependabot[bot]"
RUNTIME_ENV = "QS3D_PR_BRANCH_CI_RUNTIME"


def fail(message: str) -> int:
    print(f"ERROR: {message}", file=sys.stderr)
    return 1


def require_object(value: Any, label: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise RuntimeError(f"{label} must be a JSON object")
    return value


def optional_object(value: Any, label: str) -> dict[str, Any]:
    if value is None:
        return {}
    return require_object(value, label)


def require_string(value: Any, label: str, *, allow_empty: bool = False) -> str:
    if not isinstance(value, str):
        raise RuntimeError(f"{label} must be a string")
    if value != value.strip():
        raise RuntimeError(f"{label} must be canonical without leading/trailing whitespace")
    if not allow_empty and not value:
        raise RuntimeError(f"{label} must not be empty")
    return value


def is_allowed_internal_ref(head_ref: str) -> bool:
    return bool(head_ref) and head_ref.startswith(ALLOWED_INTERNAL_PREFIXES)


def qualify_pr_payload(event: dict[str, Any], repository_hint: str) -> tuple[str, str, str] | None:
    if event.get("action") != "opened":
        raise RuntimeError("live PR identity check requires pull_request action=opened")

    pr = require_object(event.get("pull_request"), "pull_request payload")
    author_obj = optional_object(pr.get("user"), "pull_request.user")
    author = require_string(author_obj.get("login", ""), "pull_request.user.login", allow_empty=True)
    if author == DEPENDABOT_LOGIN:
        return None

    head = require_object(pr.get("head"), "pull_request.head")
    base = require_object(pr.get("base"), "pull_request.base")
    head_repo_obj = require_object(head.get("repo"), "pull_request.head.repo")
    base_repo_obj = optional_object(base.get("repo"), "pull_request.base.repo")

    head_repo = require_string(head_repo_obj.get("full_name", ""), "pull_request.head.repo.full_name")
    base_repo_value = base_repo_obj.get("full_name") or repository_hint
    base_repo = require_string(base_repo_value, "pull_request.base.repo.full_name")
    if head_repo.casefold() != base_repo.casefold():
        return None

    head_ref = require_string(head.get("ref", ""), "pull_request.head.ref")
    if not is_allowed_internal_ref(head_ref):
        raise RuntimeError(
            f"internal PR head '{head_ref}' is outside automatic branch-CI namespaces; "
            "use agent/** or integration/** so the repository receives automatic branch validation"
        )

    head_sha = require_string(head.get("sha", ""), "pull_request.head.sha")
    if len(head_sha) != 40 or any(ch not in "0123456789abcdefABCDEF" for ch in head_sha):
        raise RuntimeError(f"invalid PR head SHA: {head_sha!r}")

    return head_ref, head_sha, base_repo


def validate_self_tests() -> list[str]:
    errors: list[str] = []
    if not is_allowed_internal_ref("agent/worker/fix"):
        errors.append("agent/** branch must be admitted")
    if not is_allowed_internal_ref("integration/batch"):
        errors.append("integration/** branch must be admitted")
    for rejected in ("fix/bug", "feat/ui", "main", ""):
        if is_allowed_internal_ref(rejected):
            errors.append(f"non-watched task branch must be rejected: {rejected!r}")
    return errors


def validate_static_contract() -> list[str]:
    errors: list[str] = []
    if not CI_WORKFLOW.is_file():
        return ["missing .github/workflows/ci.yml"]
    text = CI_WORKFLOW.read_text(encoding="utf-8")
    required = (
        "PR branch CI admission gate",
        "github.event.action == 'opened'",
        "github.actor != 'dependabot[bot]'",
        "github.event.pull_request.head.repo.full_name == github.repository",
        'QS3D_PR_BRANCH_CI_RUNTIME: "1"',
        "python scripts/preflight-pr-branch-ci-gate.py",
    )
    for token in required:
        if token not in text:
            errors.append(f"ci.yml missing PR identity/provenance contract token: {token}")
    return errors


def read_event() -> dict[str, Any]:
    event_path = os.environ.get("GITHUB_EVENT_PATH", "").strip()
    if not event_path:
        raise RuntimeError("GITHUB_EVENT_PATH is required for pull_request identity validation")
    path = Path(event_path)
    if not path.is_file():
        raise RuntimeError(f"GitHub event payload not found: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"could not read GitHub event payload: {exc}") from exc
    return require_object(payload, "GitHub event payload")


def main() -> int:
    errors = validate_self_tests() + validate_static_contract()
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if os.environ.get(RUNTIME_ENV) != "1":
        print("PR branch-CI identity guard: static contract/self-tests PASS; live PR check not requested.")
        return 0

    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        return fail("live PR identity check requires a pull_request event")

    try:
        event = read_event()
        qualified = qualify_pr_payload(event, os.environ.get("GITHUB_REPOSITORY", "").strip())
    except RuntimeError as exc:
        return fail(str(exc))

    if qualified is None:
        pr = optional_object(event.get("pull_request"), "pull_request payload")
        user = optional_object(pr.get("user"), "pull_request.user")
        if user.get("login") == DEPENDABOT_LOGIN:
            print("PR branch-CI identity guard PASS: Dependabot standing exception applies.")
        else:
            print("PR branch-CI identity guard PASS: external/fork PR; internal carrier identity rule not applied.")
        return 0

    head_ref, head_sha, _base_repo = qualified
    print(
        "PR branch-CI identity guard PASS: "
        f"internal carrier {head_ref}@{head_sha} is eligible for automatic branch validation. "
        "Branch-CI completion time is advisory and MUST NOT invalidate or force replacement of this PR; "
        "protected current-candidate preflight/core remain the merge gate."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
