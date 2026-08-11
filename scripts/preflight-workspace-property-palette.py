#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
xaml = UI / "WorkspacePanel.xaml"
filter_code = UI / "WorkspacePanel.PropertyFiltering.cs"
errors = []

if not xaml.is_file():
    errors.append("missing WorkspacePanel.xaml")
else:
    try:
        ET.parse(xaml)
    except ET.ParseError as exc:
        errors.append("WorkspacePanel.xaml is not well-formed: " + str(exc))
    text = xaml.read_text(encoding="utf-8")
    for token in (
        'x:Name="PropertySearch"',
        'TextChanged="OnPropertySearchChanged"',
        'Click="OnClearPropertySearchClick"',
        'Text="Family kế thừa • Instance override • CAD khóa"',
        'Text="{Binding Properties.Count, StringFormat={}{0} dòng}"',
        'Text="{Binding Properties.Count, StringFormat={}{0} thuộc tính}"',
        'Value="Override"',
        'Value="CAD / đọc"',
        'x:Key="WorkspacePropertyRow"',
        'x:Key="WorkspaceSearchBand"',
        'MinWidth="220"',
    ):
        if token not in text:
            errors.append("Workspace upgraded property palette missing: " + token)

if not filter_code.is_file():
    errors.append("missing WorkspacePanel.PropertyFiltering.cs")
else:
    text = filter_code.read_text(encoding="utf-8")
    for token in (
        "private const int MaxPropertySearchTokens = 12;",
        "private void OnWorkspaceDataContextChanged",
        "PreviewKeyDown -= OnPropertyFilterShortcut;",
        "PreviewKeyDown += OnPropertyFilterShortcut;",
        "private void OnPropertyFilterShortcut",
        "modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F",
        "PropertySearch?.Focus();",
        "PropertySearch?.SelectAll();",
        "e.Key == Key.Escape",
        "PropertySearch.IsKeyboardFocusWithin",
        "PropertySearch.Clear();",
        "private void OnPropertySearchChanged",
        "private void OnClearPropertySearchClick",
        "private void ApplyPropertyFilter()",
        "CollectionViewSource.GetDefaultView(PropertyList?.ItemsSource)",
        ".Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)",
        ".Take(MaxPropertySearchTokens)",
        "tokens.All(token => MatchesPropertyToken(row, token))",
        "private static bool MatchesPropertyToken",
        "Contains(row.Group, token)",
        "Contains(row.Name, token)",
        "Contains(row.Unit, token)",
        "Contains(row.Value, token)",
        "Contains(row.EditorKind, token)",
        "row.Choices.Any(choice => Contains(choice, token))",
        'Contains("CAD đọc khóa readonly source nguồn", token)',
        'Contains("Instance override ghi đè", token)',
        "StringComparison.CurrentCultureIgnoreCase",
    ):
        if token not in text:
            errors.append("Workspace property filter/search shortcut missing: " + token)
    for forbidden in (
        "GetOrCreate(",
        "ExistingProjectMutationContext",
        "ProjectFamilyService",
        ".Touch(",
        "SetProperty(",
        "SendStringToExecute",
    ):
        if forbidden in text:
            errors.append("Workspace presentation-only property filter must not mutate project/CAD: " + forbidden)

print("QS3D Workspace property palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Workspace exposes a denser BLT-style Family/property palette with bounded multi-term search, Ctrl+Shift+F/Escape keyboard UX, source/override/editor/choice aliases, counts and wider editors; filtering remains presentation-only.")
