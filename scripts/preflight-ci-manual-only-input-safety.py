#!/usr/bin/env python3
from pathlib import Path
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

    print("PASS: CI manual-only workflow input safety regression")


if __name__ == "__main__":
    main()
