#!/usr/bin/env python3
import ast
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
CHECKER = ROOT / "scripts" / "preflight-ci-manual-only.py"


def load_parser_functions():
    tree = ast.parse(CHECKER.read_text(encoding="utf-8"), filename=str(CHECKER))
    wanted = {"parse_block_mapping_key", "collect_job_blocks"}
    definitions = [
        node for node in tree.body
        if isinstance(node, ast.FunctionDef) and node.name in wanted
    ]
    found = {node.name for node in definitions}
    if found != wanted:
        raise AssertionError(f"missing parser functions: {sorted(wanted - found)}")
    module = ast.Module(body=definitions, type_ignores=[])
    ast.fix_missing_locations(module)
    namespace = {"re": re}
    exec(compile(module, str(CHECKER), "exec"), namespace)
    return namespace["parse_block_mapping_key"], namespace["collect_job_blocks"]


def assert_equal(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected {expected!r}, got {actual!r}")


def main():
    parse_key, collect_jobs = load_parser_functions()

    accepted = (
        ("on:", 0, "on"),
        ("'on':", 0, "on"),
        ('"on":', 0, "on"),
        ('"on": # trigger block', 0, "on"),
        ("  preflight:", 2, "preflight"),
        ("  'core':", 2, "core"),
        ('  "release-job": # publish job', 2, "release-job"),
        ("  job_2:", 2, "job_2"),
    )
    for line, indentation, expected in accepted:
        assert_equal(parse_key(line, indentation), expected, f"accepted {line!r}")

    rejected = (
        (" on:", 0),
        ("\ton:", 0),
        ("'on\":", 0),
        ('"on\':', 0),
        ("on: [push]", 0),
        (" preflight:", 2),
        ("   preflight:", 2),
        ("\t\tpreflight:", 2),
        ("  'bad.name':", 2),
        ("  'bad name':", 2),
        ("  'unterminated:", 2),
        ('  "unterminated:', 2),
    )
    for line, indentation in rejected:
        assert_equal(parse_key(line, indentation), None, f"rejected {line!r}")

    blocks = collect_jobs([
        "name: parser fixture",
        "jobs:",
        "  preflight:",
        "    if: ${{ github.event_name == 'workflow_dispatch' }}",
        "  'core': # quoted job",
        "    needs: preflight",
        '  "release-job":',
        "    runs-on: windows-latest",
    ])
    assert_equal(
        [name for name, _ in blocks],
        ["preflight", "core", "release-job"],
        "mixed quoted job IDs",
    )

    malformed_blocks = collect_jobs([
        "jobs:",
        "  valid:",
        "    runs-on: ubuntu-latest",
        "   wrong_indent:",
        "  'bad.name':",
        "  'unterminated:",
    ])
    assert_equal([name for name, _ in malformed_blocks], ["valid"], "malformed job IDs fail closed")

    print("PASS: quoted CI policy mapping keys are accepted semantically and malformed/incorrectly indented keys fail closed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
