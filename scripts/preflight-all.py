#!/usr/bin/env python3
from pathlib import Path
import os
import signal
import stat
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()
CHILD_TIMEOUT_SECONDS = 180
AGGREGATE_TIMEOUT_SECONDS = 15 * 60
PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS = 10
# Keep discovery finite while leaving explicit headroom above the repository's current 1025-gate scale.
MAX_FEATURE_GATES = 2048
MAX_FEATURE_GATE_SOURCE_BYTES = 512 * 1024
WINDOWS_CREATE_NEW_PROCESS_GROUP = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0x00000200)
PYTHON_ENVIRONMENT_CONTROLS = (
    "PYTHONBREAKPOINT",
    "PYTHONHOME",
    "PYTHONINSPECT",
    "PYTHONPATH",
    "PYTHONPYCACHEPREFIX",
    "PYTHONSTARTUP",
    "PYTHONUSERBASE",
    "PYTHONWARNINGS",
)


class GateTimeoutError(TimeoutError):
    def __init__(self, timeout_seconds, cleanup_error=None):
        super().__init__("feature preflight timed out")
        self.timeout_seconds = timeout_seconds
        self.cleanup_error = cleanup_error


def _relative_candidate(path):
    try:
        return path.relative_to(ROOT)
    except ValueError as exc:
        raise RuntimeError("feature preflight gate is outside repository root: " + str(path)) from exc


def validate_candidates(candidates):
    candidates = list(candidates)
    if len(candidates) > MAX_FEATURE_GATES:
        raise RuntimeError(
            "feature preflight discovery count "
            + str(len(candidates))
            + " exceeds maximum "
            + str(MAX_FEATURE_GATES)
        )

    unsafe = []
    by_casefold = {}

    for path in candidates:
        rel = _relative_candidate(path)
        try:
            file_stat = os.lstat(path)
        except OSError as exc:
            raise RuntimeError("cannot inspect feature preflight gate " + str(rel) + ": " + str(exc)) from exc

        mode = file_stat.st_mode
        if path.is_symlink():
            unsafe.append((str(rel), "symlink"))
        elif not stat.S_ISREG(mode):
            unsafe.append((str(rel), "non-regular"))
        elif file_stat.st_size > MAX_FEATURE_GATE_SOURCE_BYTES:
            unsafe.append(
                (
                    str(rel),
                    "source size "
                    + str(file_stat.st_size)
                    + " bytes exceeds maximum "
                    + str(MAX_FEATURE_GATE_SOURCE_BYTES),
                )
            )

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


def _is_feature_gate_name(name):
    folded = name.casefold()
    return folded.startswith("preflight-") and folded.endswith(".py")


def discover():
    candidates = []
    try:
        with os.scandir(SCRIPTS) as entries:
            for entry in entries:
                if not _is_feature_gate_name(entry.name):
                    continue
                path = Path(entry.path)
                if str(path) == str(SELF):
                    continue
                if entry.name.casefold() == SELF.name.casefold():
                    raise RuntimeError(
                        "case-insensitive preflight filename collision with aggregate runner: "
                        + entry.name
                    )
                candidates.append(path)
                if len(candidates) > MAX_FEATURE_GATES:
                    raise RuntimeError(
                        "feature preflight discovery count "
                        + str(len(candidates))
                        + " exceeds maximum "
                        + str(MAX_FEATURE_GATES)
                    )
    except OSError as exc:
        raise RuntimeError("cannot scan feature preflight directory " + str(SCRIPTS) + ": " + str(exc)) from exc
    return validate_candidates(candidates)


def build_child_env(source=None):
    child_env = dict(os.environ if source is None else source)
    for name in PYTHON_ENVIRONMENT_CONTROLS:
        child_env.pop(name, None)
    child_env["PYTHONUTF8"] = "1"
    child_env["PYTHONIOENCODING"] = "utf-8"
    child_env["PYTHONNOUSERSITE"] = "1"
    child_env["PYTHONDONTWRITEBYTECODE"] = "1"
    return child_env


