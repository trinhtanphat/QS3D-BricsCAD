#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "PropertiesPanel.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "PropertiesPanel.xaml.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


xaml = read(XAML)
code = read(CODE)

# #2399 requires a real editable QS3D Properties surface, not a read-only duplicate inspector.
# Pin each production editor to the same write-back semantics used by the WorkspaceViewModel.
for token in (
    'Text="{Binding Value, Mode=TwoWay, UpdateSourceTrigger=LostFocus}"',
    'IsChecked="{Binding BooleanValue, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'Text="{Binding Value, Mode=TwoWay, UpdateSourceTrigger=LostFocus}"',
    'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'IsReadOnly="{Binding IsReadOnly}"',
    'IsEnabled="{Binding IsEditable}"',
    'IsEnabled="{Binding CanReset}" Click="OnResetClick"',
):
    if token not in xaml:
        errors.append("editable QS3D Properties XAML contract missing: " + token)

# The dedicated PaletteSet must observe the authoritative Workspace DataContext. Creating a second
# WorkspaceViewModel here would split selection/edit state and make the new Properties UI diverge.
for token in (
    "public void Attach(WorkspacePanel workspace)",
    "DataContext = workspace.DataContext;",
    "workspace.DataContextChanged += OnWorkspaceDataContextChanged;",
    "workspace.DataContextChanged -= OnWorkspaceDataContextChanged;",
    "DataContext = e.NewValue;",
    "row.ResetValue();",
):
    if token not in code:
        errors.append("shared WorkspaceViewModel contract missing: " + token)

if "new WorkspaceViewModel" in code:
    errors.append("dedicated QS3D Properties must not construct a second WorkspaceViewModel")

print("QS3D dedicated Properties editability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: dedicated QS3D Properties keeps text/boolean/choice/scope write-back and reset behavior on the authoritative shared WorkspaceViewModel.")
