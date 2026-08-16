#!/usr/bin/env python3
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()
CHILD_TIMEOUT_SECONDS = 180


def _relative_to_root(path):
    try:
        return path.relative_to(ROOT)
    except ValueError as exc:
        raise RuntimeError("feature preflight escaped repository root: " + str(path)) from exc


def _display_path(path):
    return _relative_to_root(path).as_posix()


def discover():
    candidates = list(SCRIPTS.glob("preflight-*.py"))
    validated = []
    names = {}

    for path in candidates:
        if path.is_symlink():
            raise RuntimeError("feature preflight must not be a symlink: " + _display_path(path))
        if not path.is_file():
            raise RuntimeError("feature preflight must be a regular file: " + _display_path(path))

        try:
            resolved = path.resolve(strict=True)
        except (OSError, RuntimeError) as exc:
            raise RuntimeError("failed to resolve feature preflight: " + str(path) + " - " + str(exc)) from exc

        _relative_to_root(resolved)
        if resolved == SELF:
            continue

        folded = path.name.casefold()
        prior = names.get(folded)
        if prior is not None and prior.name != path.name:
            raise RuntimeError(
                "case-insensitive feature preflight filename collision: " + prior.name + " / " + path.name
            )
        names[folded] = path
        validated.append(path)

    return sorted(validated, key=lambda p: (p.name.casefold(), p.name))


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


def run_gate(path, child_env):
    rel = _display_path(path)
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
        return rel, "timeout"
    except OSError as exc:
        print("ERROR: failed to start", rel, "-", exc)
        return rel, "launch"

    if completed.returncode != 0:
        return rel, "exit=" + str(completed.returncode)
    return None


def main():
    try:
        gates = discover()
    except (OSError, RuntimeError) as exc:
        print("ERROR: feature preflight discovery failed -", exc)
        return 1

    if not gates:
        print("ERROR: no feature preflight gates were discovered.")
        return 1

    print("QS3D aggregate feature preflight")
    print("Discovered", len(gates), "feature gate(s):")
    for path in gates:
        print(" -", _display_path(path))

    failed = []
    child_env = os.environ.copy()
    child_env["PYTHONUTF8"] = "1"
    child_env["PYTHONIOENCODING"] = "utf-8"
    for path in gates:
        failure = run_gate(path, child_env)
        if failure is not None:
            failed.append(failure)

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
