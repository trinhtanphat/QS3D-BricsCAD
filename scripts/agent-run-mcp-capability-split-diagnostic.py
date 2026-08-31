#!/usr/bin/env python3
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "agent-apply-mcp-capability-split.py"
FAILURE = ROOT / "agent-mcp-capability-split.failure.txt"


def run(command, check=True):
    completed = subprocess.run(command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    print(completed.stdout or "", flush=True)
    if check and completed.returncode != 0:
        raise RuntimeError("command failed: " + " ".join(command))
    return completed


def git_identity():
    run(["git", "config", "user.name", "github-actions[bot]"])
    run(["git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com"])


def commit_and_push(message, paths):
    git_identity()
    run(["git", "add", "--"] + [str(path.relative_to(ROOT)) for path in paths])
    staged = subprocess.run(["git", "diff", "--cached", "--quiet"], cwd=ROOT)
    if staged.returncode == 0:
        raise RuntimeError("no staged changes available for " + message)
    run(["git", "commit", "-m", message])
    branch = run(["git", "branch", "--show-current"]).stdout.strip()
    run(["git", "push", "origin", "HEAD:" + branch])


def patched_target_source(original):
    replacements = [
        ("ClassifyFailure(string toolName, Exception exception)", "ClassifyFailure(string toolName, Exception? exception)"),
        ("IsArgumentFailure(Exception exception, string message)", "IsArgumentFailure(Exception? exception, string message)"),
        ("IsQs3dSourceBug(Exception exception, string message)", "IsQs3dSourceBug(Exception? exception, string message)"),
    ]
    patched = original
    for old, new in replacements:
        if patched.count(old) != 1:
            raise RuntimeError("nullable repair target mismatch: " + old)
        patched = patched.replace(old, new, 1)
    return patched


def main():
    original_target = TARGET.read_text(encoding="utf-8")
    TARGET.write_text(patched_target_source(original_target), encoding="utf-8")
    try:
        completed = subprocess.run(
            [sys.executable, str(TARGET)], cwd=ROOT, text=True,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    finally:
        TARGET.write_text(original_target, encoding="utf-8")

    output = completed.stdout or ""
    print(output, flush=True)

    if completed.returncode != 0:
        FAILURE.write_text(output, encoding="utf-8")
        commit_and_push("chore(mcp): capture one-shot failure diagnostics", [FAILURE])
        return completed.returncode

    if FAILURE.exists():
        FAILURE.unlink()
    changed = run(["git", "status", "--porcelain"]).stdout.splitlines()
    paths = []
    for line in changed:
        raw = line[3:].strip()
        if " -> " in raw:
            raw = raw.split(" -> ", 1)[1]
        path = ROOT / raw
        if path.exists() or raw == "agent-mcp-capability-split.failure.txt":
            paths.append(path)
    if not paths:
        raise RuntimeError("one-shot applicator reported success but produced no source changes")
    commit_and_push("feat(mcp): split CAD and QS3D capability lanes", paths)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
