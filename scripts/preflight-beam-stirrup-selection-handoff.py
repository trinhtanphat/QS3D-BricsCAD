#!/usr/bin/env python3
"""Fail closed when Beam Stirrup re-reads native selection after command admission."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise AssertionError(f"forbidden {label}: {token}")


def main() -> int:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    require(
        command,
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "single command-level admitted selection",
    )
    require(
        command,
        "BeamStirrupSolidBuilder.BuildSelected(document, project, selectedIds)",
        "exact admitted ObjectId handoff",
    )
    require(
        builder,
        "BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)",
        "builder selection-snapshot parameter",
    )
    require(
        builder,
        "if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));",
        "null snapshot refusal",
    )
    require(
        builder,
        "foreach (var id in selectedIds)",
        "builder handle derivation from admitted snapshot",
    )

    forbid(builder, "document.Editor.SelectImplied()", "selection re-read inside builder")
    forbid(builder, "document.Editor.GetSelection()", "selection prompt inside builder")
    forbid(builder, "document.Editor.SetImpliedSelection(", "selection mutation inside builder")

    print("PASS: Beam Stirrup consumes only the command-admitted selection snapshot")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: beam stirrup selection handoff preflight failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
