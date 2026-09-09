#!/usr/bin/env python3
"""Guard Hybrid PR enumeration against Bash process-substitution fail-open behavior."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"

UNSAFE_PROCESS_SUBSTITUTION = (
    "mapfile -t pr_numbers < <(gh api --paginate \"repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100\""
)
REQUIRED = {
    "captured paginated PR query": "pr_rows=\"$(",
    "paginated PR API": "gh api --paginate \"repos/${GITHUB_REPOSITORY}/pulls?state=open&base=main&per_page=100\"",
    "PR query exit capture": "pr_query_status=$?",
    "PR query failure check": "if (( pr_query_status != 0 )); then",
    "explicit fail-closed diagnostic": "Could not enumerate open main-targeting PRs",
    "parse captured PR rows": "done <<< \"${pr_rows}\"",
}


def contract_errors(text: str) -> list[str]:
    errors: list[str] = []
    if UNSAFE_PROCESS_SUBSTITUTION in text:
        errors.append("Hybrid PR enumeration still uses unchecked process substitution")
    for label, token in REQUIRED.items():
        if token not in text:
            errors.append(f"missing {label}")
    return errors


def self_test() -> list[str]:
    errors: list[str] = []
    unsafe = UNSAFE_PROCESS_SUBSTITUTION + ")"
    if not contract_errors(unsafe):
        errors.append("guard failed to reject unchecked Hybrid PR process substitution")

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
