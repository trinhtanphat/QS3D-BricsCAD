#!/usr/bin/env python3
"""Guard the runtime probe's BIM-workspace palette visibility contract."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeProbeCommands.cs"
V25_WRAPPER = ROOT / "scripts" / "test-bricscad-v25-runtime.ps1"
RUNNERS = (
    ROOT / "scripts" / "test-bricscad-v25-runtime-core.ps1",
    ROOT / "scripts" / "test-bricscad-v26-runtime.ps1",
)

WORKSPACE_ASSERTION = "if (!PaletteCoordinator.IsWorkspaceVisible)"
RIGHT_PANEL_ASSERTION = "if (!PaletteCoordinator.IsRightPanelVisible)"
QUANTITY_PANEL_ASSERTION = "if (PaletteCoordinator.IsQuantityInsightVisible)"
NATIVE_RUNTIME_ASSERTION = "if (!RuntimeDiagnosticsCommands.CurrentNativeRuntimeMatches())"
STALE_RIGHT_PANEL_ASSERTION = "if (PaletteCoordinator.IsRightPanelVisible)"
STALE_QUANTITY_PANEL_ASSERTION = "if (!PaletteCoordinator.IsQuantityInsightVisible)"
WORKSPACE_MARKER = '"workspace_palette_visible=true"'
RIGHT_PANEL_MARKER = '"right_palette_visible=true"'
QUANTITY_PANEL_MARKER = '"quantity_palette_visible=false"'
NATIVE_RUNTIME_MARKER = '"native_runtime_matches=true"'
NATIVE_BREP_MARKER = '"native_brep_identity=" + OneLine(RuntimeDiagnosticsCommands.CurrentNativeBrepIdentity())'
WORKSPACE_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "workspace_palette_visible" -Expected "true"'
RIGHT_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "right_palette_visible" -Expected "true"'
QUANTITY_RUNNER_REQUIREMENT = 'Require-Qs3dMarkerValue -Marker $marker -Key "quantity_palette_visible" -Expected "false"'


def main():
    errors = []
    probe_text = PROBE.read_text(encoding="utf-8")

    if WORKSPACE_ASSERTION not in probe_text:
        errors.append("runtime probe no longer requires the Workspace palette to be visible")
    if RIGHT_PANEL_ASSERTION not in probe_text:
        errors.append("runtime probe must require the BIM drawing/layer palette to be visible")
    if QUANTITY_PANEL_ASSERTION not in probe_text:
        errors.append("runtime probe must fail when the quantity insight palette is visible")
    if NATIVE_RUNTIME_ASSERTION not in probe_text:
        errors.append("runtime probe must fail when the compile-selected native dependency identity mismatches")
    if STALE_RIGHT_PANEL_ASSERTION in probe_text:
        errors.append("runtime probe still rejects the BIM drawing/layer palette when it is visible")
    if STALE_QUANTITY_PANEL_ASSERTION in probe_text:
        errors.append("runtime probe incorrectly requires the quantity insight palette to be visible")
    if WORKSPACE_MARKER not in probe_text:
        errors.append("runtime probe must report workspace_palette_visible=true on success")
    if RIGHT_PANEL_MARKER not in probe_text:
        errors.append("runtime probe must report right_palette_visible=true on success")
    if QUANTITY_PANEL_MARKER not in probe_text:
        errors.append("runtime probe must report quantity_palette_visible=false on success")
    if NATIVE_RUNTIME_MARKER not in probe_text:
        errors.append("runtime probe must report native_runtime_matches=true on success")
    if NATIVE_BREP_MARKER not in probe_text:
        errors.append("runtime probe must record the compile-selected BREP identity")
    if '"workspace_palette_visible=false"' in probe_text:
        errors.append("runtime probe reports the Workspace palette as hidden")
    if '"right_palette_visible=false"' in probe_text:
        errors.append("runtime probe reports the BIM drawing/layer palette as hidden")
    if '"quantity_palette_visible=true"' in probe_text:
        errors.append("runtime probe reports the quantity insight palette as visible")

    if not V25_WRAPPER.is_file():
        errors.append("V25 runtime profile-safety wrapper is missing")
    else:
        wrapper_text = V25_WRAPPER.read_text(encoding="utf-8")
        for needle in (
            "test-bricscad-v25-runtime-core.ps1",
            "New-Qs3dV25ProfileSandbox",
            "Restore-Qs3dV25ProfileSandbox",
            ". $coreScript @coreArgs",
        ):
            if needle not in wrapper_text:
                errors.append("V25 runtime wrapper missing split-contract token: {}".format(needle))

    expected_host_identity = {
        "test-bricscad-v25-runtime-core.ps1": ("25", "V25"),
        "test-bricscad-v26-runtime.ps1": ("26", "V26"),
    }
    for runner in RUNNERS:
        runner_text = runner.read_text(encoding="utf-8")
        relative = runner.relative_to(ROOT)
        if WORKSPACE_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce workspace_palette_visible=true".format(relative))
        if RIGHT_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce right_palette_visible=true".format(relative))
        if QUANTITY_RUNNER_REQUIREMENT not in runner_text:
            errors.append("{} does not enforce quantity_palette_visible=false".format(relative))
        major, label = expected_host_identity[runner.name]
        native_requirements = (
            'Require-Qs3dMarkerValue -Marker $marker -Key "native_runtime_major" -Expected "{}"'.format(major),
            'Require-Qs3dMarkerValue -Marker $marker -Key "native_runtime_label" -Expected "{}"'.format(label),
            'Require-Qs3dMarkerValue -Marker $marker -Key "native_runtime_matches" -Expected "true"',
        )
        for requirement in native_requirements:
            if requirement not in runner_text:
                errors.append("{} does not enforce {}".format(relative, requirement))

    if errors:
        for error in errors:
            print("[FAIL] {}".format(error))
        return 1

    print("[OK] runtime probe contract: V25 wrapper delegates to the profile-safe core; native identity matches; Workspace visible, BIM Right Panel visible, Quantity Insight hidden")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())