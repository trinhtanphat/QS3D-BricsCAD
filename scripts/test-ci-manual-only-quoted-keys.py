#!/usr/bin/env python3
import ast
from pathlib import Path
import re

CHECKER = Path(__file__).with_name("preflight-ci-manual-only.py")


def load_parser_functions():
    tree = ast.parse(CHECKER.read_text(encoding="utf-8"), filename=str(CHECKER))
    wanted_assignments = {"_MAPPING_KEY"}
    wanted_functions = {
        "parse_mapping_key",
        "parse_top_level_on_key",
        "parse_job_name",
        "collect_job_blocks",
    }
    selected = []
    for node in tree.body:
        if isinstance(node, ast.Assign):
            names = {target.id for target in node.targets if isinstance(target, ast.Name)}
            if names & wanted_assignments:
                selected.append(node)
        elif isinstance(node, ast.FunctionDef) and node.name in wanted_functions:
            selected.append(node)
    namespace = {"re": re}
    exec(compile(ast.Module(body=selected, type_ignores=[]), str(CHECKER), "exec"), namespace)
    return namespace


def require(condition, message):
    if not condition:
        raise AssertionError(message)


p = load_parser_functions()
parse_on = p["parse_top_level_on_key"]
parse_job = p["parse_job_name"]
collect_jobs = p["collect_job_blocks"]

for line in ("on:", "'on':", '"on":', "on: # comment", "'on': # comment", '"on": # comment'):
    require(parse_on(line), f"valid top-level on key rejected: {line!r}")

for line in (
    " on:",
    "  on:",
    "ON:",
    "'ON':",
    "'on' : value",
    '"on": value',
    "'on:",
    '"on:',
    "on::",
    "on: workflow_dispatch",
):
    require(not parse_on(line), f"malformed/broadened top-level on key accepted: {line!r}")

for line, expected in (
    ("  preflight:", "preflight"),
    ("  'preflight':", "preflight"),
    ('  "preflight":', "preflight"),
    ("  release: # comment", "release"),
    ("  'job-1_2': # comment", "job-1_2"),
):
    require(parse_job(line) == expected, f"valid job key rejected: {line!r}")

for line in (
    " preflight:",
    "    preflight:",
    "  'pre flight':",
    '  "preflight": value',
    "  preflight::",
    "  'preflight' : value",
    "  preflight.extra:",
):
    require(parse_job(line) is None, f"malformed/broadened job key accepted: {line!r}")

lines = [
    "name: test",
    "jobs:",
    "  'preflight':",
    "    if: ${{ github.event_name == 'workflow_dispatch' }}",
    '  "core": # comment',
    "    needs: preflight",
    "    if: ${{ github.event_name == 'workflow_dispatch' }}",
    "tail: value",
]
blocks = collect_jobs(lines)
require([name for name, _ in blocks] == ["preflight", "core"], f"quoted jobs parsed incorrectly: {blocks!r}")
require(any("needs: preflight" in line for line in blocks[1][1]), "core body was not retained")

malformed = [
    "jobs:",
    "    'preflight':",
    "      if: true",
]
require(collect_jobs(malformed) == [], "wrong-indentation job must not be accepted")

print("PASS: quoted/unquoted CI policy keys are parsed narrowly and deterministically.")
