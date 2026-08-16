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
QUANTITY_PANEL_ASSERTION = "if (PaletteCoordinator.IsQuantityInsightVisible)"
STALE_RIGHT_PANEL_ASSERTION = "if (!PaletteCoordinator.IsRightPanelVisible)"
STALE_QUANTITY_PANEL_ASSERTION = "if (!PaletteCoordinator.IsQuantityInsightVisible)"
WORKSPACE_MARKER = '"workspace_palette_visible=true"'
RIGHT_PANEL_MARKER = '"right_palette_visible=false"'
QUANTITY_PANEL_MARKER = '"quantity_palette_visible=false"'
WORKSPACE_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "workspace_palette_visible" -Expected "true"'
RIGHT_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "right_palette_visible" -Expected "false"'
QUANTITY_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "quantity_palette_visible" -Expected "false"'


def main():
    errors = []
    probe_text = PROBE.read_text(encoding="utf-8")

    if WORKSPACE_ASSERTION not in probe_text:
        errors.append("runtime probe no longer requires the Workspace palette to be visible")
    if RIGHT_PANEL_ASSERTION not in probe_text:
        errors.append("runtime probe must fail when the legacy right-side palette is visible")
    if QUANTITY_PANEL_ASSERTION not in probe_text:
        errors.append("runtime probe must fail when the quantity insight palette is visible")
    if STALE_RIGHT_PANEL_ASSERTION in probe_text:
        errors.append("runtime probe still requires the legacy right-side palette to be visible")
    if STALE_QUANTITY_PANEL_ASSERTION in probe_text:
        errors.append("runtime probe incorrectly requires the quantity insight palette to be visible")
    if WORKSPACE_MARKER not in probe_text:
        errors.append("runtime probe must report workspace_palette_visible=true on success")
    if RIGHT_PANEL_MARKER not in probe_text:
        errors.append("runtime probe must report right_palette_visible=false on success")
    if QUANTITY_PANEL_MARKER not in probe_text:
        errors.append("runtime probe must report quantity_palette_visible=false on success")
    if '"workspace_palette_visible=false"' in probe_text:
        errors.append("runtime probe reports the Workspace palette as hidden")
    if '"right_palette_visible=true"' in probe_text:
        errors.append("runtime probe still reports the legacy right-side palette as visible")
    if '"quantity_palette_visible=true"' in probe_text:
        errors.append("runtime probe reports the quantity insight palette as visible")

    for runner in RUNNERS:
        runner_text = runner.read_text(encoding="utf-8")
        relative = runner.relative_to(ROOT)
        if WORKSPACE_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce workspace_palette_visible=true".format(relative))
        if RIGHT_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce right_palette_visible=false".format(relative))
        if QUANTITY_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce quantity_palette_visible=false".format(relative))

    if errors:
        for error in errors:
            print("[FAIL] {}".format(error))
        return 1

    print("[OK] runtime probe contract: Workspace visible, Right Panel hidden, Quantity Insight hidden")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
