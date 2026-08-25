#!/usr/bin/env python3
"""Guard LOCAL-001 Interchange post-confirmation UI exception containment.

The licensed V25 LOCAL-001 closeout captured the same CLR-hosted process crash from
both append-only Interchange command surfaces after the user accepted the WPF
confirmation.  Core append rollback and the confirmation freshness guard are
already covered independently.  This gate keeps the post-mutation palette refresh
behind a dispatcher callback that catches its own exceptions, so a WPF refresh
cannot surface an unhandled managed exception into BricsCAD after the command's
outer try/catch has unwound.
"""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
APPEND = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeCommands.cs"
IMPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeImportCommands.cs"
HELPER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangePostMutationUi.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> int:
    append = APPEND.read_text(encoding="utf-8")
    imp = IMPORT.read_text(encoding="utf-8")
    if not HELPER.exists():
        fail("InterchangePostMutationUi.cs is missing")
    helper = HELPER.read_text(encoding="utf-8")

    required_helper_tokens = (
        "internal static class InterchangePostMutationUi",
        "Dispatcher.BeginInvoke",
        "PaletteCoordinator.RefreshProject();",
        "catch (Exception)",
        "ProjectContextCoordinator.TryGetReadOnly",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    )
    for token in required_helper_tokens:
        if token not in helper:
            fail(f"post-mutation UI helper is missing required containment token: {token}")

    required_call = "InterchangePostMutationUi.RefreshProjectFailClosed(document);"
    if required_call not in append:
        fail("QS3DINTERCHANGEAPPEND does not use fail-closed post-mutation UI refresh")
    if required_call not in imp:
        fail("QS3DINTERCHANGEIMPORT append-only path does not use fail-closed post-mutation UI refresh")

    forbidden = "try { PaletteCoordinator.RefreshProject(); } catch { }"
    if forbidden in append:
        fail("QS3DINTERCHANGEAPPEND still refreshes the WPF palette inline")
    if forbidden in imp:
        fail("QS3DINTERCHANGEIMPORT still contains inline post-mutation WPF refreshes")

    print("PASS interchange post-confirmation UI exception containment")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
