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
        "metadata identity validation step": "name: Validate PR metadata-edit identities",
        "runtime identity verifier": "python scripts/preflight-ci-edited-evidence.py --verify-runtime",
        "head binding": "QS3D_EXPECTED_HEAD_SHA: ${{ github.event.pull_request.head.sha }}",
        "PR-number binding": "QS3D_EXPECTED_PR_NUMBER: ${{ github.event.pull_request.number }}",
        "base-ref binding": "QS3D_EXPECTED_BASE_REF: ${{ github.event.pull_request.base.ref }}",
        "base-SHA binding": "QS3D_EXPECTED_BASE_SHA: ${{ github.event.pull_request.base.sha }}",
    }
    errors = [label for label, needle in required_needles.items() if needle not in text]

    mirror_condition = "if: ${{ always() && github.event_name == 'pull_request' && github.event.pull_request.head.repo.full_name == github.repository }}"
    if text.count(mirror_condition) != 2:
        errors.append("preflight/core required-status mirrors must both include metadata-edited PR runs")

    forbidden_needles = {
        "historical GREEN reuse output": "reuse_exact_head_green",
        "historical GREEN reuse environment": "QS3D_REUSE_EXACT_HEAD_GREEN",
        "historical evidence step id": "id: edited_evidence",
        "edited-event required-status exclusion": "github.event.action != 'edited'",
    }
    errors.extend(
        f"forbidden {label} remains"
        for label, needle in forbidden_needles.items()
        if needle in text
    )
    return errors


def _exact_sha(name: str) -> str:
    value = os.environ.get(name, "").strip().lower()
    if len(value) != 40 or any(ch not in "0123456789abcdef" for ch in value):
        raise RuntimeError(f"{name} is not a 40-character hexadecimal commit identity")
    return value


def verify_runtime() -> None:
    repository = os.environ.get("GITHUB_REPOSITORY", "").strip()
    expected_sha = _exact_sha("QS3D_EXPECTED_HEAD_SHA")
    expected_base_sha = _exact_sha("QS3D_EXPECTED_BASE_SHA")
    expected_base_ref = os.environ.get("QS3D_EXPECTED_BASE_REF", "").strip()
    pr_number_text = os.environ.get("QS3D_EXPECTED_PR_NUMBER", "").strip()
    run_id_text = os.environ.get("GITHUB_RUN_ID", "").strip()
    if not repository or not expected_base_ref or not pr_number_text or not run_id_text:
        raise RuntimeError("edited-event identity verifier is missing required GitHub runtime metadata")
    try:
        expected_pr_number = int(pr_number_text)
        current_run_id = int(run_id_text)
    except ValueError:
        raise RuntimeError("edited-event PR/run identity is not an integer") from None
    if expected_pr_number <= 0 or current_run_id <= 0:
        raise RuntimeError("edited-event PR/run identity must be positive")

    # Historical workflow-run nested PR metadata cannot prove an immutable base
    # snapshot. The workflow therefore has no reuse output or skip branch at all;
    # this step only validates the event identities before normal scope admission.
    identity = (
        f"{repository} PR #{expected_pr_number} run={current_run_id} "
        f"head={expected_sha} base={expected_base_ref}@{expected_base_sha}"
    )
    print("PASS: PR metadata-edit identities are exact; historical GREEN reuse is disabled", identity)


def main(argv: list[str]) -> int:
    try:
        if not WORKFLOW.is_file():
            raise RuntimeError("missing .github/workflows/ci.yml")
        errors = workflow_contract_errors(WORKFLOW.read_text(encoding="utf-8"))
        if errors:
            raise RuntimeError("edited-event CI safety contract violation: " + ", ".join(errors))
        if "--verify-runtime" in argv:
            verify_runtime()
    except (OSError, RuntimeError) as exc:
        print("ERROR:", exc)
        return 1
    print("PASS: PR-edited CI has no historical GREEN validation/status bypass.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
