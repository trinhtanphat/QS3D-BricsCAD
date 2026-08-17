#!/usr/bin/env python3
"""Hermetic regression for duplicate GitHub Actions policy mapping keys."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-ci-policy-key-uniqueness.py"


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def load_target():
    spec = importlib.util.spec_from_file_location("qs3d_ci_policy_key_uniqueness", TARGET)
    if spec is None or spec.loader is None:
        fail("could not load CI policy key uniqueness preflight")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def assert_clean(module, name: str, text: str) -> None:
    errors = module.scan_workflow_text(text, name)
    if errors:
        fail(f"{name} unexpectedly failed: {errors}")


def assert_rejected(module, name: str, text: str, token: str) -> None:
    errors = module.scan_workflow_text(text, name)
    if not errors:
        fail(f"{name} unexpectedly passed")
    if not any(token in error for error in errors):
        fail(f"{name} did not report expected diagnostic {token!r}: {errors}")


def main() -> int:
    if not TARGET.is_file():
        fail("missing scripts/preflight-ci-policy-key-uniqueness.py")
    module = load_target()

    valid = """name: validation
on:
  workflow_dispatch:
  push:
    branches:
      - main
jobs:
  preflight:
    runs-on: windows-latest
  \"core\":
    needs: preflight
    runs-on: windows-latest
"""
    assert_clean(module, "valid.yml", valid)

    quoted_valid = """name: quoted
\"on\":
  'workflow_dispatch':
'jobs':
  \"release-job\":
    runs-on: windows-latest
"""
    assert_clean(module, "quoted-valid.yml", quoted_valid)

    duplicate_on = """name: duplicate-on
on:
  workflow_dispatch:
'on':
  push:
jobs:
  check:
    runs-on: windows-latest
"""
    assert_rejected(module, "duplicate-on.yml", duplicate_on, "duplicate top-level on")

    duplicate_jobs_block = """name: duplicate-jobs-block
on:
  workflow_dispatch:
jobs:
  first:
    runs-on: windows-latest
\"jobs\":
  second:
    runs-on: windows-latest
"""
    assert_rejected(module, "duplicate-jobs-block.yml", duplicate_jobs_block, "duplicate top-level jobs")

    duplicate_trigger = """name: duplicate-trigger
on:
  push:
    branches:
      - main
  'push':
    branches:
      - agent/**
jobs:
  check:
    runs-on: windows-latest
"""
    assert_rejected(module, "duplicate-trigger.yml", duplicate_trigger, "duplicate trigger mapping key: push")

    duplicate_job = """name: duplicate-job
on:
  workflow_dispatch:
jobs:
  preflight:
    runs-on: windows-latest
  \"preflight\":
    runs-on: ubuntu-latest
"""
    assert_rejected(module, "duplicate-job.yml", duplicate_job, "duplicate job mapping key: preflight")

    comments_and_nested = """name: nested-controls
on:
  workflow_dispatch:
    inputs:
      mode:
        required: false
jobs:
  preflight:
    runs-on: windows-latest
    steps:
      - name: on
        run: echo jobs
# on:
# jobs:
"""
    assert_clean(module, "nested-controls.yml", comments_and_nested)

    case_distinct = """name: case-distinct
on:
  workflow_dispatch:
jobs:
  Audit:
    runs-on: windows-latest
  audit:
    runs-on: windows-latest
"""
    assert_clean(module, "case-distinct.yml", case_distinct)

    repository_errors = module.scan_repository()
    if repository_errors:
        fail(f"current repository workflow set violates uniqueness contract: {repository_errors}")

    print(
        "PASS: CI policy uniqueness regression rejects duplicate top-level on/jobs, triggers and job IDs, "
        "including quoted/unquoted equivalents, while preserving valid/nested/comment/case-distinct controls."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
