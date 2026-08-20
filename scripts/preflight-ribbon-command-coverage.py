#!/usr/bin/env python3
"""Fail CI when a QS3D Ribbon command has no CommandMethod registration.

This guard intentionally validates source wiring rather than pretending to execute a
licensed BricsCAD host in GitHub Actions. It scans every C# file whose path contains
``Ribbon`` for QS3D command-like string literals used as bindings, then checks that
each one is backed by a ``[CommandMethod(...)]`` registration somewhere under ``src``.
Classifier-only literals (for example ``command.IndexOf(\"QS3D...\")``) are not bindings
and therefore must not be promoted to executable commands by this source guard.
"""

from __future__ import annotations

import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"

# Ribbon element ids in this repository generally contain underscores (for example
# QS3D_BLT_...), while BricsCAD command names are compact upper-case tokens. Keeping
# the grammar narrow prevents UI ids from being misclassified as executable commands.
QS3D_COMMAND_RE = re.compile(r"^QS3D[A-Z0-9]+$")
STRING_RE = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"', re.DOTALL)
COMMAND_METHOD_RE = re.compile(
    r"\[\s*(?:[A-Za-z_][A-Za-z0-9_]*\.)*CommandMethod(?:Attribute)?\s*\((.*?)\)\s*\]",
    re.DOTALL,
)

SCREENSHOT_COMMANDS = {
    "QS3DREVBASE",
    "QS3DREVDIFF",
    "QS3DHEALTHALL",
    "QS3DRELEASECHECK",
    "QS3DSAVE",
}


def _decode_csharp_string(token: str) -> str:
    if token.startswith('@"'):
        return token[2:-1].replace('""', '"')

    body = token[1:-1]
    # Command names are ASCII identifiers, so only the escapes that can affect the
    # literal delimiters themselves need to be decoded here.
    return body.replace(r"\\", "\\").replace(r'\"', '"')


def _string_literals(text: str):
    for match in STRING_RE.finditer(text):
        yield match.start(), _decode_csharp_string(match.group(0))


def _is_source_file(path: Path) -> bool:
    lowered = {part.lower() for part in path.parts}
    return path.suffix == ".cs" and "bin" not in lowered and "obj" not in lowered


def _line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def _is_classifier_literal(text: str, offset: int) -> bool:
    """Return True for command-name strings used only to classify an existing binding.

    RibbonBootstrapIconAugmenter and similar source can inspect CommandParameter values
    with ``IndexOf`` to choose a semantic icon. Those strings do not create or dispatch
    commands, so requiring a second CommandMethod registration for them is a false
    positive. Restrict the exclusion to the source line containing the literal; actual
    ButtonSpec/CommandParameter literals continue through the normal coverage check.
    """

    line_start = text.rfind("\n", 0, offset) + 1
    line_end = text.find("\n", offset)
    if line_end < 0:
        line_end = len(text)
    line = text[line_start:line_end]
    return "IndexOf(" in line


def main() -> int:
    if not SRC.is_dir():
        print(f"ERROR: source directory not found: {SRC}", file=sys.stderr)
        return 2

    cs_files = sorted(path for path in SRC.rglob("*.cs") if _is_source_file(path))
    ribbon_files = [path for path in cs_files if "ribbon" in {p.lower() for p in path.parts}]

    if not ribbon_files:
        print("ERROR: no C# Ribbon source files were discovered", file=sys.stderr)
        return 2

    ribbon_refs: dict[str, list[str]] = defaultdict(list)
    registrations: dict[str, list[str]] = defaultdict(list)

    for path in ribbon_files:
        text = path.read_text(encoding="utf-8")
        rel = path.relative_to(ROOT).as_posix()
        for offset, value in _string_literals(text):
            if QS3D_COMMAND_RE.fullmatch(value) and not _is_classifier_literal(text, offset):
                ribbon_refs[value].append(f"{rel}:{_line_number(text, offset)}")

    for path in cs_files:
        text = path.read_text(encoding="utf-8")
        rel = path.relative_to(ROOT).as_posix()
        for attribute in COMMAND_METHOD_RE.finditer(text):
            args = attribute.group(1)
            for _, value in _string_literals(args):
                if QS3D_COMMAND_RE.fullmatch(value):
                    registrations[value].append(
                        f"{rel}:{_line_number(text, attribute.start())}"
                    )

    if not ribbon_refs:
        print("ERROR: no QS3D Ribbon command bindings were discovered", file=sys.stderr)
        return 2
    if not registrations:
        print("ERROR: no QS3D CommandMethod registrations were discovered", file=sys.stderr)
        return 2

    failures: list[str] = []

    missing_registrations = sorted(set(ribbon_refs) - set(registrations))
    if missing_registrations:
        failures.append(
            "Ribbon commands without a matching [CommandMethod] registration:"
        )
        for command in missing_registrations:
            failures.append(f"  - {command}: {', '.join(ribbon_refs[command])}")

    missing_screenshot_bindings = sorted(SCREENSHOT_COMMANDS - set(ribbon_refs))
    if missing_screenshot_bindings:
        failures.append(
            "Expected BẢN SỬA ĐỔI screenshot commands missing from Ribbon source: "
            + ", ".join(missing_screenshot_bindings)
        )

    missing_screenshot_registrations = sorted(SCREENSHOT_COMMANDS - set(registrations))
    if missing_screenshot_registrations:
        failures.append(
            "Expected BẢN SỬA ĐỔI screenshot commands missing from CommandMethod registrations: "
            + ", ".join(missing_screenshot_registrations)
        )

    if failures:
        print("Ribbon command coverage preflight: FAIL", file=sys.stderr)
        for failure in failures:
            print(failure, file=sys.stderr)
        print(
            f"Discovered {len(ribbon_refs)} Ribbon command(s) and "
            f"{len(registrations)} registered QS3D command(s).",
            file=sys.stderr,
        )
        return 1

    print(
        "Ribbon command coverage preflight: PASS — "
        f"{len(ribbon_refs)} Ribbon command(s) all have CommandMethod registrations; "
        "BẢN SỬA ĐỔI baseline/compare/health/release/save bindings are covered."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())