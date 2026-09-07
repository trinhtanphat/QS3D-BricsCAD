#!/usr/bin/env python3
"""Guard PR-edited CI from cancelling or fabricating exact-head/base build evidence."""

from __future__ import annotations

import json
import os
from pathlib import Path
import sys
from typing import Iterable
from urllib.error import HTTPError, URLError
from urllib.parse import quote
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
WORKFLOW_NAME = "QS3D Shared Branch and Integration CI"
MAX_PAGES = 10
PER_PAGE = 100


def prior_green_exists(
    runs: Iterable[dict],
    expected_sha: str,
    expected_base_ref: str,
    expected_base_sha: str,
    expected_pr_number: int,
    current_run_id: int,
) -> bool:
    """Return true only for successful PR evidence on the exact head and exact base snapshot."""
    for run in runs:
        try:
            run_id = int(run.get("id", 0))
        except (TypeError, ValueError):
            continue
        if run_id == current_run_id:
            continue
        if run.get("head_sha") != expected_sha:
            continue
        if run.get("status") != "completed" or run.get("conclusion") != "success":
            continue
        if run.get("name") != WORKFLOW_NAME or run.get("event") != "pull_request":
            continue
        snapshots = run.get("pull_requests")
        if not isinstance(snapshots, list) or len(snapshots) != 1 or not isinstance(snapshots[0], dict):
            continue
        snapshot = snapshots[0]
        try:
            pr_number = int(snapshot.get("number", 0))
        except (TypeError, ValueError):
            continue
        head = snapshot.get("head")
        base = snapshot.get("base")
        if pr_number != expected_pr_number or not isinstance(head, dict) or not isinstance(base, dict):
            continue
        if head.get("sha") != expected_sha:
            continue
        if base.get("ref") != expected_base_ref or base.get("sha") != expected_base_sha:
            continue
        return True
    return False


def workflow_contract_errors(text: str) -> list[str]:
    required_needles = {
        "pull_request edited trigger": "      - edited\n",
        "edited-isolated concurrency class": "github.event.action == 'edited' && 'metadata'",
        "bounded cancellation inside each concurrency class": "cancel-in-progress: true",
        "prior exact-head evidence step": "name: Check prior exact-head GREEN for PR metadata edit",
        "evidence step id": "id: edited_evidence",
        "runtime evidence verifier": "python scripts/preflight-ci-edited-evidence.py --verify-runtime",
        "head binding": "QS3D_EXPECTED_HEAD_SHA: ${{ github.event.pull_request.head.sha }}",
        "PR-number binding": "QS3D_EXPECTED_PR_NUMBER: ${{ github.event.pull_request.number }}",
        "base-ref binding": "QS3D_EXPECTED_BASE_REF: ${{ github.event.pull_request.base.ref }}",
        "base-SHA binding": "QS3D_EXPECTED_BASE_SHA: ${{ github.event.pull_request.base.sha }}",
        "evidence output": "reuse_exact_head_green: ${{ steps.edited_evidence.outputs.reuse_exact_head_green }}",
        "edited evidence-aware scope": "$env:QS3D_REUSE_EXACT_HEAD_GREEN -eq 'true'",
    }
    return [label for label, needle in required_needles.items() if needle not in text]


def self_test() -> None:
    sha = "a" * 40
    base_sha = "b" * 40
    base_ref = "main"
    pr_number = 123
    current = 200
    valid = {
        "id": 100,
        "head_sha": sha,
        "status": "completed",
        "conclusion": "success",
        "name": WORKFLOW_NAME,
        "event": "pull_request",
        "pull_requests": [{
            "number": pr_number,
            "head": {"sha": sha},
            "base": {"ref": base_ref, "sha": base_sha},
        }],
    }

    def accepted(candidate: dict) -> bool:
        return prior_green_exists([candidate], sha, base_ref, base_sha, pr_number, current)

    if not accepted(valid):
        raise RuntimeError("exact PR/head/base prior GREEN fixture was rejected")
    mutations = {
        "current run": {**valid, "id": current},
        "stale head": {**valid, "head_sha": "c" * 40},
        "non-terminal": {**valid, "status": "in_progress"},
        "failed": {**valid, "conclusion": "failure"},
        "other workflow": {**valid, "name": "other"},
        "push evidence": {**valid, "event": "push"},
        "missing PR snapshot": {**valid, "pull_requests": []},
        "wrong PR": {**valid, "pull_requests": [{"number": 124, "head": {"sha": sha}, "base": {"ref": base_ref, "sha": base_sha}}]},
        "wrong snapshot head": {**valid, "pull_requests": [{"number": pr_number, "head": {"sha": "c" * 40}, "base": {"ref": base_ref, "sha": base_sha}}]},
        "wrong base ref": {**valid, "pull_requests": [{"number": pr_number, "head": {"sha": sha}, "base": {"ref": "integration/x", "sha": base_sha}}]},
        "wrong base SHA": {**valid, "pull_requests": [{"number": pr_number, "head": {"sha": sha}, "base": {"ref": base_ref, "sha": "d" * 40}}]},
    }
    for label, candidate in mutations.items():
        if accepted(candidate):
            raise RuntimeError(f"{label} incorrectly satisfied prior-evidence gate")


