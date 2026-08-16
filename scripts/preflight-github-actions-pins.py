#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
GITHUB_DIR = ROOT / ".github"
USES_RE = re.compile(r"^\s*(?:-\s*)?uses:\s*([^\s#]+)", re.IGNORECASE)
ACTION_PIN_RE = re.compile(r"^[^@\s]+@[0-9a-fA-F]{40}$")
DOCKER_DIGEST_RE = re.compile(r"^docker://[^\s@]+@sha256:[0-9a-fA-F]{64}$", re.IGNORECASE)


def candidate_files():
    if not GITHUB_DIR.exists():
        return []
    files = []
    for suffix in ("*.yml", "*.yaml"):
        files.extend(GITHUB_DIR.rglob(suffix))
    return sorted(set(files), key=lambda path: str(path).lower())


def validate_reference(reference):
    value = reference.strip().strip('"\'')
    if value.startswith("./"):
        return None
    if value.lower().startswith("docker://"):
        if DOCKER_DIGEST_RE.fullmatch(value):
            return None
        return "container action must use an immutable sha256 digest"
    if ACTION_PIN_RE.fullmatch(value):
        return None
    if "@" not in value:
        return "external action reference is missing an immutable commit SHA"
    return "external action must be pinned to one exact 40-hex commit SHA"


def main():
    failures = []
    scanned = 0
    for path in candidate_files():
        rel = path.relative_to(ROOT)
        try:
            lines = path.read_text(encoding="utf-8").splitlines()
        except (OSError, UnicodeError) as exc:
            failures.append((str(rel), 0, "could not read file: " + str(exc)))
            continue
        for line_number, line in enumerate(lines, start=1):
            match = USES_RE.match(line)
            if not match:
                continue
            scanned += 1
            reference = match.group(1)
            reason = validate_reference(reference)
            if reason:
                failures.append((str(rel), line_number, f"{reason}: {reference}"))

    if scanned == 0:
        print("ERROR: no GitHub Actions uses: references were discovered under .github.")
        return 1

    if failures:
        print("GitHub Actions immutable-pin preflight FAILED:")
        for path, line_number, reason in failures:
            location = f"{path}:{line_number}" if line_number else path
            print(f" - {location}: {reason}")
        print(f"FAILED with {len(failures)} mutable or invalid action reference(s).")
        return 1

    print(f"PASS: all {scanned} discovered GitHub Actions uses: references are local or immutably pinned.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
