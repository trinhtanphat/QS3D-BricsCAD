#!/usr/bin/env python3
"""Deterministic inventory of QS3D repository verification assets."""

from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path
import re
import sys


class InventoryError(RuntimeError):
    """Raised when the inventory cannot be derived unambiguously."""


def _read_required(path: Path) -> str:
    if not path.is_file():
        raise InventoryError(f"missing required inventory source: {path.as_posix()}")
    try:
        return path.read_text(encoding="utf-8")
    except OSError as exc:
        raise InventoryError(f"cannot read inventory source {path.as_posix()}: {exc}") from exc


def _mask_csharp(text: str) -> str:
    """Mask comments and literals while preserving line/character positions."""
    out = list(text)
    i = 0
    n = len(text)

    def blank(index: int) -> None:
        if out[index] not in ("\n", "\r"):
            out[index] = " "

    while i < n:
        if text.startswith("//", i):
            blank(i)
            if i + 1 < n:
                blank(i + 1)
            i += 2
            while i < n and text[i] not in "\r\n":
                blank(i)
                i += 1
            continue

        if text.startswith("/*", i):
            blank(i)
            if i + 1 < n:
                blank(i + 1)
            i += 2
            while i < n:
                if text.startswith("*/", i):
                    blank(i)
                    if i + 1 < n:
                        blank(i + 1)
                    i += 2
                    break
                blank(i)
                i += 1
            continue

        if text[i] == '"':
            verbatim = "@" in text[max(0, i - 2):i]
            blank(i)
            i += 1
            while i < n:
                if verbatim:
                    if text[i] == '"':
                        blank(i)
                        if i + 1 < n and text[i + 1] == '"':
                            blank(i + 1)
                            i += 2
                            continue
                        i += 1
                        break
                    blank(i)
                    i += 1
                    continue

                if text[i] == "\\":
                    blank(i)
                    if i + 1 < n:
                        blank(i + 1)
                    i += 2
                    continue
                if text[i] == '"':
                    blank(i)
                    i += 1
                    break
                blank(i)
                i += 1
            continue

        if text[i] == "'":
            blank(i)
            i += 1
            while i < n:
                if text[i] == "\\":
                    blank(i)
                    if i + 1 < n:
                        blank(i + 1)
                    i += 2
                    continue
                if text[i] == "'":
                    blank(i)
                    i += 1
                    break
                blank(i)
                i += 1
            continue

        i += 1

    return "".join(out)


