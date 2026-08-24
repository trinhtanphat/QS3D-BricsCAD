#!/usr/bin/env python3
from pathlib import Path
import contextlib
import importlib.util
import io
import os
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SCANNER = ROOT / "scripts" / "preflight-ci-manual-only.py"
MAX_WORKFLOW_SOURCE_BYTES = 1024 * 1024


def run_case(workflows):
    return subprocess.run(
        [sys.executable, str(SCANNER), "--validate-workflow-inputs-only", str(workflows)],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
        timeout=20,
    )


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def expect_success(workflows, expected_names):
    result = run_case(workflows)
    require(result.returncode == 0, f"expected success, got {result.returncode}: {result.stdout}")
    expected = "PASS: " + ", ".join(expected_names)
    require(result.stdout.strip() == expected, f"unexpected success output: {result.stdout!r}")


def expect_failure(workflows, token):
    result = run_case(workflows)
    require(result.returncode != 0, f"expected failure for {token!r}")
    require(token in result.stdout, f"missing diagnostic {token!r}: {result.stdout!r}")


def write_safe(path, payload=b"name: safe\non:\n  workflow_dispatch:\njobs:\n  test:\n    runs-on: ubuntu-latest\n"):
    path.write_bytes(payload)


def load_scanner_module(empty_workflows):
    module_name = "qs3d_preflight_ci_manual_only_input_safety_subject"
    spec = importlib.util.spec_from_file_location(module_name, SCANNER)
    require(spec is not None and spec.loader is not None, "cannot load CI manual-only scanner module")
    module = importlib.util.module_from_spec(spec)
    original_argv = sys.argv[:]
    sys.argv = [str(SCANNER), "--validate-workflow-inputs-only", str(empty_workflows)]
    output = io.StringIO()
    try:
        with contextlib.redirect_stdout(output):
            try:
                spec.loader.exec_module(module)
            except SystemExit as exc:
                require(exc.code == 0, f"scanner import bootstrap failed: {output.getvalue()}")
    finally:
        sys.argv = original_argv
    return module


def exercise_identity_retry(root):
    bootstrap = root / "module-bootstrap"
    bootstrap.mkdir()
    scanner = load_scanner_module(bootstrap)

    retry_root = root / "identity-retry"
    retry_root.mkdir()
    candidate = retry_root / "candidate.yml"
    original_payload = b"name: original\non:\n  workflow_dispatch:\njobs:\n  test:\n    runs-on: ubuntu-latest\n"
    replacement_payload = b"name: replacement\non:\n  workflow_dispatch:\njobs:\n  test:\n    runs-on: ubuntu-latest\n"
    write_safe(candidate, original_payload)

    real_open = scanner.os.open
    real_same_opened_file = scanner._same_opened_file
    open_calls = 0
    identity_checks = 0

    def swap_before_first_open(path, flags):
        nonlocal open_calls
        open_calls += 1
        if open_calls == 1:
            replacement = retry_root / "candidate.next"
            replacement.write_bytes(replacement_payload)
            os.replace(replacement, candidate)
        return real_open(path, flags)

    def first_identity_changed(before, opened):
        nonlocal identity_checks
        identity_checks += 1
        if identity_checks == 1:
            return False
        return real_same_opened_file(before, opened)

    scanner.os.open = swap_before_first_open
    scanner._same_opened_file = first_identity_changed
    try:
        sources = scanner.discover_workflow_sources(retry_root)
    finally:
        scanner.os.open = real_open
        scanner._same_opened_file = real_same_opened_file

    require(open_calls == 2, f"identity change must cause exactly one bounded retry, got {open_calls} opens")
    require(identity_checks >= 2, "identity check was not re-evaluated after retry")
    require(len(sources) == 1 and sources[0][0].name == "candidate.yml", "unexpected retry discovery result")
    require(sources[0][1].encode("utf-8") == replacement_payload, "retry must read the final revalidated workflow object")

    churn_root = root / "identity-churn"
    churn_root.mkdir()
    churn = churn_root / "candidate.yml"
    write_safe(churn)
    scanner = load_scanner_module(bootstrap)
    real_same_opened_file = scanner._same_opened_file
    scanner._same_opened_file = lambda before, opened: False
    try:
        try:
            scanner.discover_workflow_sources(churn_root)
        except ValueError as exc:
            require(
                "changed identity between workflow validation and open after bounded retry" in str(exc),
                f"unexpected identity-churn diagnostic: {exc}",
            )
        else:
            raise AssertionError("repeated identity change must fail closed after bounded retry")
    finally:
        scanner._same_opened_file = real_same_opened_file


def main():
    with tempfile.TemporaryDirectory(prefix="qs3d-ci-policy-input-") as temp:
        root = Path(temp)

        safe = root / "safe"
        safe.mkdir()
        write_safe(safe / "z.yaml")
        write_safe(safe / "a.yml")
        (safe / "ignored.txt").write_text("ignored", encoding="utf-8")
        expect_success(safe, ["a.yml", "z.yaml"])

        non_regular = root / "non-regular"
        non_regular.mkdir()
        (non_regular / "bad.yml").mkdir()
        expect_failure(non_regular, "must be a regular file")

        oversized = root / "oversized"
        oversized.mkdir()
        (oversized / "large.yml").write_bytes(b"a" * (MAX_WORKFLOW_SOURCE_BYTES + 1))
        expect_failure(oversized, "exceeds 1048576 bytes")

        invalid_utf8 = root / "invalid-utf8"
        invalid_utf8.mkdir()
        (invalid_utf8 / "bad.yaml").write_bytes(b"on:\n\xff\xfe")
        expect_failure(invalid_utf8, "not strict UTF-8")

        symlink_root = root / "symlink"
        symlink_root.mkdir()
        target = root / "outside.yml"
        write_safe(target)
        link = symlink_root / "escape.yml"
        try:
            os.symlink(target, link)
        except (OSError, NotImplementedError):
            print("SKIP: symlink creation unavailable on this platform")
        else:
            expect_failure(symlink_root, "must not be a symlink/reparse point")

        workflow_dir_symlink_parent = root / "workflow-dir-symlink-parent"
        workflow_dir_symlink_parent.mkdir()
        real_workflows = workflow_dir_symlink_parent / "real"
        real_workflows.mkdir()
        write_safe(real_workflows / "safe.yml")
        linked_workflows = workflow_dir_symlink_parent / "linked"
        try:
            os.symlink(real_workflows, linked_workflows, target_is_directory=True)
        except (OSError, NotImplementedError):
            print("SKIP: directory symlink creation unavailable on this platform")
        else:
            expect_failure(linked_workflows, "workflow directory must not be a symlink/reparse point")

        exercise_identity_retry(root)

    print("PASS: CI manual-only workflow input safety regression")


if __name__ == "__main__":
    main()
