#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(errors="backslashreplace")

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelHealthWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelHealthWindow.xaml.cs"
errors = []

for path in (XAML, CODE):
    if not path.is_file():
        errors.append("missing Model Health review file: " + str(path.relative_to(ROOT)))

if XAML.is_file():
    try:
        ET.parse(XAML)
    except ET.ParseError as exc:
        errors.append("ModelHealthWindow.xaml is not well-formed XML: " + str(exc))

    text = XAML.read_text(encoding="utf-8")
    for needle in (
        'Text="HEALTH REVIEW"',
        'x:Name="SummaryText"',
        'x:Name="SearchBox"',
        'TextChanged="OnFilterChanged"',
        'x:Name="SeverityCombo"',
        'SelectionChanged="OnFilterChanged"',
        'Tag="All"', 'Tag="Error"', 'Tag="Warning"', 'Tag="Info"',
        'x:Name="VisibleCountText"',
        'x:Name="IssueGrid"',
        'Click="OnLocateClick"',
        'MouseDoubleClick="OnGridDoubleClick"',
        'Text="READ-ONLY TRIAGE • ISSUE → CAD LOCATE"',
    ):
        if needle not in text:
            errors.append("ModelHealthWindow.xaml missing review UI contract: " + needle)

if CODE.is_file():
    text = CODE.read_text(encoding="utf-8")
    for needle in (
        "private readonly IReadOnlyList<ModelHealthIssue> _issues;",
        "_issues = issues.ToList();",
        "private void OnFilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();",
        "private void ApplyFilter()",
        "MatchesSeverity(issue, severity)",
        "MatchesSearch(issue, query)",
        "StringComparison.OrdinalIgnoreCase",
        "IssueGrid.ItemsSource = filtered;",
        'VisibleCountText.Text = filtered.Count + " / " + _issues.Count;',
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var current)",
        "current.ProjectId", "current.UpdatedUtc", "current.ChangeVersion", "current.DrawingFingerprint",
        "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
        "SearchBox.IsEnabled = false", "SeverityCombo.IsEnabled = false", 'VisibleCountText.Text = "STALE"',
        "_locate(issue)",
    ):
        if needle not in text:
            errors.append("ModelHealthWindow.xaml.cs missing review/freshness contract: " + needle)

    for forbidden in (
        "ComprehensiveModelHealthService",
        "ModelHealthService(",
        "project.Touch(",
        ".MarkDirty(",
        ".MarkClean(",
        "ProjectContextCoordinator.GetOrCreate(",
        "SendStringToExecute(",
    ):
        if forbidden in text:
            errors.append("Model Health review UI must remain in-memory/read-only; found forbidden token: " + forbidden)

print("QS3D Model Health review UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Model Health review provides in-memory search/severity triage and visible counts while preserving stale-snapshot and active-DWG locate guards.")
