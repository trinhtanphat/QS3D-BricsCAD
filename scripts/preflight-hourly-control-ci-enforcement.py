#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
WATCHED_DOC = "docs/HOURLY-AGENT-CONTROL.md"
SOURCE_CONDITION = "steps.scope.outputs.source_validation == 'true'"


def _step_block(lines, name):
    start = next((i for i, line in enumerate(lines) if line.strip() == f"- name: {name}"), None)
    if start is None:
        return None, None, ""
    end = len(lines)
    for index in range(start + 1, len(lines)):
        if re.match(r"^\s{6}- name:\s+", lines[index]):
            end = index
            break
        if re.match(r"^\s{2}[A-Za-z0-9_-]+:\s*$", lines[index]):
            end = index
            break
    return start, end, "\n".join(lines[start:end])


def inspect(text):
    errors = []
    lines = text.splitlines()

    push_start = next((i for i, line in enumerate(lines) if re.match(r'^\s{2}["\']?push["\']?\s*:', line)), None)
    pull_request_start = next((i for i, line in enumerate(lines) if re.match(r'^\s{2}["\']?pull_request["\']?\s*:', line)), None)
    if push_start is None or pull_request_start is None or pull_request_start <= push_start:
        errors.append("could not resolve shared-CI push trigger block")
    else:
        push_block = "\n".join(lines[push_start:pull_request_start])
        if f'"{WATCHED_DOC}"' not in push_block and f"'{WATCHED_DOC}'" not in push_block:
            errors.append(f"shared branch-push CI does not watch {WATCHED_DOC}")

    scope_start, _, scope_block = _step_block(lines, "Classify validation scope")
    generic_start, _, generic_block = _step_block(lines, "Generic source guard")
    aggregate_start, _, aggregate_block = _step_block(lines, "All discovered feature source guards")

    if scope_start is None:
        errors.append("missing validation-scope classification step")
    elif WATCHED_DOC not in scope_block:
        errors.append(f"validation scope does not classify {WATCHED_DOC} for source guards")

    if generic_start is None:
        errors.append("missing generic source guard step")
    elif SOURCE_CONDITION not in generic_block:
        errors.append("generic source guard is not gated by classified source_validation")

    if aggregate_start is None:
        errors.append("missing aggregate discovered feature source guard step")
    else:
        if "python scripts/preflight-all.py" not in aggregate_block:
            errors.append("aggregate source guard step no longer executes scripts/preflight-all.py")
        if SOURCE_CONDITION not in aggregate_block:
            errors.append("aggregate source guard is not gated by classified source_validation")

    if None not in (scope_start, generic_start, aggregate_start):
        if not (scope_start < generic_start < aggregate_start):
            errors.append("source validation steps must remain ordered: classify -> generic -> aggregate")

    core_start = next((i for i, line in enumerate(lines) if re.match(r'^\s{2}core:\s*$', line)), None)
    if core_start is None:
        errors.append("missing core job")
    else:
        core_header = "\n".join(lines[core_start:core_start + 8])
        if not re.search(r"^\s{4}needs:\s*preflight\s*$", core_header, re.MULTILINE):
            errors.append("core job no longer depends on preflight")

    return errors


def _remove_once(text, needle):
    if needle not in text:
        raise RuntimeError("self-test fixture missing expected token: " + needle)
    return text.replace(needle, "", 1)


def self_test(text):
    cases = [
        (
            "watched push path",
            _remove_once(text, f'      - "{WATCHED_DOC}"\n'),
            "shared branch-push CI does not watch",
        ),
        (
            "scope classification",
            _remove_once(text, f", '{WATCHED_DOC}'"),
            "validation scope does not classify",
        ),
        (
            "aggregate command",
            _remove_once(text, "        run: python scripts/preflight-all.py\n"),
            "aggregate source guard step no longer executes",
        ),
        (
            "aggregate source-validation condition",
            text.replace(
                "      - name: All discovered feature source guards\n"
                f"        if: ${{{{ {SOURCE_CONDITION} }}}}\n",
                "      - name: All discovered feature source guards\n",
                1,
            ),
            "aggregate source guard is not gated",
        ),
        (
            "core preflight dependency",
            _remove_once(text, "    needs: preflight\n"),
            "core job no longer depends on preflight",
        ),
    ]

    failures = []
    for label, mutated, expected in cases:
        found = inspect(mutated)
        if not any(expected in error for error in found):
            failures.append(label + " mutation was not rejected")
    return failures


def main():
    if not WORKFLOW.is_file():
        print("Hourly-control CI enforcement preflight FAILED:")
        print(" - missing .github/workflows/ci.yml")
        return 1

    text = WORKFLOW.read_text(encoding="utf-8")
    errors = inspect(text)
    errors.extend("guard self-test failed: " + error for error in self_test(text))

    if errors:
        print("Hourly-control CI enforcement preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: hourly-control policy edits trigger branch CI; source validation runs generic then aggregate "
        "preflights; core remains dependent on preflight; negative guard self-tests passed."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
