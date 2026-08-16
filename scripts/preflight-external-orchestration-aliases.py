#!/usr/bin/env python3
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
SCAN_SUFFIXES = {".md", ".txt", ".yml", ".yaml", ".json", ".toml", ".py", ".ps1", ".sh"}
SELF_PATH = "scripts/preflight-external-orchestration-aliases.py"
REGRESSION_PATH = "scripts/preflight-external-orchestration-aliases-regression.py"
EXCLUDED_PATHS = {SELF_PATH, REGRESSION_PATH}

# PR #1964 already protects the historical exact names. This guard covers a
# deliberately small set of obvious aliases so the same external scheduler
# topology cannot be reintroduced simply by renaming its repository artifact.
FORBIDDEN_PATH_MARKERS = (
    "hourly-control",
    "scheduler-control",
    "scheduled-worker",
    "worker-pool",
    "controller-worker",
)

CONTROL_SCHEDULER_RE = re.compile(
    r"(?im)\b(?:qs3d[-_ ]?)?control(?:ler)?\b[^\n]{0,120}"
    r"\b(?:schedule|scheduler|worker|lane|task)\b"
)
NUMBERED_WORKER_RE = re.compile(r"(?im)\b(?:qs3d[-_ ]?)?worker[-_ ]?0?[1-9]\b")
POOL_SIZE_RE = re.compile(
    r"(?im)\b(?:five|six|5|6)\s+(?:active\s+)?(?:schedules|lanes|workers)\b"
)


def is_external_orchestration(relative: str, text: str) -> bool:
    lowered_relative = relative.lower()
    if any(marker in lowered_relative for marker in FORBIDDEN_PATH_MARKERS):
        return True

    # Avoid rejecting ordinary product prose merely because it uses words such
    # as "controller" or "worker". Content is suspicious only when controller
    # scheduling language is combined with an explicit numbered worker or a
    # five/six-lane pool declaration.
    return bool(
        CONTROL_SCHEDULER_RE.search(text)
        and (NUMBERED_WORKER_RE.search(text) or POOL_SIZE_RE.search(text))
    )


def scan_tree(root: Path) -> list[str]:
    failures: list[str] = []
    for path in root.rglob("*"):
        if not path.is_file() or ".git" in path.parts:
            continue

        relative = path.relative_to(root).as_posix()
        if relative in EXCLUDED_PATHS:
            continue

        lowered_relative = relative.lower()
        if any(marker in lowered_relative for marker in FORBIDDEN_PATH_MARKERS):
            failures.append(
                f"external scheduler/orchestration alias must stay outside the QS3D source tree: {relative}"
            )
            continue

        if path.suffix.lower() not in SCAN_SUFFIXES:
            continue

        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue

        if is_external_orchestration(relative, text):
            failures.append(
                f"external controller/worker scheduler topology leaked into repository content: {relative}"
            )

    return failures


def main() -> int:
    failures = scan_tree(ROOT)
    if failures:
        print("External orchestration alias preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: obvious external scheduler/controller-worker aliases stay outside the QS3D source tree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
