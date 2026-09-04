#!/usr/bin/env python3
"""Classify Shared CI validation scope from lossless Git changed-path records."""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import threading
import time
from typing import NamedTuple

ROOT = Path(__file__).resolve().parents[1]

BUILD_PREFIXES = (
    "src/",
    "tests/",
    "scripts/",
    "samples/generated/",
    ".github/workflows/",
)
BUILD_EXACT = {
    ".gitmodules",
    "Directory.Build.props",
    "QS3D.sln",
    "QS3D.V26.sln",
    "external/QS3D-Platform",
}
SOURCE_EXACT = {
    "CI_POLICY.md",
    "AGENTS.md",
    "README.md",
    "docs/MAIN-WRITE-AUTHORIZATION.md",
    "docs/AGENT-WORK-REGISTRATION.md",
    "docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md",
    "docs/AGENT-STATUS-MARKER-SEMANTICS.md",
    "docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md",
}

GIT_DIFF_TIMEOUT_SECONDS = 30.0
MAX_CHANGED_PATH_BYTES = 4 * 1024 * 1024
MAX_GIT_DIAGNOSTIC_BYTES = 64 * 1024
READ_CHUNK_BYTES = 64 * 1024
PROCESS_STOP_TIMEOUT_SECONDS = 5.0


class ScopeError(RuntimeError):
    pass


class BoundedProcessResult(NamedTuple):
    returncode: int
    stdout: bytes
    stderr: bytes


def _drain_bounded(
    stream,
    limit: int,
    retained: bytearray,
    overflow: threading.Event,
    errors: list[BaseException],
) -> None:
    try:
        while True:
            chunk = stream.read(READ_CHUNK_BYTES)
            if not chunk:
                return
            remaining = limit - len(retained)
            if remaining > 0:
                retained.extend(chunk[:remaining])
            if len(chunk) > max(remaining, 0):
                overflow.set()
    except BaseException as exc:  # reader failures must fail the classifier closed
        errors.append(exc)
    finally:
        try:
            stream.close()
        except OSError:
            pass


