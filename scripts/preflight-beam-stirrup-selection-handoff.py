#!/usr/bin/env python3
"""Fail closed when Beam Stirrup escapes its admitted selection/target generation."""

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
        "BeamStirrupSolidBuilder.BuildSelected(document, project, selectedIds, expectedTargetIds)",
        "exact admitted ObjectId and target-set handoff",
    )
    require(builder, "ObjectId[] selectedIds", "builder selection-snapshot parameter")
    require(builder, "ISet<string> expectedTargetIds", "builder target-generation parameter")
    require(
        builder,
        "if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));",
        "null selection snapshot refusal",
    )
    require(
        builder,
        "if (expectedTargetIds == null) throw new ArgumentNullException(nameof(expectedTargetIds));",
        "null target-set refusal",
    )
    require(
        builder,
        "foreach (var id in selectedIds)",
        "builder handle derivation from admitted snapshot",
    )
    require(
        builder,
        "if (!expectedTargetIds.SetEquals(elements.Select(x => x.Id)))",
        "pre-mutation semantic target-set fence",
    )
    require(
        builder,
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "exact active-document fence",
    )
    require(
        builder,
        "using (document.LockDocument())",
        "native document-lock boundary",
    )
    require(
        builder,
        "DWG active đã thay đổi sau document lock",
        "post-lock document affinity fence",
    )

    forbid(builder, "document.Editor.SelectImplied()", "selection re-read inside builder")
    forbid(builder, "document.Editor.GetSelection()", "selection prompt inside builder")
    forbid(builder, "document.Editor.SetImpliedSelection(", "selection mutation inside builder")

    print("PASS: Beam Stirrup consumes only the command-admitted selection and target generation")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: beam stirrup selection handoff preflight failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
