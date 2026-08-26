#!/usr/bin/env python3
"""Losslessly enumerate tracked PowerShell scripts below scripts/."""

from __future__ import annotations

import argparse
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys
import threading
import time

MAX_GIT_OUTPUT_BYTES = 4 * 1024 * 1024
MAX_GIT_DIAGNOSTIC_BYTES = 64 * 1024
GIT_TIMEOUT_SECONDS = 30.0
READ_CHUNK_BYTES = 64 * 1024
PROCESS_STOP_TIMEOUT_SECONDS = 5.0


class EnumerationError(RuntimeError):
    pass


def _drain_bounded(stream, limit: int, retained: bytearray, overflow: threading.Event, errors: list[BaseException]) -> None:
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
    except BaseException as exc:
        errors.append(exc)
    finally:
        try:
            stream.close()
        except OSError:
            pass


def _run_git(root: Path) -> bytes:
    try:
        process = subprocess.Popen(
            ["git", "ls-files", "-z", "--", "scripts"],
            cwd=str(root),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except OSError as exc:
        raise EnumerationError(f"could not enumerate tracked PowerShell scripts: {exc}") from exc

    if process.stdout is None or process.stderr is None:
        try:
            process.kill()
        except OSError:
            pass
        raise EnumerationError("tracked-script enumeration did not expose bounded output pipes")

    stdout = bytearray()
    stderr = bytearray()
    stdout_overflow = threading.Event()
    stderr_overflow = threading.Event()
    reader_errors: list[BaseException] = []

    stdout_thread = threading.Thread(
        target=_drain_bounded,
        args=(process.stdout, MAX_GIT_OUTPUT_BYTES, stdout, stdout_overflow, reader_errors),
        name="qs3d-powershell-paths-stdout",
        daemon=True,
    )
    stderr_thread = threading.Thread(
        target=_drain_bounded,
        args=(process.stderr, MAX_GIT_DIAGNOSTIC_BYTES, stderr, stderr_overflow, reader_errors),
        name="qs3d-powershell-paths-stderr",
        daemon=True,
    )
    stdout_thread.start()
    stderr_thread.start()

    deadline = time.monotonic() + GIT_TIMEOUT_SECONDS
    stop_reason: str | None = None
    while process.poll() is None:
        if reader_errors:
            stop_reason = "tracked-script output drain failed"
            break
        if stdout_overflow.is_set():
            stop_reason = f"tracked-path output exceeds {MAX_GIT_OUTPUT_BYTES} bytes"
            break
        if stderr_overflow.is_set():
            stop_reason = f"Git diagnostic output exceeds {MAX_GIT_DIAGNOSTIC_BYTES} bytes"
            break
        if time.monotonic() >= deadline:
            stop_reason = f"git ls-files timed out after {GIT_TIMEOUT_SECONDS:g} seconds"
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
        raise EnumerationError("git ls-files did not stop after bounded termination") from exc

    stdout_thread.join(PROCESS_STOP_TIMEOUT_SECONDS)
    stderr_thread.join(PROCESS_STOP_TIMEOUT_SECONDS)
    if stdout_thread.is_alive() or stderr_thread.is_alive():
        raise EnumerationError("tracked-script output drain did not stop after bounded termination")

    if reader_errors:
        raise EnumerationError(f"tracked-script output drain failed: {reader_errors[0]}") from reader_errors[0]
    if stop_reason is not None:
        raise EnumerationError(stop_reason)
    if stdout_overflow.is_set():
        raise EnumerationError(f"tracked-path output exceeds {MAX_GIT_OUTPUT_BYTES} bytes")
    if stderr_overflow.is_set():
        raise EnumerationError(f"Git diagnostic output exceeds {MAX_GIT_DIAGNOSTIC_BYTES} bytes")
    if returncode != 0:
        diagnostic = bytes(stderr).decode("utf-8", errors="replace").strip()
        raise EnumerationError(f"git ls-files failed with exit {returncode}: {diagnostic}")
    return bytes(stdout)


def parse_nul_paths(raw: bytes) -> list[str]:
    if not raw:
        return []
    if not raw.endswith(b"\0"):
        raise EnumerationError("tracked-path output is not NUL-terminated")

    encoded = raw[:-1].split(b"\0")
    if any(not item for item in encoded):
        raise EnumerationError("tracked-path output contains an empty path record")

    paths: list[str] = []
    for item in encoded:
        try:
            path = item.decode("utf-8", errors="strict")
        except UnicodeDecodeError as exc:
            raise EnumerationError("tracked-path output contains a non-UTF-8 pathname") from exc
        parsed = PurePosixPath(path)
        if parsed.is_absolute() or ".." in parsed.parts:
            raise EnumerationError(f"tracked path escapes repository-relative scope: {path!r}")
        if not parsed.parts or parsed.parts[0] != "scripts":
            raise EnumerationError(f"tracked path escapes scripts/ scope: {path!r}")
        paths.append(path)
    return paths


def enumerate_powershell_scripts(root: Path) -> list[str]:
    paths = parse_nul_paths(_run_git(root))
    scripts = [path for path in paths if PurePosixPath(path).suffix.casefold() == ".ps1"]
    if not scripts:
        raise EnumerationError("no tracked PowerShell scripts were found below scripts/")
    if len(set(scripts)) != len(scripts):
        raise EnumerationError("duplicate tracked PowerShell path records were returned by Git")
    return sorted(scripts)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="repository root containing .git")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        scripts = enumerate_powershell_scripts(Path(args.root).resolve())
    except EnumerationError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(json.dumps(scripts, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
