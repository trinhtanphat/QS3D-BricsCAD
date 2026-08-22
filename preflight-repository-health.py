#!/usr/bin/env python3
import ast
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "scripts"
GENERIC = SCRIPTS / "preflight.py"
AGENTS = ROOT / "AGENTS.md"
errors = []


def require(condition, message):
    if not condition:
        errors.append(message)


def mandatory_handoff_markdown_paths(source):
    header = "## Mandatory handoff reading order"
    start = source.find(header)
    if start < 0:
        errors.append("AGENTS.md is missing the mandatory handoff reading-order section")
        return []

    end = source.find("\n## ", start + len(header))
    section = source[start:] if end < 0 else source[start:end]
    paths = []
    seen = set()
    for raw in re.findall(r"`([^`\r\n]+\.md)`", section):
        normalized = raw.replace("\\", "/")
        if normalized not in seen:
            seen.add(normalized)
            paths.append(normalized)
    require(bool(paths), "AGENTS.md mandatory handoff reading order contains no Markdown paths")
    return paths


python_scripts = sorted(path for path in SCRIPTS.rglob("*.py") if path.is_file())
require(bool(python_scripts), "no Python scripts discovered under scripts/")

for path in python_scripts:
    try:
        source = path.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)}: {exc}")
        continue
    try:
        ast.parse(source, filename=str(path))
    except SyntaxError as exc:
        errors.append(
            f"{path.relative_to(ROOT)} syntax error at line {exc.lineno}: {exc.msg}"
        )

require(GENERIC.is_file(), "missing canonical generic preflight: scripts/preflight.py")
if GENERIC.is_file():
    source = GENERIC.read_text(encoding="utf-8")
    require(
        'private_extensions = {".dwg", ".dxf", ".docx"}' in source
        and "relative.suffix.casefold()" in source,
        "generic preflight must detect private CAD/document extensions case-insensitively",
    )
    require(
        'path.suffix.casefold() in {".yml", ".yaml"}' in source,
        "generic preflight must enforce manual-only policy for both .yml and .yaml workflows",
    )
    require(
        "'StartsWith(\"CAD.\")'" in source,
        "generic preflight semantic-capture token must remain syntactically closed",
    )

require(AGENTS.is_file(), "missing repository coordination policy: AGENTS.md")
if AGENTS.is_file():
    try:
        agents_source = AGENTS.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append(f"cannot read AGENTS.md: {exc}")
    else:
        for relative in mandatory_handoff_markdown_paths(agents_source):
            path = Path(relative)
            require(
                not path.is_absolute() and ".." not in path.parts,
                f"AGENTS.md mandatory handoff path must stay repository-relative: {relative}",
            )
            if path.is_absolute() or ".." in path.parts:
                continue
            require(
                (ROOT / path).is_file(),
                f"AGENTS.md mandatory handoff path does not exist: {relative}",
            )

if errors:
    print("Repository-health preflight regression FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "Repository-health preflight regression passed "
    f"({len(python_scripts)} Python scripts parsed; coordination paths verified)."
)
