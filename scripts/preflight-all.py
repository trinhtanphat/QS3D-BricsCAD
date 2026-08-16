#!/usr/bin/env python3
from pathlib import Path
import os
import stat
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()
CHILD_TIMEOUT_SECONDS = 180


def _relative_candidate(path):
    try:
        return path.relative_to(ROOT)
    except ValueError as exc:
        raise RuntimeError("feature preflight gate is outside repository root: " + str(path)) from exc


def validate_candidates(candidates):
    unsafe = []
    by_casefold = {}

    for path in candidates:
        rel = _relative_candidate(path)
        try:
            mode = os.lstat(path).st_mode
        except OSError as exc:
            raise RuntimeError("cannot inspect feature preflight gate " + str(rel) + ": " + str(exc)) from exc

        if path.is_symlink():
            unsafe.append((str(rel), "symlink"))
        elif not stat.S_ISREG(mode):
            unsafe.append((str(rel), "non-regular"))

        key = path.name.casefold()
        by_casefold.setdefault(key, []).append(path)

    collisions = [paths for paths in by_casefold.values() if len(paths) > 1]
    if unsafe or collisions:
        messages = []
        for rel, reason in sorted(unsafe):
            messages.append(rel + " is " + reason)
        for paths in sorted(collisions, key=lambda group: (group[0].name.casefold(), group[0].name)):
            names = ", ".join(sorted(str(_relative_candidate(path)) for path in paths))
            messages.append("case-insensitive preflight filename collision: " + names)
        raise RuntimeError("unsafe or ambiguous feature preflight discovery: " + "; ".join(messages))

    return sorted(candidates, key=lambda path: (path.name.casefold(), path.name))


def discover():
    candidates = [path for path in SCRIPTS.glob("preflight-*.py") if path.resolve() != SELF]
    return validate_candidates(candidates)


def escape_actions_data(value):
    return str(value).replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def escape_actions_property(value):
    return escape_actions_data(value).replace(":", "%3A").replace(",", "%2C")


def emit_failure_annotation(path, reason):
    if os.environ.get("GITHUB_ACTIONS", "").lower() != "true":
        return
    rel = str(path).replace("\\", "/")
    file_property = escape_actions_property(rel)
    message = escape_actions_data("Feature preflight failed: " + rel + " (" + str(reason) + ")")
    print("::error file=" + file_property + "::" + message)


def main():
    try:
        gates = discover()
    except RuntimeError as exc:
        print("ERROR:", exc)
        return 1

    if not gates:
        print("ERROR: no feature preflight gates were discovered.")
        return 1

    print("QS3D aggregate feature preflight")
    print("Discovered", len(gates), "feature gate(s):")
    for path in gates:
        print(" -", path.relative_to(ROOT))

    failed = []
    child_env = os.environ.copy()
    child_env["PYTHONUTF8"] = "1"
    child_env["PYTHONIOENCODING"] = "utf-8"
    for path in gates:
        rel = path.relative_to(ROOT)
        print("\n===", rel, "===")
        try:
            completed = subprocess.run(
                [sys.executable, str(path)],
                cwd=str(ROOT),
                check=False,
                env=child_env,
                timeout=CHILD_TIMEOUT_SECONDS,
            )
        except subprocess.TimeoutExpired:
            print("ERROR:", rel, "timed out after", CHILD_TIMEOUT_SECONDS, "seconds.")
            failed.append((str(rel), "timeout"))
            continue
        except OSError as exc:
            print("ERROR: failed to start", rel, "-", exc)
            failed.append((str(rel), "launch"))
            continue

        if completed.returncode != 0:
            failed.append((str(rel), "exit=" + str(completed.returncode)))

    if failed:
        print("\nAggregate preflight FAILED:")
        for path, reason in failed:
            print(" -", path, reason)
            emit_failure_annotation(path, reason)
        print("FAILED with", len(failed), "feature gate failure(s).")
        return 1

    print("\nPASS: all", len(gates), "discovered feature preflight gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
