#!/usr/bin/env python3
"""Guard PR-edited CI from cancelling or fabricating exact-head build evidence."""

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


def prior_green_exists(runs: Iterable[dict], expected_sha: str, current_run_id: int) -> bool:
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
        if run.get("name") != WORKFLOW_NAME:
            continue
        if run.get("event") not in {"push", "pull_request", "workflow_dispatch"}:
            continue
        return True
    return False


def workflow_contract_errors(text: str) -> list[str]:
    required_needles = {
        "pull_request edited trigger": "      - edited\n",
        "edited-safe concurrency cancellation": (
            "cancel-in-progress: ${{ github.event_name != 'pull_request' || github.event.action != 'edited' }}"
        ),
        "prior exact-head evidence step": "Require prior exact-head GREEN for PR metadata edit",
        "runtime evidence verifier": "python scripts/preflight-ci-edited-evidence.py --verify-runtime",
        "edited metadata-only scope": "QS3D_PR_ACTION: ${{ github.event.action }}",
        "edited source/build bypass after evidence": "$env:QS3D_PR_ACTION -eq 'edited'",
    }
    return [label for label, needle in required_needles.items() if needle not in text]


def self_test() -> None:
    sha = "a" * 40
    current = 200
    base = {
        "id": 100,
        "head_sha": sha,
        "status": "completed",
        "conclusion": "success",
        "name": WORKFLOW_NAME,
        "event": "pull_request",
    }
    if not prior_green_exists([base], sha, current):
        raise RuntimeError("exact-head prior GREEN fixture was rejected")
    if prior_green_exists([{**base, "id": current}], sha, current):
        raise RuntimeError("current run incorrectly satisfied prior-evidence gate")
    if prior_green_exists([{**base, "head_sha": "b" * 40}], sha, current):
        raise RuntimeError("stale SHA incorrectly satisfied prior-evidence gate")
    if prior_green_exists([{**base, "conclusion": "failure"}], sha, current):
        raise RuntimeError("failed run incorrectly satisfied prior-evidence gate")
    if prior_green_exists([{**base, "name": "other"}], sha, current):
        raise RuntimeError("unrelated workflow incorrectly satisfied prior-evidence gate")
    if prior_green_exists([{**base, "event": "schedule"}], sha, current):
        raise RuntimeError("unqualified event incorrectly satisfied prior-evidence gate")


def fetch_prior_runs(repository: str, token: str, expected_sha: str) -> list[dict]:
    encoded_repo = quote(repository, safe="/")
    encoded_sha = quote(expected_sha, safe="")
    collected: list[dict] = []
    for page in range(1, MAX_PAGES + 1):
        url = (
            f"https://api.github.com/repos/{encoded_repo}/actions/workflows/ci.yml/runs"
            f"?head_sha={encoded_sha}&status=success&per_page={PER_PAGE}&page={page}"
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


def verify_runtime() -> None:
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    token = os.environ.get("GITHUB_TOKEN", "").strip()
    expected_sha = os.environ.get("QS3D_EXPECTED_HEAD_SHA", "").strip()
    run_id_text = os.environ.get("GITHUB_RUN_ID", "").strip()
    if not repository or not token or not expected_sha or not run_id_text:
        raise RuntimeError("edited-event evidence verifier is missing required GitHub runtime metadata")
    if len(expected_sha) != 40 or any(ch not in "0123456789abcdefABCDEF" for ch in expected_sha):
        raise RuntimeError("expected head SHA is not a 40-character hexadecimal commit identity")
    try:
        current_run_id = int(run_id_text)
    except ValueError:
        raise RuntimeError("GITHUB_RUN_ID is not an integer") from None

    runs = fetch_prior_runs(repository, token, expected_sha)
    if not prior_green_exists(runs, expected_sha, current_run_id):
        raise RuntimeError(
            "PR metadata edit has no prior successful Shared CI run bound to the same exact head SHA; "
            "refusing to manufacture required preflight/core GREEN contexts."
        )
    print("PASS: prior successful Shared CI evidence exists for exact head", expected_sha)


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
    except RuntimeError as exc:
        print("ERROR:", exc)
        return 1
    print("PASS: PR-edited CI preserves exact-head evidence and fail-closed metadata admission.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
