#!/usr/bin/env python3
"""Regression guard for fail-closed PR-edited workflow evidence."""

from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "preflight-ci-edited-evidence.py"


def _load_module():
    spec = importlib.util.spec_from_file_location("qs3d_ci_edited_evidence_shape", SOURCE)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load edited-evidence verifier")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    module = _load_module()
    source = SOURCE.read_text(encoding="utf-8")

    # Historical workflow-run nested PR metadata is mutable. Keep the unsafe
    # optimization surface physically absent so a later refactor cannot call a
    # stale helper and silently restore exact-base reuse from live PR metadata.
    forbidden = (
        "prior_green_exists",
        "fetch_prior_runs",
        "workflow_runs",
        "urlopen",
    )
    present = [needle for needle in forbidden if needle in source]
    if present:
        print("ERROR: mutable historical edited-evidence surface remains:", ", ".join(present))
        return 1

    original_env = os.environ.copy()
    try:
        with tempfile.TemporaryDirectory() as temp_dir:
            output = Path(temp_dir) / "github-output.txt"
            os.environ.update({
                "GITHUB_REPOSITORY": "trinhtanphat/QS3D-BricsCAD",
                "GITHUB_TOKEN": "token",
                "QS3D_EXPECTED_HEAD_SHA": "a" * 40,
                "QS3D_EXPECTED_BASE_SHA": "b" * 40,
                "QS3D_EXPECTED_BASE_REF": "main",
                "QS3D_EXPECTED_PR_NUMBER": "123",
                "GITHUB_RUN_ID": "200",
                "GITHUB_OUTPUT": str(output),
            })
            module.verify_runtime()
            lines = output.read_text(encoding="utf-8").splitlines()
            if lines != ["reuse_exact_head_green=false"]:
                print("ERROR: edited-event runtime did not force fail-closed full validation:", lines)
                return 1

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

    print("PASS: PR-edited evidence has no mutable historical reuse path and always requests full validation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
