#!/usr/bin/env python3
"""Regression guard for fail-closed PR-edited workflow evidence."""

from __future__ import annotations

import ast
import importlib.util
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "preflight-ci-edited-evidence.py"
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def _load_module():
    spec = importlib.util.spec_from_file_location("qs3d_ci_edited_evidence_shape", SOURCE)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load edited-evidence verifier")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _historical_reuse_surface(source: str) -> list[str]:
    tree = ast.parse(source, filename=str(SOURCE))
    findings: list[str] = []
    forbidden_functions = {"prior_green_exists", "fetch_prior_runs", "emit_reuse_output"}
    forbidden_import_roots = {"urllib", "requests"}

    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)) and node.name in forbidden_functions:
            findings.append(f"function:{node.name}")
        elif isinstance(node, ast.Import):
            for alias in node.names:
                root = alias.name.split(".", 1)[0]
                if root in forbidden_import_roots:
                    findings.append(f"import:{alias.name}")
        elif isinstance(node, ast.ImportFrom) and node.module:
            root = node.module.split(".", 1)[0]
            if root in forbidden_import_roots:
                findings.append(f"import:{node.module}")

    return sorted(set(findings))


def _cross_event_required_status_race_errors(workflow: str) -> list[str]:
    """Require one PR cancellation domain and keep check-run names distinct from required statuses."""
    errors: list[str] = []
    if "github.event.action == 'edited' && 'metadata'" in workflow:
        errors.append("metadata edits use a separate PR concurrency domain")

    required_status_check_run_names = {
        "preflight": "github.event_name == 'pull_request' && 'preflight'",
        "core": "github.event_name == 'pull_request' && 'core'",
    }
    for context, needle in required_status_check_run_names.items():
        if needle in workflow:
            errors.append(f"pull_request job check-run still shares required status context {context}")

    expected_pr_check_run_names = {
        "candidate-preflight": "github.event_name == 'pull_request' && 'candidate-preflight'",
        "candidate-core": "github.event_name == 'pull_request' && 'candidate-core'",
    }
    for label, needle in expected_pr_check_run_names.items():
        if needle not in workflow:
            errors.append(f"missing non-required PR check-run name {label}")

    return errors


def main() -> int:
    module = _load_module()
    source = SOURCE.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")

    findings = _historical_reuse_surface(source)
    if findings:
        print("ERROR: mutable historical edited-evidence implementation remains:", ", ".join(findings))
        return 1

    contract_errors = module.workflow_contract_errors(workflow)
    if contract_errors:
        print("ERROR: edited-event workflow bypass contract remains:", ", ".join(contract_errors))
        return 1

    race_errors = _cross_event_required_status_race_errors(workflow)
    if race_errors:
        print("ERROR: PR event required-status last-writer race remains:", ", ".join(race_errors))
        return 1

    original_env = os.environ.copy()
    try:
        os.environ.update({
            "GITHUB_REPOSITORY": "trinhtanphat/QS3D-BricsCAD",
            "QS3D_EXPECTED_HEAD_SHA": "a" * 40,
            "QS3D_EXPECTED_BASE_SHA": "b" * 40,
            "QS3D_EXPECTED_BASE_REF": "main",
            "QS3D_EXPECTED_PR_NUMBER": "123",
            "GITHUB_RUN_ID": "200",
        })
        module.verify_runtime()

        os.environ["QS3D_EXPECTED_HEAD_SHA"] = "not-a-sha"
        try:
            module.verify_runtime()
        except RuntimeError as exc:
            if "40-character hexadecimal" not in str(exc):
                print("ERROR: malformed exact-head identity failed with unexpected diagnostic:", exc)
                return 1
        else:
            print("ERROR: malformed exact-head identity was accepted")
            return 1
    finally:
        os.environ.clear()
        os.environ.update(original_env)

    print("PASS: PR-edited CI has no mutable historical reuse, workflow skip bypass, or cross-event required-status race")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
