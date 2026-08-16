#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawSlabOpeningCommands.cs"
SELECTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "CadSelectionGuard.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: expected source contract not found: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"{label}: stale source contract is still present: {token}")


def main() -> int:
    if not COMMAND.is_file():
        fail("missing DirectDrawSlabOpeningCommands.cs")
    if not SELECTION.is_file():
        fail("missing CadSelectionGuard.cs")

    command = COMMAND.read_text(encoding="utf-8")
    selection = SELECTION.read_text(encoding="utf-8")

    # The VẼ ribbon must be usable click-first as well as PICKFIRST. A user who activates
    # Cắt sàn without a preselection should receive BricsCAD's normal selection prompt, and
    # cancelling that prompt should cancel the command quietly rather than report an error.
    require(
        command,
        "var selectedHostIds = CadSelectionGuard.AcquireCurrentSelection(document);",
        "slabOpen interactive host acquisition",
    )
    require(command, "if (selectedHostIds.Length == 0) return;", "slabOpen selection cancellation")
    require(command, "if (selectedHostIds.Length != 1)", "slabOpen single-host invariant")
    forbid(
        command,
        "var selectedHostIds = CadSelectionGuard.ReadImpliedSelection(document);",
        "slabOpen must not require PICKFIRST-only host acquisition",
    )

    require(selection, "var objectIds = ReadImpliedSelection(document);", "PICKFIRST preference")
    require(selection, "var selection = editor.GetSelection();", "click-first selection prompt")
    require(selection, "editor.SetImpliedSelection(objectIds);", "interactive selection preservation")

    print(
        "PASS: VẼ Cắt sàn accepts PICKFIRST or interactive host selection, preserves the chosen "
        "host for downstream slab building, and treats selection cancellation as a clean cancel."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