def _method_body(masked: str, method_name: str) -> str | None:
    match = re.search(r"\b" + re.escape(method_name) + r"\s*\([^)]*\)\s*\{", masked)
    if match is None:
        return None
    open_index = masked.find("{", match.start(), match.end())
    if open_index < 0:
        return None
    depth = 0
    for index in range(open_index, len(masked)):
        char = masked[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return masked[open_index + 1:index]
    raise InventoryError(f"unterminated {method_name} method body")


def _count_unqualified_calls(masked: str, name: str) -> int:
    scope = _method_body(masked, "Main")
    if scope is not None:
        return len(re.findall(r"(?<![\w.])" + re.escape(name) + r"\s*\(", scope))

    calls = len(re.findall(r"(?<![\w.])" + re.escape(name) + r"\s*\(", masked))
    declarations = len(
        re.findall(
            r"\b(?:void|int|long|bool|string|object|Task|ValueTask)\s+"
            + re.escape(name)
            + r"\s*\(",
            masked,
        )
    )
    return max(0, calls - declarations)


def _core_count(root: Path) -> int:
    directory = root / "tests" / "QS3D.Core.SmokeTests"
    registration = _mask_csharp(_read_required(directory / "SmokeTestRegistration.cs"))
    registered = re.findall(
        r"(?<![\w.])([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*Run\s*\(\s*\)\s*;",
        registration,
    )
    duplicates = sorted(name for name, count in Counter(registered).items() if count > 1)
    if duplicates:
        raise InventoryError("duplicate Core smoke registration: " + ", ".join(duplicates))
    if not registered:
        raise InventoryError("no Core smoke registrations discovered")

    program = _mask_csharp(_read_required(directory / "Program.cs"))
    direct = _count_unqualified_calls(program, "Test")
    if direct <= 0:
        raise InventoryError("no direct Core smoke scenarios discovered")
    return len(registered) + direct


def _agent_count(root: Path) -> int:
    program = _mask_csharp(
        _read_required(root / "tests" / "QS3D.AgentHarness.Core.SmokeTests" / "Program.cs")
    )
    scenarios = set(
        re.findall(
            r"(?<![\w.])Run\s*\(\s*nameof\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)",
            program,
        )
    )
    main = _method_body(program, "Main")
    if main is not None:
        scenarios.update(
            re.findall(
                r"(?m)^\s*([A-Z][A-Za-z0-9_]*)\s*\(\s*\)\s*;\s*$",
                main,
            )
        )
    if not scenarios:
        raise InventoryError("no Agent Harness smoke scenarios discovered")
    return len(scenarios)


def _cli_count(root: Path) -> int:
    program = _mask_csharp(
        _read_required(root / "tests" / "QS3D.Code.Cli.SmokeTests" / "Program.cs")
    )
    count = _count_unqualified_calls(program, "Run")
    if count <= 0:
        raise InventoryError("no Code.Cli smoke scenarios discovered")
    return count


def _preflight_count(root: Path) -> int:
    scripts = root / "scripts"
    if not scripts.is_dir():
        raise InventoryError("missing scripts directory")
    return sum(
        1
        for path in scripts.glob("preflight-*.py")
        if path.is_file() and path.name != "preflight-all.py"
    )


def _project_count(root: Path) -> int:
    tests = root / "tests"
    if not tests.is_dir():
        raise InventoryError("missing tests directory")
    return sum(
        1
        for directory in tests.iterdir()
        if directory.is_dir() and any(path.is_file() for path in directory.glob("*.csproj"))
    )


def _workflow_count(root: Path) -> int:
    workflows = root / ".github" / "workflows"
    if not workflows.is_dir():
        raise InventoryError("missing .github/workflows directory")
    return sum(
        1
        for path in workflows.iterdir()
        if path.is_file() and path.suffix.lower() in {".yml", ".yaml"}
    )


def collect_inventory(root: Path) -> dict:
    root = Path(root)
    suites = {
        "QS3D.Core.SmokeTests": _core_count(root),
        "QS3D.AgentHarness.Core.SmokeTests": _agent_count(root),
        "QS3D.Code.Cli.SmokeTests": _cli_count(root),
    }
    return {
        "automated_smoke_regression_scenarios": sum(suites.values()),
        "smoke_suites": suites,
        "preflight_feature_gates": _preflight_count(root),
        "test_harness_projects": _project_count(root),
        "github_actions_workflows": _workflow_count(root),
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", action="store_true", help="emit compact deterministic JSON")
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help=argparse.SUPPRESS,
    )
    args = parser.parse_args(argv)
    try:
        inventory = collect_inventory(args.root)
    except (InventoryError, OSError, ValueError) as exc:
        print("ERROR:", exc, file=sys.stderr)
        return 1

    if args.json:
        print(json.dumps(inventory, sort_keys=True, separators=(",", ":")))
    else:
        print("QS3D verification inventory")
        for name, count in inventory["smoke_suites"].items():
            print(f"  {name}: {count}")
        print("  automated smoke/regression scenarios:", inventory["automated_smoke_regression_scenarios"])
        print("  preflight feature gates:", inventory["preflight_feature_gates"])
        print("  test/harness projects:", inventory["test_harness_projects"])
        print("  GitHub Actions workflows:", inventory["github_actions_workflows"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
