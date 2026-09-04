#!/usr/bin/env python3
from dataclasses import dataclass
from pathlib import Path
import os
import signal
import stat
import subprocess
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
SELF = Path(__file__).resolve()
CHILD_TIMEOUT_SECONDS = 180
AGGREGATE_TIMEOUT_SECONDS = 15 * 60
PROCESS_TREE_CLEANUP_TIMEOUT_SECONDS = 10
OUTPUT_DRAIN_JOIN_TIMEOUT_SECONDS = 10
INPUT_FEED_JOIN_TIMEOUT_SECONDS = 10
MAX_FEATURE_GATE_OUTPUT_BYTES = 1024 * 1024
OUTPUT_READ_CHUNK_BYTES = 64 * 1024
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
_GATE_EXEC_WRAPPER = (
    "import os, sys\n"
    "source = sys.stdin.buffer.read()\n"
    "filename = sys.argv[1]\n"
    "sys.argv[:] = [filename]\n"
    "sys.path[0] = os.path.dirname(os.path.abspath(filename))\n"
    "namespace = {'__name__': '__main__', '__file__': filename, '__package__': None, '__cached__': None, '__spec__': None}\n"
    "exec(compile(source, filename, 'exec'), namespace, namespace)\n"
)


@dataclass(frozen=True)
class AdmittedGate:
    path: Path
    source: bytes


class GateTimeoutError(TimeoutError):
    def __init__(self, timeout_seconds, cleanup_error=None, output_error=None, input_error=None):
        super().__init__("feature preflight timed out")
        self.timeout_seconds = timeout_seconds
        self.cleanup_error = cleanup_error
        self.output_error = output_error
        self.input_error = input_error


class GateOutputError(RuntimeError):
    pass


class GateInputError(RuntimeError):
    pass


def _is_within(candidate, root):
    try:
        candidate.relative_to(root)
        return True
    except ValueError:
        return False


def _relative_candidate(path):
    try:
        return path.relative_to(ROOT)
    except ValueError as exc:
        raise RuntimeError("feature preflight gate is outside repository root: " + str(path)) from exc


def _metadata_type_error(metadata):
    if stat.S_ISLNK(metadata.st_mode):
        return "symlink"
    if not stat.S_ISREG(metadata.st_mode):
        return "non-regular"
    return None


def _same_opened_file(before, opened):
    before_dev = getattr(before, "st_dev", 0)
    before_ino = getattr(before, "st_ino", 0)
    opened_dev = getattr(opened, "st_dev", 0)
    opened_ino = getattr(opened, "st_ino", 0)
    if before_dev and before_ino and opened_dev and opened_ino:
        return (before_dev, before_ino) == (opened_dev, opened_ino)
    return (
        before.st_size == opened.st_size
        and getattr(before, "st_mtime_ns", None) == getattr(opened, "st_mtime_ns", None)
        and getattr(before, "st_ctime_ns", None) == getattr(opened, "st_ctime_ns", None)
    )


def admit_gate(path, allowed_root=ROOT):
    path = Path(path)
    root = Path(allowed_root).resolve(strict=True)
    try:
        metadata = os.lstat(path)
    except OSError as exc:
        raise RuntimeError("cannot inspect feature preflight gate " + str(path) + ": " + str(exc)) from exc
    type_error = _metadata_type_error(metadata)
    if type_error is not None:
        raise RuntimeError("feature preflight gate " + str(path) + " is " + type_error)
    if metadata.st_size > MAX_FEATURE_GATE_SOURCE_BYTES:
        raise RuntimeError(
            "feature preflight gate " + str(path) + " source size " + str(metadata.st_size)
            + " bytes exceeds maximum " + str(MAX_FEATURE_GATE_SOURCE_BYTES)
        )
    try:
        resolved = path.resolve(strict=True)
    except (OSError, RuntimeError) as exc:
        raise RuntimeError("cannot resolve feature preflight gate " + str(path) + ": " + str(exc)) from exc
    if not _is_within(resolved, root):
        raise RuntimeError("feature preflight gate escapes allowed root: " + str(path))

    flags = os.O_RDONLY | getattr(os, "O_BINARY", 0) | getattr(os, "O_NOFOLLOW", 0)
    fd = None
    try:
        fd = os.open(path, flags)
        opened = os.fstat(fd)
        opened_type_error = _metadata_type_error(opened)
        if opened_type_error is not None:
            raise RuntimeError("opened feature preflight gate " + str(path) + " is " + opened_type_error)
        if not _same_opened_file(metadata, opened):
            raise RuntimeError("feature preflight gate changed identity between admission and open: " + str(path))
        if opened.st_size > MAX_FEATURE_GATE_SOURCE_BYTES:
            raise RuntimeError(
                "feature preflight gate " + str(path) + " source exceeds maximum "
                + str(MAX_FEATURE_GATE_SOURCE_BYTES) + " bytes"
            )
        chunks = []
        total = 0
        while total <= MAX_FEATURE_GATE_SOURCE_BYTES:
            chunk = os.read(fd, min(64 * 1024, MAX_FEATURE_GATE_SOURCE_BYTES + 1 - total))
            if not chunk:
                break
            chunks.append(chunk)
            total += len(chunk)
        if total > MAX_FEATURE_GATE_SOURCE_BYTES:
            raise RuntimeError(
                "feature preflight gate " + str(path) + " source exceeds maximum "
                + str(MAX_FEATURE_GATE_SOURCE_BYTES) + " bytes"
            )
        source = b"".join(chunks)
    except OSError as exc:
        raise RuntimeError("cannot safely open/read feature preflight gate " + str(path) + ": " + str(exc)) from exc
    finally:
        if fd is not None:
            os.close(fd)
    return AdmittedGate(path=path, source=source)


