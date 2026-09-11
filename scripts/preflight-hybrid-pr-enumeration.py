#!/usr/bin/env python3
"""Guard Hybrid PR enumeration against Bash process-substitution fail-open behavior."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"

UNSAFE_PROCESS_SUBSTITUTION_RE = re.compile(
    r"mapfile\s+-t\s+pr_numbers\s*<\s*<\s*\(",
    re.MULTILINE,
)
REQUIRED_IN_ORDER = (
    ("disable errexit for status capture", "set +e"),
    ("captured paginated PR query", 'pr_rows="$('),
    (
        "paginated PR API",
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100"',
    ),
    ("PR query exit capture", "pr_query_status=$?"),
    ("restore errexit", "set -e"),
    ("PR query failure check", "if (( pr_query_status != 0 )); then"),
    ("explicit fail-closed diagnostic", "Could not enumerate open main-targeting PRs"),
    ("parse captured PR rows", 'done <<< "${pr_rows}"'),
)


def contract_errors(text: str) -> list[str]:
    errors: list[str] = []
    if UNSAFE_PROCESS_SUBSTITUTION_RE.search(text):
        errors.append("Hybrid PR enumeration still uses unchecked process substitution")

    last_index = -1
    for label, token in REQUIRED_IN_ORDER:
        index = text.find(token, last_index + 1)
        if index < 0:
            errors.append(f"missing or out-of-order {label}")
            continue
        last_index = index
    return errors


def self_test() -> list[str]:
    errors: list[str] = []
    unsafe_variants = (
        'mapfile -t pr_numbers < <(gh api --paginate "repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100")',
        "mapfile  -t  pr_numbers\n  <  < (\n    gh api --paginate whatever\n  )",
    )
    for unsafe in unsafe_variants:
        if not any("unchecked process substitution" in error for error in contract_errors(unsafe)):
            errors.append("guard failed to reject whitespace-varied Hybrid PR process substitution")

    safe = "\n".join(
        [
            "set +e",
            'pr_rows="$(' ,
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100"',
            ')"',
            "pr_query_status=$?",
            "set -e",
            "if (( pr_query_status != 0 )); then",
            '  echo "Could not enumerate open main-targeting PRs" >&2',
            '  exit "${pr_query_status}"',
            "fi",
            "pr_numbers=()",
            'while IFS= read -r number; do [[ -n "${number}" ]] && pr_numbers+=("${number}"); done <<< "${pr_rows}"',
        ]
    )
    if contract_errors(safe):
        errors.append("guard rejected the intended status-checked Hybrid PR enumeration contract")

    reordered = safe.replace("pr_query_status=$?\nset -e", "set -e\npr_query_status=$?")
    if not contract_errors(reordered):
        errors.append("guard failed to reject reordered status-capture semantics")
    return errors


def main() -> int:
    errors = self_test()
    try:
        if not WORKFLOW.is_file():
            errors.append("missing Hybrid PR coordinator workflow")
        else:
            errors.extend(contract_errors(WORKFLOW.read_text(encoding="utf-8")))
    except (OSError, UnicodeError) as exc:
        errors.append(f"could not inspect Hybrid PR coordinator workflow: {exc}")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PASS: Hybrid coordinator fails closed when open-PR enumeration fails.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
