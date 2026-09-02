#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"


def main() -> int:
    errors: list[str] = []
    if not SOURCE.is_file():
        print(f"ERROR: missing {SOURCE.relative_to(ROOT)}")
        return 1

    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private static void FinalizeUi(")
    end = text.find("private static void Report(", start + 1)
    finalize = text[start:end] if start >= 0 and end > start else ""
    if not finalize:
        errors.append("unable to isolate CurtainWallBuildCommands.FinalizeUi")
    else:
        for required in (
            "PaletteCoordinator.RefreshProject();",
            "PaletteCoordinator.SetStatus(status);",
            'document.Editor.WriteMessage("\\nQS3D " + status);',
        ):
            if required not in finalize:
                errors.append("FinalizeUi lost required non-viewport status synchronization: " + required)

        for forbidden in (
            "document.Editor.Regen();",
            'document.SendStringToExecute("QS3DVIEW3D ", true, false, false);',
        ):
            if forbidden in finalize:
                errors.append("normal post-commit success must not force a second viewport refresh: " + forbidden)

    rollback_start = text.find("if (!nativeCommitted && rollback != null && project != null)")
    rollback_end = text.find("ReportAtomicFailure(document, phase, nativeCommitted, ex);", rollback_start + 1)
    rollback = text[rollback_start:rollback_end] if rollback_start >= 0 and rollback_end > rollback_start else ""
    if "TryRegen(document);" not in rollback:
        errors.append("rollback recovery must retain its best-effort regen")

    helper_start = text.find("private static void TryRegen(Document document)")
    if helper_start < 0 or "document.Editor.Regen();" not in text[helper_start:]:
        errors.append("rollback-only TryRegen helper must remain available")

    if errors:
        print("FAIL: Curtain 3D post-commit idle-refresh guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: Curtain 3D normal post-commit UI sync avoids forced Regen/QS3DVIEW3D while rollback keeps best-effort regen.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