def run_bounded_process(
    command: list[str],
    *,
    cwd: Path,
    timeout_seconds: float,
    max_stdout_bytes: int,
    max_stderr_bytes: int,
) -> BoundedProcessResult:
    if timeout_seconds <= 0:
        raise ScopeError("process timeout must be positive")
    if max_stdout_bytes <= 0 or max_stderr_bytes <= 0:
        raise ScopeError("process output limits must be positive")

    try:
        process = subprocess.Popen(
            command,
            cwd=str(cwd),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except OSError as exc:
        raise ScopeError(f"could not launch changed-path command: {exc}") from exc

    if process.stdout is None or process.stderr is None:
        try:
            process.kill()
        except OSError:
            pass
        raise ScopeError("changed-path command did not expose bounded output pipes")

    stdout = bytearray()
    stderr = bytearray()
    stdout_overflow = threading.Event()
    stderr_overflow = threading.Event()
    reader_errors: list[BaseException] = []

    stdout_thread = threading.Thread(
        target=_drain_bounded,
        args=(process.stdout, max_stdout_bytes, stdout, stdout_overflow, reader_errors),
        name="qs3d-ci-scope-stdout",
        daemon=True,
    )
    stderr_thread = threading.Thread(
        target=_drain_bounded,
        args=(process.stderr, max_stderr_bytes, stderr, stderr_overflow, reader_errors),
        name="qs3d-ci-scope-stderr",
        daemon=True,
    )
    stdout_thread.start()
    stderr_thread.start()

    deadline = time.monotonic() + timeout_seconds
    stop_reason: str | None = None
    while process.poll() is None:
        if reader_errors:
            stop_reason = "changed-path output drain failed"
            break
        if stdout_overflow.is_set():
            stop_reason = f"Git changed-path output exceeded {max_stdout_bytes}-byte limit"
            break
        if stderr_overflow.is_set():
            stop_reason = f"Git diagnostic output exceeded {max_stderr_bytes}-byte limit"
            break
        if time.monotonic() >= deadline:
            stop_reason = f"Git changed-path command timed out after {timeout_seconds:g} seconds"
            break
        time.sleep(0.01)

    if stop_reason is not None:
        try:
            process.kill()
        except OSError:
            pass

    try:
        returncode = process.wait(timeout=PROCESS_STOP_TIMEOUT_SECONDS)
    except subprocess.TimeoutExpired as exc:
        try:
            process.kill()
        except OSError:
            pass
        raise ScopeError("changed-path command did not stop after bounded termination") from exc

    stdout_thread.join(PROCESS_STOP_TIMEOUT_SECONDS)
    stderr_thread.join(PROCESS_STOP_TIMEOUT_SECONDS)
    if stdout_thread.is_alive() or stderr_thread.is_alive():
        raise ScopeError("changed-path output drain did not stop after bounded termination")

    if reader_errors:
        raise ScopeError(f"changed-path output drain failed: {reader_errors[0]}") from reader_errors[0]
    if stop_reason is not None:
        raise ScopeError(stop_reason)
    if stdout_overflow.is_set():
        raise ScopeError(f"Git changed-path output exceeded {max_stdout_bytes}-byte limit")
    if stderr_overflow.is_set():
        raise ScopeError(f"Git diagnostic output exceeded {max_stderr_bytes}-byte limit")

    return BoundedProcessResult(returncode, bytes(stdout), bytes(stderr))


def parse_nul_paths(raw: bytes) -> list[str]:
    """Decode Git `-z` path records without newline/quote tokenization."""
    if not raw:
        return []
    if not raw.endswith(b"\0"):
        raise ScopeError("Git changed-path output is not NUL-terminated")

    encoded_paths = raw[:-1].split(b"\0")
    if any(not item for item in encoded_paths):
        raise ScopeError("Git changed-path output contains an empty path record")

    paths: list[str] = []
    for item in encoded_paths:
        try:
            path = item.decode("utf-8", errors="strict")
        except UnicodeDecodeError as exc:
            raise ScopeError("Git changed-path output contains a non-UTF-8 pathname") from exc
        if "\0" in path:
            raise ScopeError("Git changed-path output contains an embedded NUL")
        paths.append(path)
    return paths


def classify_path(path: str) -> tuple[bool, bool]:
    """Return (source_validation, build_validation) for one exact Git pathname."""
    build = path.startswith(BUILD_PREFIXES) or path in BUILD_EXACT
    source = build or path in SOURCE_EXACT
    return source, build


def classify_paths(paths: list[str]) -> tuple[bool, bool]:
    source = False
    build = False
    for path in paths:
        path_source, path_build = classify_path(path)
        source = source or path_source
        build = build or path_build
    return source, build


def changed_paths(base_ref: str, head_ref: str = "HEAD", root: Path = ROOT) -> list[str]:
    result = run_bounded_process(
        [
            "git",
            "diff",
            "--no-ext-diff",
            "--no-textconv",
            "--no-renames",
            "--name-only",
            "-z",
            f"{base_ref}...{head_ref}",
            "--",
        ],
        cwd=root,
        timeout_seconds=GIT_DIFF_TIMEOUT_SECONDS,
        max_stdout_bytes=MAX_CHANGED_PATH_BYTES,
        max_stderr_bytes=MAX_GIT_DIAGNOSTIC_BYTES,
    )
    if result.returncode != 0:
        stderr = result.stderr.decode("utf-8", errors="replace").strip()
        raise ScopeError(
            f"could not classify changed paths against {base_ref} (git exit={result.returncode}): {stderr}"
        )
    return parse_nul_paths(result.stdout)


def write_outputs(output_path: Path, source: bool, build: bool) -> None:
    if not str(output_path):
        raise ScopeError("GITHUB_OUTPUT path is required")
    with output_path.open("a", encoding="utf-8", newline="\n") as stream:
        stream.write(f"source_validation={'true' if source else 'false'}\n")
        stream.write(f"build_validation={'true' if build else 'false'}\n")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="fetched Git comparison base ref")
    parser.add_argument("--head", default="HEAD", help="Git comparison head ref")
    parser.add_argument("--all", action="store_true", help="force source/build validation")
    parser.add_argument("--github-output", help="GitHub Actions output file; defaults to GITHUB_OUTPUT")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    output_value = args.github_output or os.environ.get("GITHUB_OUTPUT", "")
    if not output_value:
        print("ERROR: GITHUB_OUTPUT path is required", file=sys.stderr)
        return 1

    try:
        if args.all:
            paths: list[str] = []
            source, build = True, True
        else:
            if not args.base:
                raise ScopeError("--base is required unless --all is used")
            paths = changed_paths(args.base, args.head)
            source, build = classify_paths(paths)

        write_outputs(Path(output_value), source, build)
    except (OSError, ScopeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print(f"Validation scope: source_validation={'true' if source else 'false'} build_validation={'true' if build else 'false'}")
    if paths:
        print("Candidate changed paths:")
        for path in paths:
            print(" - " + json.dumps(path, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
