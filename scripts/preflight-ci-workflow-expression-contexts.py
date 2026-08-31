#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
RUNNER_EXPRESSION = re.compile(r"\$\{\{[^}\n]*\brunner\.", re.IGNORECASE)
errors = []


def strip_yaml_comment(line):
    quote = None
    index = 0
    while index < len(line):
        char = line[index]
        if quote == "'":
            if char == "'":
                if index + 1 < len(line) and line[index + 1] == "'":
                    index += 2
                    continue
                quote = None
            index += 1
            continue
        if quote == '"':
            if char == "\\" and index + 1 < len(line):
                index += 2
                continue
            if char == '"':
                quote = None
            index += 1
            continue
        if char in ("'", '"'):
            quote = char
            index += 1
            continue
        if char == "#":
            return line[:index]
        index += 1
    return line


def indentation(line):
    return len(line) - len(line.lstrip(" "))


def job_level_env_ranges(lines):
    jobs_index = next(
        (index for index, line in enumerate(lines) if re.fullmatch(r"jobs\s*:\s*(?:#.*)?", line)),
        None,
    )
    if jobs_index is None:
        return []

    ranges = []
    index = jobs_index + 1
    while index < len(lines):
        line = lines[index]
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        if re.match(r"^\s{4}env\s*:", line):
            start = index
            end = index + 1
            while end < len(lines):
                candidate = lines[end]
                if candidate.strip() and indentation(candidate) <= 4:
                    break
                end += 1
            ranges.append((start, end))
            index = end
            continue
        index += 1
    return ranges


if not WORKFLOWS.is_dir():
    raise SystemExit("CI workflow expression-context preflight failed: missing .github/workflows")

for path in sorted(WORKFLOWS.glob("*.y*ml"), key=lambda item: item.name.casefold()):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    for start, end in job_level_env_ranges(lines):
        for line_number in range(start, end):
            code = strip_yaml_comment(lines[line_number])
            if RUNNER_EXPRESSION.search(code):
                errors.append(
                    f"{path.name}:{line_number + 1}: runner context is unavailable in jobs.<job_id>.env; "
                    "derive runner-specific paths in a step or use runner default environment variables"
                )

if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit("CI workflow expression-context preflight failed with unsupported context usage")

print("PASS CI workflow expression-context availability guard")
