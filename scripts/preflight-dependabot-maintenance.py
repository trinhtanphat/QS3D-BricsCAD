#!/usr/bin/env python3
"""Fail closed if the bounded Dependabot maintenance topology drifts.

Dependabot is allowed to own generated dependency-update branches/PRs, but the
repository intentionally keeps that boundary narrow.  This guard avoids the
weaker "required substrings exist somewhere" check: the complete committed
configuration must match the reviewed topology, and mutation probes prove the
guard rejects common scope-broadening changes.
"""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / ".github" / "dependabot.yml"

EXPECTED = """version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "03:00"
      timezone: "Asia/Ho_Chi_Minh"
    open-pull-requests-limit: 5
    commit-message:
      prefix: "chore(deps)"

  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "monthly"
      time: "03:30"
      timezone: "Asia/Ho_Chi_Minh"
    open-pull-requests-limit: 3
    commit-message:
      prefix: "chore(deps)"
"""


def normalize(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def validate(text: str) -> list[str]:
    text = normalize(text)
    if text == EXPECTED:
        return []

    errors = [
        "Dependabot configuration differs from the reviewed bounded maintenance topology"
    ]
    expected_lines = EXPECTED.splitlines()
    actual_lines = text.splitlines()
    for index in range(max(len(expected_lines), len(actual_lines))):
        expected = expected_lines[index] if index < len(expected_lines) else "<end-of-file>"
        actual = actual_lines[index] if index < len(actual_lines) else "<end-of-file>"
        if expected != actual:
            errors.append(
                f"first drift at line {index + 1}: expected {expected!r}, found {actual!r}"
            )
            break
    return errors


def mutation_self_test() -> list[str]:
    """Keep fail-closed behavior deterministic without a YAML dependency."""
    failures: list[str] = []
    mutations = {
        "duplicate ecosystem": EXPECTED + '\n  - package-ecosystem: "nuget"\n    directory: "/"\n',
        "non-root directory": EXPECTED.replace('directory: "/"', 'directory: "/src"', 1),
        "private registry": EXPECTED + "\nregistries:\n  private:\n    type: nuget-feed\n",
        "non-default target": EXPECTED.replace(
            '    directory: "/"\n    schedule:',
            '    directory: "/"\n    target-branch: "integration/deps"\n    schedule:',
            1,
        ),
        "broadened ecosystem": EXPECTED + '\n  - package-ecosystem: "docker"\n    directory: "/"\n',
        "unbounded PR count": EXPECTED.replace("open-pull-requests-limit: 5", "open-pull-requests-limit: 50", 1),
    }
    for label, candidate in mutations.items():
        if not validate(candidate):
            failures.append(f"mutation probe unexpectedly accepted: {label}")
    if validate(EXPECTED):
        failures.append("canonical Dependabot topology rejected by validator")
    return failures


def main() -> int:
    try:
        text = CONFIG.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        print(f"FAIL: cannot read Dependabot configuration safely: {exc}", file=sys.stderr)
        return 1

    errors = validate(text)
    errors.extend(mutation_self_test())
    if errors:
        print("Dependabot maintenance topology preflight FAILED", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1

    print("PASS: Dependabot maintenance remains exactly bounded to root github-actions weekly and NuGet monthly generated-PR updates")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
