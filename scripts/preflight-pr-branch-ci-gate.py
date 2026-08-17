#!/usr/bin/env python3
"""Fail closed when an internal watched PR is opened without prior branch CI success."""

from __future__ import annotations

from datetime import datetime, timezone
import json
import os
from pathlib import Path
import sys
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

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


def parse_github_time(value: Any) -> datetime:
    if not isinstance(value, str) or not value:
        raise ValueError("GitHub timestamp is missing or not a string")
    if value != value.strip():
        raise ValueError("GitHub timestamp must be canonical without leading/trailing whitespace")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ValueError(f"GitHub timestamp is invalid: {value!r}") from exc
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise ValueError(f"GitHub timestamp must include an explicit timezone offset: {value!r}")
    return parsed.astimezone(timezone.utc)


def parse_attempt(value: Any) -> int | None:
    if value is None:
        return 1
    if isinstance(value, bool):
        return None
    if isinstance(value, int):
        return value if value >= 1 else None
    if isinstance(value, str) and value.isascii() and value.isdigit():
        parsed = int(value)
        return parsed if parsed >= 1 else None
    return None


def is_allowed_internal_ref(head_ref: str) -> bool:
    return bool(head_ref) and head_ref.startswith(ALLOWED_INTERNAL_PREFIXES)


def run_qualifies(run: dict[str, Any], head_ref: str, head_sha: str, pr_created_at: datetime) -> bool:
    if not isinstance(run, dict):
        return False
    if run.get("event") != "push":
        return False
    if run.get("head_branch") != head_ref or run.get("head_sha") != head_sha:
        return False
    if run.get("status") != "completed" or run.get("conclusion") != "success":
        return False
    if parse_attempt(run.get("run_attempt")) != 1:
        return False
    if run.get("path") not in (None, ".github/workflows/ci.yml"):
        return False
    try:
        created_at = parse_github_time(run.get("created_at"))
        updated_at = parse_github_time(run.get("updated_at"))
    except ValueError:
        return False
    return created_at <= pr_created_at and updated_at <= pr_created_at


def qualify_pr_payload(event: dict[str, Any], repository_hint: str) -> tuple[str, str, str, datetime] | None:
    if event.get("action") != "opened":
        raise RuntimeError("live PR branch CI admission check requires pull_request action=opened")

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
            f"internal watched PR head '{head_ref}' is outside automatic branch-CI namespaces; "
            "use agent/** or integration/** and obtain exact-head branch CI SUCCESS before opening the PR"
        )

    head_sha = require_string(head.get("sha", ""), "pull_request.head.sha")
    if len(head_sha) != 40 or any(ch not in "0123456789abcdefABCDEF" for ch in head_sha):
        raise RuntimeError(f"invalid PR head SHA: {head_sha!r}")

    try:
        created_at = parse_github_time(pr.get("created_at"))
    except ValueError as exc:
        raise RuntimeError(str(exc)) from exc
    return head_ref, head_sha, base_repo, created_at


def validate_self_tests() -> list[str]:
    errors: list[str] = []
    if not is_allowed_internal_ref("agent/worker/fix"):
        errors.append("agent/** branch must be admitted")
    if not is_allowed_internal_ref("integration/batch"):
        errors.append("integration/** branch must be admitted")
    for rejected in ("fix/bug", "feat/ui", "ui/parity", "main", ""):
        if is_allowed_internal_ref(rejected):
            errors.append(f"non-watched task branch must be rejected: {rejected!r}")

    opened = parse_github_time("2026-08-16T10:00:00Z")
    base = {
        "event": "push",
        "head_branch": "agent/test",
        "head_sha": "a" * 40,
        "status": "completed",
        "conclusion": "success",
        "run_attempt": 1,
        "path": ".github/workflows/ci.yml",
        "created_at": "2026-08-16T09:50:00Z",
        "updated_at": "2026-08-16T09:59:00Z",
    }
    if not run_qualifies(base, "agent/test", "a" * 40, opened):
        errors.append("successful automatic exact-head branch run before PR creation must qualify")

    mutations = (
        ("late completion", {"updated_at": "2026-08-16T10:01:00Z"}),
        ("manual rerun attempt", {"run_attempt": 2}),
        ("malformed attempt", {"run_attempt": "not-an-int"}),
        ("boolean attempt", {"run_attempt": True}),
        ("wrong branch", {"head_branch": "agent/other"}),
        ("wrong sha", {"head_sha": "b" * 40}),
        ("PR event", {"event": "pull_request"}),
        ("failed run", {"conclusion": "failure"}),
        ("in-progress run", {"status": "in_progress", "conclusion": None}),
        ("malformed created timestamp", {"created_at": []}),
        ("malformed updated timestamp", {"updated_at": {}}),
        ("naive created timestamp", {"created_at": "2026-08-16T09:50:00"}),
        ("naive updated timestamp", {"updated_at": "2026-08-16T09:59:00"}),
        ("padded created timestamp", {"created_at": " 2026-08-16T09:50:00Z"}),
    )
    for name, patch in mutations:
        candidate = dict(base)
        candidate.update(patch)
        if run_qualifies(candidate, "agent/test", "a" * 40, opened):
            errors.append(f"{name} must not qualify as pre-PR branch evidence")
    return errors


