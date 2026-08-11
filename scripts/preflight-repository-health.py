#!/usr/bin/env python3
import ast
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GENERIC = ROOT / "scripts/preflight.py"
AGGREGATE = ROOT / "scripts/preflight-all.py"
errors = []


def require(condition, message):
    if not condition:
        errors.append(message)


for path in (GENERIC, AGGREGATE):
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

if errors:
    print("Repository-health preflight regression FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("Repository-health preflight regression passed.")
