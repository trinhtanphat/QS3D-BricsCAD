#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

safety_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ModelTreeVirtualizationSafety.cs"
browser_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs"
augmenter_path = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs"
theme_path = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"

for path in (safety_path, browser_path, augmenter_path, theme_path):
    if not path.is_file():
        errors.append("missing Workspace virtualization contract file: " + str(path.relative_to(ROOT)))

if not errors:
    safety = safety_path.read_text(encoding="utf-8")
    browser = browser_path.read_text(encoding="utf-8")
    augmenter = augmenter_path.read_text(encoding="utf-8")
    theme = theme_path.read_text(encoding="utf-8")

    required_safety = {
        "protected override void OnInitialized(EventArgs e)": "ModelTree safety must be applied before the first Workspace measure/layout pass",
        "ApplyModelTreeVirtualizationSafety(this);": "ModelTree safety OnInitialized application missing",
        "FrameworkElement.LoadedEvent": "ModelTree safety must retain a Loaded fallback for reparenting/reload paths",
        "new RoutedEventHandler(OnModelTreeVirtualizationSafetyLoaded)": "ModelTree safety Loaded fallback registration missing",
        "VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);": "ModelTree must opt out of recycling virtualization locally",
        "ScrollViewer.SetCanContentScroll(panel.ModelTree, false);": "ModelTree must use physical scrolling so a virtualizing items host is not selected",
    }
    for token, message in required_safety.items():
        if token not in safety:
            errors.append(message)

    browser_tokens = (
        "modelDock.Children.Remove(ModelTree);",
        'tabs.Items.Add(new TabItem { Header = "Mô hình", Content = ModelTree });',
    )
    if any(token not in browser for token in browser_tokens):
        errors.append("Project Browser must still use the canonical ModelTree reparenting path guarded by the local virtualization containment")

    if not re.search(r"\btree\.Items\.Remove\(", augmenter) or not re.search(r"\.Items\.Insert\(", augmenter):
        errors.append("reference-tree registry must retain explicit container reordering evidence covered by the containment")

    # The fix is deliberately narrow. Do not regress the production virtualization policy for
    # normal data-heavy controls while protecting the small explicit navigation tree.
    for control in ("TreeView", "ListView", "ListBox"):
        style_match = re.search(
            r'<Style\s+TargetType="\{x:Type\s+' + re.escape(control) + r'\}"[^>]*>(.*?)</Style>',
            theme,
            flags=re.DOTALL,
        )
        if not style_match:
            errors.append("Theme.xaml missing implicit " + control + " style")
            continue
        style = style_match.group(1)
        if 'Property="VirtualizingPanel.IsVirtualizing" Value="True"' not in style:
            errors.append("Theme.xaml must keep " + control + " virtualization enabled globally")
        if 'Property="VirtualizingPanel.VirtualizationMode" Value="Recycling"' not in style:
            errors.append("Theme.xaml must keep " + control + " recycling virtualization for data-heavy surfaces")

if errors:
    print("V25 Workspace ModelTree virtualization containment guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("V25 Workspace ModelTree virtualization containment guard passed")