def validate_static_contract() -> list[str]:
    errors: list[str] = []
    if not CI_WORKFLOW.is_file():
        return ["missing .github/workflows/ci.yml"]
    text = CI_WORKFLOW.read_text(encoding="utf-8")
    required = (
        "actions: read",
        "PR branch CI admission gate",
        "github.event.action == 'opened'",
        "github.actor != 'dependabot[bot]'",
        "github.event.pull_request.head.repo.full_name == github.repository",
        "steps.scope.outputs.source_validation == 'true'",
        'QS3D_PR_BRANCH_CI_RUNTIME: "1"',
        "GITHUB_TOKEN: ${{ github.token }}",
        "python scripts/preflight-pr-branch-ci-gate.py",
    )
    for token in required:
        if token not in text:
            errors.append(f"ci.yml missing branch-CI admission contract token: {token}")
    return errors


def read_event() -> dict[str, Any]:
    event_path = os.environ.get("GITHUB_EVENT_PATH", "").strip()
    if not event_path:
        raise RuntimeError("GITHUB_EVENT_PATH is required for pull_request admission validation")
    path = Path(event_path)
    if not path.is_file():
        raise RuntimeError(f"GitHub event payload not found: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"could not read GitHub event payload: {exc}") from exc
    return require_object(payload, "GitHub event payload")


def fetch_runs(repository: str, head_sha: str, token: str) -> list[dict[str, Any]]:
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
    query = urlencode({"head_sha": head_sha, "event": "push", "per_page": "100"})
    url = f"{api_url}/repos/{repository}/actions/workflows/ci.yml/runs?{query}"
    request = Request(
        url,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "qs3d-pre-pr-branch-ci-gate",
        },
    )
    try:
        with urlopen(request, timeout=20) as response:
            payload = json.load(response)
    except HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"GitHub Actions lookup failed with HTTP {exc.code}: {detail[:500]}") from exc
    except URLError as exc:
        raise RuntimeError(f"GitHub Actions lookup failed: {exc.reason}") from exc
    except (OSError, UnicodeError, json.JSONDecodeError, ValueError) as exc:
        raise RuntimeError(f"GitHub Actions lookup returned unreadable JSON: {exc}") from exc

    payload_obj = require_object(payload, "GitHub Actions lookup payload")
    runs = payload_obj.get("workflow_runs")
    if not isinstance(runs, list):
        raise RuntimeError("GitHub Actions lookup returned an invalid workflow_runs payload")
    if len(runs) > 100:
        raise RuntimeError("GitHub Actions lookup returned more workflow runs than requested")
    result = []
    for index, run in enumerate(runs):
        if not isinstance(run, dict):
            raise RuntimeError(f"GitHub Actions workflow_runs[{index}] must be a JSON object")
        result.append(run)
    return result


def main() -> int:
    errors = validate_self_tests() + validate_static_contract()
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if os.environ.get(RUNTIME_ENV) != "1":
        print("PR branch CI admission gate: static contract/self-tests PASS; live admission check not requested.")
        return 0

    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        return fail("live PR branch CI admission check requires a pull_request event")

    try:
        event = read_event()
        qualified = qualify_pr_payload(event, os.environ.get("GITHUB_REPOSITORY", "").strip())
    except RuntimeError as exc:
        return fail(str(exc))

    if qualified is None:
        pr = optional_object(event.get("pull_request"), "pull_request payload")
        user = optional_object(pr.get("user"), "pull_request.user")
        if user.get("login") == DEPENDABOT_LOGIN:
            print("PR branch CI admission gate: Dependabot standing exception applies.")
        else:
            print("PR branch CI admission gate: external/fork PR; internal task-branch admission rule not applied.")
        return 0

    head_ref, head_sha, base_repo, pr_created_at = qualified
    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not token:
        return fail("GITHUB_TOKEN is required to verify exact pre-PR branch CI evidence")

    repository = os.environ.get("GITHUB_REPOSITORY", "").strip() or base_repo
    try:
        runs = fetch_runs(repository, head_sha, token)
    except RuntimeError as exc:
        return fail(str(exc))

    qualifying = [run for run in runs if run_qualifies(run, head_ref, head_sha, pr_created_at)]
    if qualifying:
        run = max(qualifying, key=lambda item: str(item.get("updated_at") or ""))
        print(
            "PR branch CI admission gate PASS: "
            f"run {run.get('id')} completed SUCCESS on {head_ref}@{head_sha} before PR creation."
        )
        return 0

    observed = []
    for run in runs[:10]:
        if run.get("head_branch") != head_ref:
            continue
        observed.append(
            f"id={run.get('id')} status={run.get('status')} conclusion={run.get('conclusion')} "
            f"attempt={run.get('run_attempt')} updated_at={run.get('updated_at')}"
        )
    evidence = "; ".join(observed) if observed else "no matching branch push runs returned"
    return fail(
        f"no automatic attempt-1 ci.yml branch-push run completed SUCCESS on exact head "
        f"{head_ref}@{head_sha} before PR creation at {pr_created_at.isoformat()}; observed: {evidence}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
