#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SELF = "scripts/preflight-repository-orchestration-aliases.py"
TEXT_SUFFIXES = {".md", ".txt", ".yml", ".yaml", ".json", ".toml", ".py", ".ps1", ".sh"}
PATH_MARKERS = (
    "hourly-agent-control",
    "hourly-control",
    "scheduled-agent-control",
    "scheduled-agent-pool",
    "agent-orchestration",
    "controller-worker-pool",
    "control-worker-pool",
)
CONTROL_RE = re.compile(r"\b(?:qs3d[-_ ]?)?control(?:ler)?\b", re.IGNORECASE)
WORKER_RE = re.compile(r"\b(?:qs3d[-_ ]?)?worker[-_ ]?(?:0?[1-9]|[1-9][0-9])\b", re.IGNORECASE)
SCHEDULE_RE = re.compile(r"\b(?:hourly|scheduled|schedule|scheduler|recurring|automation)\b", re.IGNORECASE)


def looks_like_external_orchestration(relative: str, text: str) -> bool:
    lowered = relative.lower()
    if any(marker in lowered for marker in PATH_MARKERS):
        return True
    return bool(CONTROL_RE.search(text) and WORKER_RE.search(text) and SCHEDULE_RE.search(text))


def scan(root: Path) -> list[str]:
    failures: list[str] = []
    for path in root.rglob("*"):
        if not path.is_file() or ".git" in path.parts:
            continue
        relative = path.relative_to(root).as_posix()
        if relative == SELF or path.suffix.lower() not in TEXT_SUFFIXES:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        if looks_like_external_orchestration(relative, text):
            failures.append(relative)
    return failures


def hermetic_regression() -> list[str]:
    failures: list[str] = []
    positive = {
        "docs/hourly-control.md": "ordinary prose",
        "ops/coordination.md": "CONTROL schedule dispatches WORKER-04 every hour",
        "ops/pool.yaml": "scheduler: hourly\ncontroller: QS3D control\nlane: worker_3\n",
    }
    negative = {
        "docs/product-control.md": "The quantity controller validates worker data structures.",
        "docs/release.md": "The scheduled release remains manual-only; no worker pool exists here.",
        "scripts/helper.py": "def control_worker_payload(value):\n    return value\n",
    }
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        for relative, text in {**positive, **negative}.items():
            path = root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8")
        detected = set(scan(root))
        for relative in positive:
            if relative not in detected:
                failures.append(f"alias regression failed to reject orchestration artifact: {relative}")
        for relative in negative:
            if relative in detected:
                failures.append(f"alias regression false-positive on product content: {relative}")
    return failures


def main() -> int:
    failures = hermetic_regression()
    leaked = scan(ROOT)
    if leaked:
        failures.extend(
            f"external scheduler/controller-worker orchestration alias leaked into repository content: {relative}"
            for relative in leaked
        )
    if failures:
        print("Repository orchestration alias preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1
    print("PASS: external scheduler/controller-worker aliases remain outside the QS3D source tree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
