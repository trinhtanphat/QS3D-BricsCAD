#!/usr/bin/env python3
"""Guard V25 automatic-dispatch reservation enumeration against Bash fail-open I/O."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"

RESERVATION_API = r'gh\s+api\s+--paginate\s+"repos/\$\{GITHUB_REPOSITORY\}/issues/\$\{reservation_issue\}/comments"'
UNSAFE_PROCESS_SUBSTITUTION = re.compile(
    rf'done\s*<\s*<\(\s*{RESERVATION_API}',
    re.DOTALL,
)
CAPTURE = re.compile(
    rf'reservation_rows\s*=\s*"\$\(\s*{RESERVATION_API}.*?\)"',
    re.DOTALL,
)
STATUS = re.compile(r'reservation_query_status\s*=\s*\$\?')
FAIL_CHECK = re.compile(r'if\s+\(\(\s*reservation_query_status\s*!=\s*0\s*\)\)\s*;\s*then')
FAIL_DIAGNOSTIC = re.compile(r'Could not enumerate V25 preview reservation comments')
FAIL_EXIT = re.compile(r'exit\s+"\$\{reservation_query_status\}"')
PARSE_CAPTURED = re.compile(r'done\s*<<<\s*"\$\{reservation_rows\}"')
SET_PLUS_E = re.compile(r'(?m)^\s*set\s+\+e\s*$')
SET_MINUS_E = re.compile(r'(?m)^\s*set\s+-e\s*$')


def _search_after(pattern: re.Pattern[str], text: str, start: int) -> re.Match[str] | None:
    return pattern.search(text, start)


def contract_errors(text: str) -> list[str]:
    errors: list[str] = []
    if UNSAFE_PROCESS_SUBSTITUTION.search(text):
        errors.append("reservation enumeration still uses unchecked process substitution")

    capture = CAPTURE.search(text)
    if capture is None:
        errors.append("missing captured paginated reservation comment query")
        return errors

    set_plus_e_matches = [match for match in SET_PLUS_E.finditer(text, 0, capture.start())]
    if not set_plus_e_matches:
        errors.append("reservation query capture must be preceded by set +e")

    status = _search_after(STATUS, text, capture.end())
    if status is None:
        errors.append("reservation query exit status must be captured immediately after the API command substitution")
        return errors

    set_minus_e = _search_after(SET_MINUS_E, text, status.end())
    if set_minus_e is None:
        errors.append("reservation query status capture must restore set -e before admission continues")
        return errors

    fail_check = _search_after(FAIL_CHECK, text, set_minus_e.end())
    if fail_check is None:
        errors.append("reservation query failure check must follow the captured status")
        return errors

    diagnostic = _search_after(FAIL_DIAGNOSTIC, text, fail_check.end())
    if diagnostic is None:
        errors.append("reservation query failure path is missing its explicit diagnostic")
        return errors

    fail_exit = _search_after(FAIL_EXIT, text, diagnostic.end())
    if fail_exit is None:
        errors.append("reservation query failure path must exit with the captured API status")
        return errors

    parse = _search_after(PARSE_CAPTURED, text, fail_exit.end())
    if parse is None:
        errors.append("captured reservation rows must be parsed only after the API status is admitted")

    return errors


def self_test() -> list[str]:
    errors: list[str] = []

    unsafe = "\n".join(
        [
            "while IFS= read -r reservation; do",
            "  :",
            "done < <(",
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments" \\',
            "    --jq '.[] | .body'",
            ")",
            "set +e",
            'reservation_rows="$(gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments")"',
            "reservation_query_status=$?",
            "set -e",
            "if (( reservation_query_status != 0 )); then",
            '  echo "Could not enumerate V25 preview reservation comments" >&2',
            '  exit "${reservation_query_status}"',
            "fi",
            'while read -r row; do :; done <<< "${reservation_rows}"',
        ]
    )
    if not any("unchecked process substitution" in error for error in contract_errors(unsafe)):
        errors.append("guard failed to reject multiline unchecked process substitution")

    safe = "\n".join(
        [
            "set +e",
            'reservation_rows="$(' ,
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments" \\',
            "    --jq '.[] | .body'",
            ')"',
            "reservation_query_status=$?",
            "set -e",
            "if (( reservation_query_status != 0 )); then",
            '  echo "Could not enumerate V25 preview reservation comments" >&2',
            '  exit "${reservation_query_status}"',
            "fi",
            'while read -r row; do :; done <<< "${reservation_rows}"',
        ]
    )
    if contract_errors(safe):
        errors.append("guard rejected the intended status-checked capture contract")

    misordered = "\n".join(
        [
            "set +e",
            "reservation_query_status=$?",
            "set -e",
            "if (( reservation_query_status != 0 )); then",
            '  echo "Could not enumerate V25 preview reservation comments" >&2',
            '  exit "${reservation_query_status}"',
            "fi",
            'while read -r row; do :; done <<< "${reservation_rows}"',
            'reservation_rows="$(' ,
            '  gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
            ')"',
        ]
    )
    if not contract_errors(misordered):
        errors.append("guard failed to reject misordered status-check contract")

    missing_exit = safe.replace('  exit "${reservation_query_status}"\n', "")
    if not contract_errors(missing_exit):
        errors.append("guard failed to reject a failure branch that does not propagate API status")

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
