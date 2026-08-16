#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
GUARD = SCRIPTS / "check-actions-pinned.py"
SPEC = importlib.util.spec_from_file_location("qs3d_check_actions_pinned", GUARD)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError("cannot load check-actions-pinned.py")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

SHA = "a" * 40


def expect_pass(name: str, text: str):
    errors = MODULE.scan_workflow_text(name, text)
    if errors:
        raise AssertionError(f"{name} unexpectedly failed: {errors}")


def expect_fail(name: str, text: str, marker: str):
    errors = MODULE.scan_workflow_text(name, text)
    if not errors:
        raise AssertionError(f"{name} unexpectedly passed")
    if not any(marker in error for error in errors):
        raise AssertionError(f"{name} did not report {marker!r}: {errors}")


def main():
    expect_pass("plain.yml", f"steps:\n  - uses: actions/checkout@{SHA}\n")
    expect_pass("double-key-value.yml", f"steps:\n  - \"uses\": \"actions/setup-python@{SHA}\"\n")
    expect_pass("single-key-value.yml", f"steps:\n  - 'uses': 'actions/cache@{SHA}'\n")
    expect_pass("local.yml", "steps:\n  - \"uses\": \"./.github/actions/local\"\n")
    expect_pass("comment.yml", f"steps:\n  - uses: actions/checkout@{SHA} # immutable\n")

    expect_fail(
        "quoted-key-branch.yml",
        "steps:\n  - \"uses\": actions/checkout@main\n",
        "full 40-hex commit SHA",
    )
    expect_fail(
        "quoted-value-tag.yml",
        "steps:\n  - 'uses': 'actions/setup-python@v5'\n",
        "full 40-hex commit SHA",
    )
    expect_fail(
        "missing-ref.yml",
        "steps:\n  - \"uses\": actions/checkout\n",
        "must include an immutable ref",
    )
    expect_fail(
        "unterminated-double.yml",
        "steps:\n  - \"uses\": \"actions/checkout@main\n",
        "malformed double-quoted scalar",
    )
    expect_fail(
        "unterminated-single.yml",
        "steps:\n  - 'uses': 'actions/checkout@main\n",
        "malformed single-quoted scalar",
    )

    print("PASS: quoted/plain uses keys and scalars are parsed deterministically and unpinned external actions fail closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
