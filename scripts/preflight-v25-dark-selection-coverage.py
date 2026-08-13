#!/usr/bin/env python3
"""Require host-independent dark selection coverage for every V25 XAML collection surface."""

from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
SELECTION_CONTROL_RE = re.compile(r"<(?:TreeView|ListBox|ListView|DataGrid)\b")
CLASS_RE = re.compile(r'x:Class="([^"]+)"')
REQUIRED_GUARD_TOKENS = (
    "SystemColors.HighlightBrushKey",
    "SystemColors.InactiveSelectionHighlightBrushKey",
    "SystemColors.HighlightTextBrushKey",
    "SystemColors.InactiveSelectionHighlightTextBrushKey",
)


def fail(message: str) -> None:
    print(f"FAIL: {message}")


def main() -> None:
    if not UI.is_dir():
        raise SystemExit(f"FAIL: missing V25 UI directory: {UI.relative_to(ROOT)}")

    errors: list[str] = []
    covered: list[str] = []

    for xaml in sorted(UI.glob("*.xaml")):
        if xaml.name == "Theme.xaml":
            continue

        text = xaml.read_text(encoding="utf-8")
        if not SELECTION_CONTROL_RE.search(text):
            continue

        match = CLASS_RE.search(text)
        if not match:
            errors.append(f"{xaml.name}: selection controls present but x:Class is missing")
            continue

        class_name = match.group(1).rsplit(".", 1)[-1]
        guard = UI / f"{class_name}.DarkHostTheme.cs"
        if not guard.is_file():
            errors.append(f"{xaml.name}: missing companion {guard.name}")
            continue

        if "Theme.xaml" not in text:
            errors.append(f"{xaml.name}: selection surface does not merge Theme.xaml")

        guard_text = guard.read_text(encoding="utf-8")
        for token in REQUIRED_GUARD_TOKENS:
            if token not in guard_text:
                errors.append(f"{guard.name}: missing {token}")

        if 'TryFindResource("BgSelectedBrush")' not in guard_text:
            errors.append(f"{guard.name}: missing QS3D selected-background lookup")
        if 'TryFindResource("TextBrush")' not in guard_text:
            errors.append(f"{guard.name}: missing QS3D selected-text lookup")
        if not re.search(r"\bResources\s*\[[^\]]+\]\s*=", guard_text):
            errors.append(f"{guard.name}: missing root resource-boundary pin")

        covered.append(xaml.name)

    if not covered:
        errors.append("no V25 XAML selection surfaces were discovered")

    if errors:
        print("V25 dark-selection coverage preflight FAILED:")
        for error in errors:
            fail(error)
        sys.exit(1)

    print(
        "PASS: V25 dark-selection coverage gate — "
        f"{len(covered)} XAML collection surface file(s) have local host-selection guards: "
        + ", ".join(covered)
    )


if __name__ == "__main__":
    main()
