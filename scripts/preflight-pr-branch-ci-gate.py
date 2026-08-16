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


def is_allowed_internal_ref(head_ref: str) -> bool:
    return bool(head_ref) and head_ref.startswith(ALLOWED_INTERNAL_PREFIXES)


def run_qualifies(run: dict[str, Any], head_ref: str, head_sha: str, pr_created_at: datetime) -> bool:
    if run.get("event") != "push":
        return False
    if run.get("head_branch") != head_ref or run.get("head_sha") != head_sha:
        return False
    if run.get("status") != "completed" or run.get("conclusion") != "success":
        return False
    if int(run.get("run_attempt") or 1) != 1:
        return False
    if run.get("path") not in (None, ".github/workflows/ci.yml"):
        return False
    try:
        created_at = parse_github_time(str(run.get("created_at") or ""))
        updated_at = parse_github_time(str(run.get("updated_at") or ""))
    except ValueError:
        return False
    return created_at <= pr_created_at and updated_at <= pr_created_at


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
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise RuntimeError("GitHub event payload must be a JSON object")
    return payload


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

    runs = payload.get("workflow_runs", []) if isinstance(payload, dict) else []
    if not isinstance(runs, list):
        raise RuntimeError("GitHub Actions lookup returned an invalid workflow_runs payload")
    return [run for run in runs if isinstance(run, dict)]


def main() -> int:
    errors = validate_self_tests() + validate_static_contract()
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if os.environ.get("GITHUB_EVENT_NAME") != "pull_request":
        print("PR branch CI admission gate: static contract/self-tests PASS; runtime check not applicable.")
        return 0

    try:
        event = read_event()
    except (RuntimeError, json.JSONDecodeError) as exc:
        return fail(str(exc))

    if event.get("action") != "opened":
        print("PR branch CI admission gate: PR was not newly opened; pre-PR admission check not applicable.")
        return 0

    pr = event.get("pull_request")
    if not isinstance(pr, dict):
        return fail("pull_request payload is missing")

    author = ((pr.get("user") or {}).get("login") or "").strip()
    if author == DEPENDABOT_LOGIN:
        print("PR branch CI admission gate: Dependabot standing exception applies.")
        return 0

    head = pr.get("head") or {}
    base = pr.get("base") or {}
    head_repo = ((head.get("repo") or {}).get("full_name") or "").strip()
    base_repo = ((base.get("repo") or {}).get("full_name") or os.environ.get("GITHUB_REPOSITORY", "")).strip()
    if not head_repo or not base_repo:
        return fail("could not resolve PR head/base repository identity")
    if head_repo.casefold() != base_repo.casefold():
        print("PR branch CI admission gate: external/fork PR; internal task-branch admission rule not applied.")
        return 0

    head_ref = str(head.get("ref") or "").strip()
    head_sha = str(head.get("sha") or "").strip()
    if not is_allowed_internal_ref(head_ref):
        return fail(
            f"internal watched PR head '{head_ref}' is outside automatic branch-CI namespaces; "
            "use agent/** or integration/** and obtain exact-head branch CI SUCCESS before opening the PR"
        )
    if len(head_sha) != 40:
        return fail(f"invalid PR head SHA: {head_sha!r}")

    try:
        pr_created_at = parse_github_time(str(pr.get("created_at") or ""))
    except ValueError as exc:
        return fail(str(exc))

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
        f"{head_ref}@{head_sha} before PR creation at {pr.get('created_at')}; observed: {evidence}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
