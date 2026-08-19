#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ScopeDropdownHostInteraction.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing Workspace scope dropdown host interaction source")
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required_fragments = {
    "Zone selector wiring": "WireWorkspaceScopeCombo(ZoneCombo);",
    "Floor selector wiring": "WireWorkspaceScopeCombo(FloorCombo);",
    "preview mouse host fallback": "PreviewMouseLeftButtonDown += OnWorkspaceScopeComboPreviewMouseLeftButtonDown;",
    "closed-popup guard": "combo.IsDropDownOpen",
    "populated-items guard": "combo.HasItems",
    "explicit popup opening": "combo.IsDropDownOpen = true;",
    "single-press handling": "e.Handled = true;",
}

for label, fragment in required_fragments.items():
    if fragment not in text:
        errors.append(label + " contract is missing: " + fragment)

if "WireWorkspaceScopeCombo(Family" in text or "FindVisualChildren<ComboBox>" in text:
    errors.append("host fallback must remain scoped to ZoneCombo and FloorCombo only")

print("QS3D Workspace scope dropdown host-interaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: ZoneCombo and FloorCombo retain a host-safe first-click dropdown fallback "
    "without broadening the behavior to unrelated ComboBoxes."
)
