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


def parse_github_time(value: str) -> datetime:
    if not value:
        raise ValueError("GitHub timestamp is missing")
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def require_mapping(value: Any, label: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def optional_mapping(value: Any, label: str) -> dict[str, Any]:
    if value is None:
        return {}
    return require_mapping(value, label)


def text_field(mapping: dict[str, Any], key: str, label: str) -> str:
    value = mapping.get(key)
    if value is None:
        return ""
    if not isinstance(value, str):
        raise ValueError(f"{label} must be a string")
    return value.strip()


def is_allowed_internal_ref(head_ref: str) -> bool:
    return bool(head_ref) and head_ref.startswith(ALLOWED_INTERNAL_PREFIXES)


def run_attempt_is_first(value: Any) -> bool:
    if value is None:
        return True
    if isinstance(value, bool):
        return False
    if isinstance(value, int):
        return value == 1
    if isinstance(value, str):
        stripped = value.strip()
        return stripped == "1"
    return False


def run_qualifies(run: dict[str, Any], head_ref: str, head_sha: str, pr_created_at: datetime) -> bool:
    if run.get("event") != "push":
        return False
    if run.get("head_branch") != head_ref or run.get("head_sha") != head_sha:
        return False
    if run.get("status") != "completed" or run.get("conclusion") != "success":
        return False
    if not run_attempt_is_first(run.get("run_attempt")):
        return False
    if run.get("path") not in (None, ".github/workflows/ci.yml"):
        return False
    try:
        created_at = parse_github_time(str(run.get("created_at") or ""))
        updated_at = parse_github_time(str(run.get("updated_at") or ""))
    except (TypeError, ValueError):
        return False
    return created_at <= pr_created_at and updated_at <= pr_created_at


def parse_workflow_runs_payload(payload: Any) -> list[dict[str, Any]]:
    root = require_mapping(payload, "GitHub Actions lookup payload")
    runs = root.get("workflow_runs")
    if not isinstance(runs, list):
        raise ValueError("GitHub Actions lookup returned an invalid workflow_runs payload")
    parsed: list[dict[str, Any]] = []
    for index, run in enumerate(runs):
        if not isinstance(run, dict):
            raise ValueError(f"GitHub Actions workflow_runs[{index}] must be a JSON object")
        parsed.append(run)
    return parsed


def extract_pr_context(event: dict[str, Any], repository_fallback: str) -> dict[str, Any]:
    if event.get("action") != "opened":
        raise ValueError("live PR branch CI admission check requires pull_request action=opened")

    pr = require_mapping(event.get("pull_request"), "pull_request payload")
    user = optional_mapping(pr.get("user"), "pull_request.user")
    author = text_field(user, "login", "pull_request.user.login")
    if author == DEPENDABOT_LOGIN:
        return {"dependabot": True}

    head = require_mapping(pr.get("head"), "pull_request.head")
    base = require_mapping(pr.get("base"), "pull_request.base")
    head_repo = optional_mapping(head.get("repo"), "pull_request.head.repo")
    base_repo = optional_mapping(base.get("repo"), "pull_request.base.repo")
    head_repo_name = text_field(head_repo, "full_name", "pull_request.head.repo.full_name")
    base_repo_name = text_field(base_repo, "full_name", "pull_request.base.repo.full_name") or repository_fallback.strip()
    if not head_repo_name or not base_repo_name:
        raise ValueError("could not resolve PR head/base repository identity")

    head_ref = text_field(head, "ref", "pull_request.head.ref")
    head_sha = text_field(head, "sha", "pull_request.head.sha")
    created_at_raw = text_field(pr, "created_at", "pull_request.created_at")
    pr_created_at = parse_github_time(created_at_raw)

    return {
        "dependabot": False,
        "external": head_repo_name.casefold() != base_repo_name.casefold(),
        "head_ref": head_ref,
        "head_sha": head_sha,
        "head_repo": head_repo_name,
        "base_repo": base_repo_name,
        "created_at": pr_created_at,
        "created_at_raw": created_at_raw,
    }


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
        "head_sha": "abc123",
        "status": "completed",
        "conclusion": "success",
        "run_attempt": 1,
        "path": ".github/workflows/ci.yml",
        "created_at": "2026-08-16T09:50:00Z",
        "updated_at": "2026-08-16T09:59:00Z",
    }
    if not run_qualifies(base, "agent/test", "abc123", opened):
        errors.append("successful automatic exact-head branch run before PR creation must qualify")

    mutations = (
        ("late completion", {"updated_at": "2026-08-16T10:01:00Z"}),
        ("manual rerun attempt", {"run_attempt": 2}),
        ("malformed rerun attempt", {"run_attempt": {"attempt": 1}}),
        ("wrong branch", {"head_branch": "agent/other"}),
        ("wrong sha", {"head_sha": "def456"}),
        ("PR event", {"event": "pull_request"}),
        ("failed run", {"conclusion": "failure"}),
        ("in-progress run", {"status": "in_progress", "conclusion": None}),
    )
    for name, patch in mutations:
        candidate = dict(base)
        candidate.update(patch)
        if run_qualifies(candidate, "agent/test", "abc123", opened):
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
        raise RuntimeError(f"GitHub event payload could not be decoded: {exc}") from exc
    try:
        return require_mapping(payload, "GitHub event payload")
    except ValueError as exc:
        raise RuntimeError(str(exc)) from exc


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
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise RuntimeError(f"GitHub Actions lookup returned unreadable JSON: {exc}") from exc

    try:
        return parse_workflow_runs_payload(payload)
    except ValueError as exc:
        raise RuntimeError(str(exc)) from exc


def main() -> int:
    errors = validate_self_tests() + validate_static_contract()
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    # This file is auto-discovered by preflight-all.py. Aggregate execution must
    # remain hermetic and exercise only self-tests/static workflow contracts.
    # The dedicated PR admission workflow step opts into the live GitHub API
    # check explicitly and is the only place that receives a token.
    if os.environ.get(RUNTIME_ENV) != "1":
        print("PR branch CI admission gate: static contract/self-tests PASS; live admission check not requested.")
        return 0

    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        return fail("live PR branch CI admission check requires a pull_request event")

    try:
        event = read_event()
        context = extract_pr_context(event, os.environ.get("GITHUB_REPOSITORY", ""))
    except (RuntimeError, ValueError) as exc:
        return fail(str(exc))

    if context["dependabot"]:
        print("PR branch CI admission gate: Dependabot standing exception applies.")
        return 0
    if context["external"]:
        print("PR branch CI admission gate: external/fork PR; internal task-branch admission rule not applied.")
        return 0

    head_ref = str(context["head_ref"])
    head_sha = str(context["head_sha"])
    if not is_allowed_internal_ref(head_ref):
        return fail(
            f"internal watched PR head '{head_ref}' is outside automatic branch-CI namespaces; "
            "use agent/** or integration/** and obtain exact-head branch CI SUCCESS before opening the PR"
        )
    if len(head_sha) != 40:
        return fail(f"invalid PR head SHA: {head_sha!r}")

    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not token:
        return fail("GITHUB_TOKEN is required to verify exact pre-PR branch CI evidence")

    repository = os.environ.get("GITHUB_REPOSITORY", "").strip() or str(context["base_repo"])
    try:
        runs = fetch_runs(repository, head_sha, token)
    except RuntimeError as exc:
        return fail(str(exc))

    pr_created_at = context["created_at"]
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
        f"{head_ref}@{head_sha} before PR creation at {context['created_at_raw']}; observed: {evidence}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
