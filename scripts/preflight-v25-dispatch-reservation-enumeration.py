#!/usr/bin/env python3
"""Guard V25 automatic-dispatch reservation enumeration against Bash fail-open I/O."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"

UNSAFE_PROCESS_SUBSTITUTION = (
    "done < <(gh api --paginate \"repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments\""
)
REQUIRED = {
    "captured reservation comment query": "reservation_rows=\"$(",
    "paginated reservation comment API": "gh api --paginate \"repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments\"",
    "reservation query exit capture": "reservation_query_status=$?",
    "reservation query failure check": "if (( reservation_query_status != 0 )); then",
    "explicit fail-closed diagnostic": "Could not enumerate V25 preview reservation comments",
    "parse captured rows": "done <<< \"${reservation_rows}\"",
}


def contract_errors(text: str) -> list[str]:
    errors: list[str] = []
    if UNSAFE_PROCESS_SUBSTITUTION in text:
        errors.append("reservation enumeration still uses unchecked process substitution")
    for label, token in REQUIRED.items():
        if token not in text:
            errors.append(f"missing {label}")
    return errors


def self_test() -> list[str]:
    errors: list[str] = []
    unsafe = f"while read -r row; do :; {UNSAFE_PROCESS_SUBSTITUTION})"
    if not contract_errors(unsafe):
        errors.append("guard failed to reject unchecked process substitution")

    multiline_unsafe = "\n".join(
        [
            "while IFS= read -r reservation; do",
            "  :",
            "done < <(",
            "  gh api --paginate \"repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments\" \\",
            "    --jq '.[] | .body'",
            ")",
        ]
    )
    if not contract_errors(multiline_unsafe):
        errors.append("guard failed to reject multiline unchecked process substitution")

    safe = "\n".join(
        [
            'set +e',
            'reservation_rows="$(' ,
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
            ')"',
            'reservation_query_status=$?',
            'set -e',
            'if (( reservation_query_status != 0 )); then',
            '  echo "Could not enumerate V25 preview reservation comments" >&2',
            '  exit "${reservation_query_status}"',
            'fi',
            'while read -r row; do :; done <<< "${reservation_rows}"',
        ]
    )
    if contract_errors(safe):
        errors.append("guard rejected the intended status-checked capture contract")

    misordered = "\n".join(
        [
            'set +e',
            'reservation_query_status=$?',
            'if (( reservation_query_status != 0 )); then',
            '  echo "Could not enumerate V25 preview reservation comments" >&2',
            'fi',
            'while read -r row; do :; done <<< "${reservation_rows}"',
            'reservation_rows="$(' ,
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
            ')"',
        ]
    )
    if not contract_errors(misordered):
        errors.append("guard failed to reject misordered status-check contract")
    return errors


def main() -> int:
    errors = self_test()
    try:
        if not WORKFLOW.is_file():
            errors.append("missing V25 automatic-dispatch workflow")
        else:
            errors.extend(contract_errors(WORKFLOW.read_text(encoding="utf-8")))
    except (OSError, UnicodeError) as exc:
        errors.append(f"could not inspect V25 automatic-dispatch workflow: {exc}")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PASS: V25 automatic dispatcher fails closed when reservation enumeration fails.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
