#!/usr/bin/env python3
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()
CHILD_TIMEOUT_SECONDS = 180


class DiscoveryError(RuntimeError):
    pass


def _resolved_within(path, root):
    try:
        path.relative_to(root)
        return True
    except ValueError:
        return False


def _ensure_unique_casefold_names(paths):
    seen = {}
    for path in paths:
        key = path.name.casefold()
        previous = seen.get(key)
        if previous is not None and previous.name != path.name:
            raise DiscoveryError(
                "case-insensitive preflight filename collision: "
                + previous.name
                + " vs "
                + path.name
            )
        seen[key] = path


def validate_candidates(candidates, root=ROOT, self_path=SELF):
    root_resolved = Path(root).resolve(strict=True)
    self_resolved = Path(self_path).resolve(strict=True)
    validated = []

    for candidate in candidates:
        path = Path(candidate)
        if path.is_symlink():
            raise DiscoveryError("preflight gate must not be a symlink: " + path.name)
        if not path.is_file():
            raise DiscoveryError("preflight gate must be a regular file: " + path.name)

        try:
            resolved = path.resolve(strict=True)
        except OSError as exc:
            raise DiscoveryError("failed to resolve preflight gate " + path.name + ": " + str(exc)) from exc

        if not _resolved_within(resolved, root_resolved):
            raise DiscoveryError("preflight gate resolves outside repository root: " + path.name)
        if resolved == self_resolved:
            continue
        validated.append(path)

    _ensure_unique_casefold_names(validated)
    return sorted(validated, key=lambda p: (p.name.casefold(), p.name))


def discover(scripts=SCRIPTS, root=ROOT, self_path=SELF):
    try:
        candidates = list(Path(scripts).glob("preflight-*.py"))
    except OSError as exc:
        raise DiscoveryError("failed to inspect preflight directory: " + str(exc)) from exc
    return validate_candidates(candidates, root=root, self_path=self_path)


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


def run_gate(path, root=ROOT, child_env=None, timeout=CHILD_TIMEOUT_SECONDS):
    env = os.environ.copy() if child_env is None else child_env
    try:
        completed = subprocess.run(
            [sys.executable, str(path)],
            cwd=str(root),
            check=False,
            env=env,
            timeout=timeout,
        )
    except subprocess.TimeoutExpired:
        return "timeout"
    except OSError:
        return "launch"

    if completed.returncode != 0:
        return "exit=" + str(completed.returncode)
    return None


def main():
    try:
        gates = discover()
    except DiscoveryError as exc:
        print("ERROR: preflight discovery failed:", exc)
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
        reason = run_gate(path, root=ROOT, child_env=child_env)
        if reason is None:
            continue
        if reason == "timeout":
            print("ERROR:", rel, "timed out after", CHILD_TIMEOUT_SECONDS, "seconds.")
        elif reason == "launch":
            print("ERROR: failed to start", rel)
        failed.append((str(rel), reason))

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
