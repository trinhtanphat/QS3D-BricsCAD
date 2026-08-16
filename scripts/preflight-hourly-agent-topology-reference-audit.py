from __future__ import annotations

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
SELF = Path(__file__).resolve()

# Task 0 owns the canonical policy and its primary preflight. This audit is a
# second, independent enforcement point for live references elsewhere.
EXCLUDED_EXACT = {
    ROOT / "docs" / "HOURLY-AGENT-CONTROL.md",
    ROOT / "scripts" / "preflight-hourly-agent-control.py",
    SELF,
}

# Historical handoff/evidence documents are intentionally immutable records.
HISTORICAL_NAME_PREFIXES = (
    "AGENT-HANDOFF-",
    "AGENT-CONCURRENCY-HANDOFF-",
)

TEXT_SUFFIXES = {
    ".md",
    ".txt",
    ".json",
    ".yml",
    ".yaml",
    ".toml",
    ".ini",
    ".cfg",
    ".py",
    ".ps1",
    ".sh",
}

# The active topology is exactly one controller plus Workers 01-04. These
# patterns represent stale six-lane/five-worker contracts, not elapsed-time
# wording or historical evidence.
FORBIDDEN_PATTERNS = (
    re.compile(r"\bQS3D[-_]WORKER[-_]0?5\b", re.IGNORECASE),
    re.compile(r"\bworker[-_ ]0?5\b", re.IGNORECASE),
    re.compile(r"\bsix\s+(?:active\s+)?schedules\b", re.IGNORECASE),
    re.compile(r"\b6\s+(?:active\s+)?schedules\b", re.IGNORECASE),
    re.compile(r"\bfive\s+workers\b", re.IGNORECASE),
    re.compile(r"\b5\s+workers\b", re.IGNORECASE),
    re.compile(r"\bsix\s+assignments\b", re.IGNORECASE),
    re.compile(r"\b6\s+assignments\b", re.IGNORECASE),
)

TOPOLOGY_MARKERS = (
    "qs3d-control",
    "qs3d_worker",
    "qs3d-worker",
    "hourly controller",
    "hourly-control",
    "hourly control",
)


def is_live_candidate(path: Path, text: str) -> bool:
    if path in EXCLUDED_EXACT:
        return False

    rel = path.relative_to(ROOT)
    if rel.parts[:2] == (".github", "workflows"):
        return False

    if rel.parts and rel.parts[0] == "docs":
        if any(path.name.startswith(prefix) for prefix in HISTORICAL_NAME_PREFIXES):
            return False

    lowered = text.lower()
    return any(marker in lowered for marker in TOPOLOGY_MARKERS)


def iter_text_files() -> list[Path]:
    roots = [
        ROOT / "AGENTS.md",
        ROOT / "README.md",
        ROOT / "CI_POLICY.md",
        ROOT / "CONTRIBUTING.md",
        ROOT / "docs",
        ROOT / "scripts",
    ]

    files: list[Path] = []
    for root in roots:
        if root.is_file():
            files.append(root)
            continue
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path.suffix.lower() in TEXT_SUFFIXES:
                files.append(path)
    return sorted(set(files))


def main() -> int:
    violations: list[str] = []

    for path in iter_text_files():
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue

        if not is_live_candidate(path, text):
            continue

        for line_number, line in enumerate(text.splitlines(), start=1):
            for pattern in FORBIDDEN_PATTERNS:
                if pattern.search(line):
                    rel = path.relative_to(ROOT)
                    violations.append(f"{rel}:{line_number}: {line.strip()}")
                    break

    if violations:
        print("FAIL: stale hourly-agent topology references found outside Task 0 scope:")
        for violation in violations:
            print(f" - {violation}")
        print("Canonical topology is 5 schedules total: QS3D-CONTROL + Workers 01-04.")
        return 1

    print(
        "PASS: no live normative scheduler references outside Task 0 scope "
        "claim Worker-05, six schedules, five workers, or six assignments."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