def fetch_prior_runs(repository: str, token: str, expected_sha: str) -> list[dict]:
    encoded_repo = quote(repository, safe="/")
    encoded_sha = quote(expected_sha, safe="")
    collected: list[dict] = []
    for page in range(1, MAX_PAGES + 1):
        url = (
            f"https://api.github.com/repos/{encoded_repo}/actions/workflows/ci.yml/runs"
            f"?head_sha={encoded_sha}&per_page={PER_PAGE}&page={page}"
        )
        request = Request(
            url,
            headers={
                "Accept": "application/vnd.github+json",
                "Authorization": f"Bearer {token}",
                "X-GitHub-Api-Version": "2022-11-28",
                "User-Agent": "qs3d-ci-edited-evidence",
            },
        )
        try:
            with urlopen(request, timeout=20) as response:
                payload = json.load(response)
        except HTTPError as exc:
            raise RuntimeError(f"GitHub Actions evidence query failed with HTTP {exc.code}") from None
        except (URLError, TimeoutError, json.JSONDecodeError) as exc:
            raise RuntimeError(f"GitHub Actions evidence query failed: {type(exc).__name__}") from None
        runs = payload.get("workflow_runs") if isinstance(payload, dict) else None
        if not isinstance(runs, list):
            raise RuntimeError("GitHub Actions evidence response omitted workflow_runs")
        collected.extend(run for run in runs if isinstance(run, dict))
        if len(runs) < PER_PAGE:
            break
    return collected


def emit_reuse_output(value: bool) -> None:
    output_path = os.environ.get("GITHUB_OUTPUT", "").strip()
    if not output_path:
        raise RuntimeError("edited-event evidence verifier is missing GITHUB_OUTPUT")
    with open(output_path, "a", encoding="utf-8", newline="\n") as stream:
        stream.write(f"reuse_exact_head_green={'true' if value else 'false'}\n")


def _exact_sha(name: str) -> str:
    value = os.environ.get(name, "").strip().lower()
    if len(value) != 40 or any(ch not in "0123456789abcdef" for ch in value):
        raise RuntimeError(f"{name} is not a 40-character hexadecimal commit identity")
    return value


def verify_runtime() -> None:
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    token = os.environ.get("GITHUB_TOKEN", "").strip()
    expected_sha = _exact_sha("QS3D_EXPECTED_HEAD_SHA")
    expected_base_sha = _exact_sha("QS3D_EXPECTED_BASE_SHA")
    expected_base_ref = os.environ.get("QS3D_EXPECTED_BASE_REF", "").strip()
    pr_number_text = os.environ.get("QS3D_EXPECTED_PR_NUMBER", "").strip()
    run_id_text = os.environ.get("GITHUB_RUN_ID", "").strip()
    if not repository or not token or not expected_base_ref or not pr_number_text or not run_id_text:
        raise RuntimeError("edited-event evidence verifier is missing required GitHub runtime metadata")
    try:
        expected_pr_number = int(pr_number_text)
        current_run_id = int(run_id_text)
    except ValueError:
        raise RuntimeError("edited-event PR/run identity is not an integer") from None
    if expected_pr_number <= 0 or current_run_id <= 0:
        raise RuntimeError("edited-event PR/run identity must be positive")

    # Evidence lookup is only an optimization. Any lookup/shape uncertainty falls back to full
    # validation rather than reusing evidence from a different head/base transaction.
    reuse = False
    try:
        runs = fetch_prior_runs(repository, token, expected_sha)
        reuse = prior_green_exists(
            runs,
            expected_sha,
            expected_base_ref,
            expected_base_sha,
            expected_pr_number,
            current_run_id,
        )
    except RuntimeError as exc:
        print(f"NOTICE: {exc}; falling back to full source/build validation.")
    emit_reuse_output(reuse)
    identity = f"PR #{expected_pr_number} head={expected_sha} base={expected_base_ref}@{expected_base_sha}"
    if reuse:
        print("PASS: prior successful Shared CI evidence exists for exact", identity)
    else:
        print("PASS: no reusable exact PR/head/base GREEN proven; full validation remains required", identity)


def main(argv: list[str]) -> int:
    try:
        self_test()
        if not WORKFLOW.is_file():
            raise RuntimeError("missing .github/workflows/ci.yml")
        errors = workflow_contract_errors(WORKFLOW.read_text(encoding="utf-8"))
        if errors:
            raise RuntimeError("edited-event CI safety contract missing: " + ", ".join(errors))
        if "--verify-runtime" in argv:
            verify_runtime()
    except (OSError, RuntimeError) as exc:
        print("ERROR:", exc)
        return 1
    print("PASS: PR-edited CI preserves exact PR/head/base evidence and fail-closed validation fallback.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
