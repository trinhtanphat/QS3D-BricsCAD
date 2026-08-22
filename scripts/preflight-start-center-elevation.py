#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SHELL_REL = "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"


def main():
    shell = (ROOT / SHELL_REL).read_text(encoding="utf-8")

    required = (
        "using System.Globalization;",
        '_elevationText.Text = "•  Cao độ 0.000 m";',
        'floor.ElevationM.ToString("0.000", CultureInfo.InvariantCulture)',
        '_elevationText.Text = "•  Cao độ " + floor.ElevationM.ToString("0.000", CultureInfo.InvariantCulture) + " m";',
    )
    for needle in required:
        if needle not in shell:
            raise SystemExit(f"FAIL: {SHELL_REL} missing Start Center elevation contract: {needle}")

    refresh = shell.split("private void RefreshHomeShell(bool recordActiveDrawing)", 1)[1]
    refresh = refresh.split("private void RefreshRecentProjects()", 1)[0]
    for forbidden in ("GetOrCreate(", "ProjectContextCoordinator.Save(", "SendStringToExecute", ".Touch("):
        if forbidden in refresh:
            raise SystemExit(
                f"FAIL: {SHELL_REL} RefreshHomeShell must remain display-only; found {forbidden}"
            )

    print(
        "PASS: Start Center keeps a 0.000 m fallback and renders the active FloorDefinition.ElevationM "
        "with invariant 3-decimal meter formatting without project mutation or command dispatch."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