def validate_candidates(candidates):
    candidates = list(candidates)
    if len(candidates) > MAX_FEATURE_GATES:
        raise RuntimeError(
            "feature preflight discovery count " + str(len(candidates)) + " exceeds maximum " + str(MAX_FEATURE_GATES)
        )

    by_casefold = {}
    for path in candidates:
        _relative_candidate(path)
        key = path.name.casefold()
        by_casefold.setdefault(key, []).append(path)
    collisions = [paths for paths in by_casefold.values() if len(paths) > 1]
    if collisions:
        messages = []
        for paths in sorted(collisions, key=lambda group: (group[0].name.casefold(), group[0].name)):
            names = ", ".join(sorted(str(_relative_candidate(path)) for path in paths))
            messages.append("case-insensitive preflight filename collision: " + names)
        raise RuntimeError("unsafe or ambiguous feature preflight discovery: " + "; ".join(messages))

    ordered = sorted(candidates, key=lambda path: (path.name.casefold(), path.name))
    return [admit_gate(path, allowed_root=ROOT) for path in ordered]


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
                        "case-insensitive preflight filename collision with aggregate runner: " + entry.name
                    )
                candidates.append(path)
                if len(candidates) > MAX_FEATURE_GATES:
                    raise RuntimeError(
                        "feature preflight discovery count " + str(len(candidates)) + " exceeds maximum "
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
                ["taskkill", "/PID", str(process.pid), "/T", "/F"], check=False,
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
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


def _write_output_bytes(target, data):
    if hasattr(target, "buffer"):
        target.buffer.write(data)
        target.buffer.flush()
        return
    try:
        target.write(data)
    except TypeError:
        target.write(data.decode("utf-8", errors="replace"))
    if hasattr(target, "flush"):
        target.flush()


def copy_bounded_output(stream, target=None, limit_bytes=MAX_FEATURE_GATE_OUTPUT_BYTES):
    if limit_bytes < 0:
        raise ValueError("feature gate output limit must be non-negative")
    output = sys.stdout if target is None else target
    emitted = 0
    truncated = False
    while True:
        chunk = stream.read(OUTPUT_READ_CHUNK_BYTES)
        if not chunk:
            break
        if not isinstance(chunk, bytes):
            raise GateOutputError("feature preflight output stream returned non-bytes data")
        remaining = max(0, limit_bytes - emitted)
        if remaining:
            visible = chunk[:remaining]
            _write_output_bytes(output, visible)
            emitted += len(visible)
        if len(chunk) > remaining:
            truncated = True
    return emitted, truncated


def _drain_gate_output(stream, state):
    try:
        emitted, truncated = copy_bounded_output(stream)
        state["emitted"] = emitted
        state["truncated"] = truncated
    except Exception as exc:
        state["error"] = exc
    finally:
        try:
            stream.close()
        except OSError:
            pass


def _feed_gate_source(stream, source, state):
    try:
        stream.write(source)
        stream.flush()
    except (BrokenPipeError, OSError) as exc:
        state["error"] = exc
    finally:
        try:
            stream.close()
        except OSError:
            pass


def _finish_output_drain(thread, state):
    thread.join(timeout=OUTPUT_DRAIN_JOIN_TIMEOUT_SECONDS)
    if thread.is_alive():
        return "output-drain-timeout"
    error = state.get("error")
    if error is not None:
        return "output-drain-error=" + type(error).__name__
    return None


def _finish_input_feed(thread, state):
    thread.join(timeout=INPUT_FEED_JOIN_TIMEOUT_SECONDS)
    if thread.is_alive():
        return "input-feed-timeout"
    error = state.get("error")
    if error is not None:
        return "input-feed-error=" + type(error).__name__
    return None


def run_gate(gate, child_env, timeout_seconds):
    if not isinstance(gate, AdmittedGate):
        raise GateInputError("feature preflight execution requires an admitted gate")
    process = subprocess.Popen(
        [sys.executable, "-c", _GATE_EXEC_WRAPPER, str(gate.path)],
        cwd=str(ROOT), env=child_env, stdin=subprocess.PIPE,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, **_process_group_launch_kwargs(),
    )
    if process.stdout is None or process.stdin is None:
        try:
            process.kill()
        except OSError:
            pass
        raise GateOutputError("feature preflight input/output pipe was not created")

    output_state = {}
    output_thread = threading.Thread(
        target=_drain_gate_output, args=(process.stdout, output_state),
        name="preflight-output-" + str(process.pid), daemon=True,
    )
    input_state = {}
    input_thread = threading.Thread(
        target=_feed_gate_source, args=(process.stdin, gate.source, input_state),
        name="preflight-input-" + str(process.pid), daemon=True,
    )
    output_thread.start()
    input_thread.start()

    timed_out = False
    timeout_exception = None
    cleanup_error = None
    returncode = None
    try:
        returncode = process.wait(timeout=timeout_seconds)
    except subprocess.TimeoutExpired as exc:
        timed_out = True
        timeout_exception = exc
        cleanup_error = _terminate_process_tree(process)

    input_error = _finish_input_feed(input_thread, input_state)
    output_error = _finish_output_drain(output_thread, output_state)
    if output_state.get("truncated"):
        label = str(gate.path)
        try:
            label = str(gate.path.relative_to(ROOT))
        except ValueError:
            pass
        print("\n[aggregate output truncated after", MAX_FEATURE_GATE_OUTPUT_BYTES, "bytes for", label, "]")
    if timed_out:
        raise GateTimeoutError(timeout_seconds, cleanup_error, output_error, input_error) from timeout_exception
    if input_error is not None:
        raise GateInputError("feature preflight source feed failed: " + input_error)
    if output_error is not None:
        raise GateOutputError("feature preflight output drain failed: " + output_error)
    return returncode


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
    for gate in gates:
        print(" -", gate.path.relative_to(ROOT))

    failed = []
    child_env = build_child_env()
    for gate in gates:
        rel = gate.path.relative_to(ROOT)
        child_timeout = remaining_child_timeout(aggregate_started_at)
        if child_timeout <= 0:
            print("ERROR: aggregate preflight exceeded", AGGREGATE_TIMEOUT_SECONDS, "seconds before launching", rel)
            failed.append((str(rel), "aggregate-timeout"))
            break
        print("\n===", rel, "===")
        try:
            returncode = run_gate(gate, child_env, child_timeout)
        except GateTimeoutError as exc:
            timeout_reason = "aggregate-timeout" if remaining_child_timeout(aggregate_started_at) <= 0 else "timeout"
            reason = timeout_reason
            if exc.cleanup_error is not None:
                reason += "-cleanup-failed"
            if exc.output_error is not None:
                reason += "-output-failed"
            if exc.input_error is not None:
                reason += "-input-failed"
            print("ERROR:", rel, "timed out after", exc.timeout_seconds, "seconds (", reason, ").")
            if exc.cleanup_error is not None:
                print("ERROR:", rel, "owned process-tree cleanup failed:", exc.cleanup_error)
            if exc.output_error is not None:
                print("ERROR:", rel, "output drain failed:", exc.output_error)
            if exc.input_error is not None:
                print("ERROR:", rel, "source feed failed:", exc.input_error)
            failed.append((str(rel), reason))
            if timeout_reason == "aggregate-timeout" or exc.cleanup_error is not None or exc.output_error is not None or exc.input_error is not None:
                break
            continue
        except (GateOutputError, GateInputError) as exc:
            print("ERROR:", rel, "I/O handling failed -", exc)
            failed.append((str(rel), "io"))
            break
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