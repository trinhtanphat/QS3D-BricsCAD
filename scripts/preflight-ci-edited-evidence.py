#!/usr/bin/env python3
"""Guard PR-edited CI from reusing mutable historical workflow evidence."""

from __future__ import annotations

import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


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

    # GitHub's workflow-run REST payload has immutable run.head_sha, but nested
    # pull_requests head/base records reflect live PR state. There is therefore
    # no immutable historical base-SHA proof that can safely authorize skipping
    # source/build validation after a metadata edit. Do not query or parse prior
    # runs here: deterministic full validation is the fail-closed admission path.
    emit_reuse_output(False)
    identity = f"PR #{expected_pr_number} head={expected_sha} base={expected_base_ref}@{expected_base_sha}"
    print(
        "PASS: historical workflow evidence reuse is disabled because immutable base evidence "
        "is unavailable; full validation remains required",
        identity,
    )


def main(argv: list[str]) -> int:
    try:
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
    print("PASS: PR-edited CI fails closed to full exact-head/base validation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
