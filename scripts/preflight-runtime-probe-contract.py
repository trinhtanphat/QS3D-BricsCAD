#!/usr/bin/env python3
"""Guard the runtime probe's Ribbon-first palette visibility contract."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeProbeCommands.cs"
RUNNERS = (
    ROOT / "scripts" / "test-bricscad-v25-runtime.ps1",
    ROOT / "scripts" / "test-bricscad-v26-runtime.ps1",
)

WORKSPACE_ASSERTION = "if (!PaletteCoordinator.IsWorkspaceVisible)"
RIGHT_PANEL_ASSERTION = "if (PaletteCoordinator.IsRightPanelVisible)"
STALE_RIGHT_PANEL_ASSERTION = "if (!PaletteCoordinator.IsRightPanelVisible)"
RIGHT_PANEL_MARKER = '"right_palette_visible=false"'
RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "right_palette_visible" -Expected "false"'


def main():
    errors = []
    probe_text = PROBE.read_text(encoding="utf-8")

    if WORKSPACE_ASSERTION not in probe_text:
        errors.append("runtime probe no longer requires the Workspace palette to be visible")
    if RIGHT_PANEL_ASSERTION not in probe_text:
        errors.append("runtime probe must fail when the legacy right-side palette is visible")
    if STALE_RIGHT_PANEL_ASSERTION in probe_text:
        errors.append("runtime probe still requires the legacy right-side palette to be visible")
    if RIGHT_PANEL_MARKER not in probe_text:
        errors.append("runtime probe must report right_palette_visible=false on success")
    if '"right_palette_visible=true"' in probe_text:
        errors.append("runtime probe still reports the legacy right-side palette as visible")

    for runner in RUNNERS:
        runner_text = runner.read_text(encoding="utf-8")
        if RUNNER_REQUIREMENT not in runner_text:
            errors.append(
                "{} does not enforce right_palette_visible=false".format(
                    runner.relative_to(ROOT)
                )
            )

    if errors:
        for error in errors:
            print("[FAIL] {}".format(error))
        return 1

    print("[OK] runtime probe contract: Workspace visible, Right Panel hidden")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