def remaining_child_timeout(started_at, now=None):
    current = time.monotonic() if now is None else now
    remaining = AGGREGATE_TIMEOUT_SECONDS - max(0.0, current - started_at)
    if remaining <= 0:
        return 0.0
    return min(float(CHILD_TIMEOUT_SECONDS), remaining)


def _process_group_launch_kwargs(platform_name=None):
    platform = os.name if platform_name is None else platform_name
    if platform == "nt":
        return {"creationflags": WINDOWS_CREATE_NEW_PROCESS_GROUP}
    return {"start_new_session": True}


def _wait_after_tree_cleanup(process):
    try:
        process.wait(timeout=PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS)
    except subprocess.TimeoutExpired:
        return "process-tree-cleanup-wait-timeout"
    except OSError:
        return "process-tree-cleanup-wait-error"
    return None


def _terminate_process_tree(process, platform_name=None):
    platform = os.name if platform_name is None else platform_name
    cleanup_error = None

    if platform == "nt":
        try:
            completed = subprocess.run(
                ["taskkill", "/PID", str(process.pid), "/T", "/F"],
                check=False,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                timeout=PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS,
            )
            if completed.returncode != 0:
                cleanup_error = "process-tree-cleanup-exit=" + str(completed.returncode)
        except subprocess.TimeoutExpired:
            cleanup_error = "process-tree-cleanup-command-timeout"
        except OSError:
            cleanup_error = "process-tree-cleanup-command-error"
    else:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        except OSError:
            cleanup_error = "process-tree-cleanup-signal-error"

    if cleanup_error is not None:
        try:
            process.kill()
        except OSError:
            pass

    wait_error = _wait_after_tree_cleanup(process)
    if cleanup_error is None:
        cleanup_error = wait_error
    elif wait_error is not None:
        cleanup_error += "+" + wait_error
    return cleanup_error


def run_gate(path, child_env, timeout_seconds):
    process = subprocess.Popen(
        [sys.executable, str(path)],
        cwd=str(ROOT),
        env=child_env,
        **_process_group_launch_kwargs(),
    )
    try:
        return process.wait(timeout=timeout_seconds)
    except subprocess.TimeoutExpired as exc:
        cleanup_error = _terminate_process_tree(process)
        raise GateTimeoutError(timeout_seconds, cleanup_error) from exc


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
    aggregate_started_at = time.monotonic()
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
    child_env = build_child_env()
    for path in gates:
        rel = path.relative_to(ROOT)
        child_timeout = remaining_child_timeout(aggregate_started_at)
        if child_timeout <= 0:
            print("ERROR: aggregate preflight exceeded", AGGREGATE_TIMEOUT_SECONDS, "seconds before launching", rel)
            failed.append((str(rel), "aggregate-timeout"))
            break

        print("\n===", rel, "===")
        try:
            returncode = run_gate(path, child_env, child_timeout)
        except GateTimeoutError as exc:
            timeout_reason = "aggregate-timeout" if remaining_child_timeout(aggregate_started_at) <= 0 else "timeout"
            reason = timeout_reason
            if exc.cleanup_error is not None:
                reason += "-cleanup-failed"
            print("ERROR:", rel, "timed out after", exc.timeout_seconds, "seconds (", reason, ").")
            if exc.cleanup_error is not None:
                print("ERROR:", rel, "owned process-tree cleanup failed:", exc.cleanup_error)
            failed.append((str(rel), reason))
            if timeout_reason == "aggregate-timeout" or exc.cleanup_error is not None:
                break
            continue
        except OSError as exc:
            print("ERROR: failed to start", rel, "-", exc)
            failed.append((str(rel), "launch"))
            continue

        if returncode != 0:
            failed.append((str(rel), "exit=" + str(returncode)))

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
