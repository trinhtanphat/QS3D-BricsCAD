#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelHealthWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Model Health XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("ModelHealthWindow.xaml is not well-formed: " + str(exc))
        root = None


def named(tag, name):
    if root is None:
        return None
    for node in root.iter(WPF + tag):
        if node.attrib.get(XAML_NS + "Name") == name:
            return node
    return None


header = named("Grid", "ModelHealthIssueHeaderGrid")
if header is None:
    errors.append("missing responsive Model Health issue header grid")
else:
    defs = header.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["*", "Auto"]:
        errors.append("ModelHealthIssueHeaderGrid must use ['*', 'Auto']; got " + repr(widths))
    children = list(header)
    title = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "DANH SÁCH VẤN ĐỀ"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "DOUBLE-CLICK → CAD LOCATE"), None)
    if title is None:
        errors.append("Model Health issue-list title missing")
    else:
        if title.attrib.get("Grid.Column", "0") != "0" or title.attrib.get("MinWidth") != "0":
            errors.append("Model Health issue-list title must be shrinkable in flexible column 0")
        if title.attrib.get("TextWrapping") != "NoWrap" or title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Model Health issue-list title must use NoWrap + CharacterEllipsis")
    if gate is None:
        errors.append("Model Health double-click locate label missing")
    else:
        if gate.attrib.get("Grid.Column") != "1" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Model Health locate label must occupy right-aligned auto column 1")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Model Health locate label must remain NoWrap")

for token in (
    'x:Name="SummaryText"',
    'x:Name="SearchBox"',
    'TextChanged="OnFilterChanged"',
    'x:Name="SeverityCombo"',
    'SelectionChanged="OnFilterChanged"',
    'x:Name="VisibleCountText"',
    'x:Name="IssueGrid"',
    'Content="Định vị" Style="{StaticResource AccentButton}"',
    'Click="OnLocateClick"',
    'MouseDoubleClick="OnGridDoubleClick"',
    'Text="READ-ONLY TRIAGE"',
):
    if token not in text:
        errors.append("Model Health filter/locate/read-only contract missing: " + token)

issue_grid = named("DataGrid", "IssueGrid")
expected_columns = (
    ("Mức", "{Binding Severity}"),
    ("Mã", "{Binding Code}"),
    ("Cấu kiện", "{Binding ElementId}"),
    ("Nội dung", "{Binding Message}"),
)
if issue_grid is None:
    errors.append("Model Health IssueGrid missing")
else:
    if issue_grid.attrib.get("AutoGenerateColumns") != "False" or issue_grid.attrib.get("IsReadOnly") != "True" or issue_grid.attrib.get("CanUserAddRows") != "False":
        errors.append("Model Health IssueGrid must remain explicit/read-only/no-add")
    columns_node = issue_grid.find(WPF + "DataGrid.Columns")
    columns = [] if columns_node is None else [x for x in list(columns_node) if x.tag == WPF + "DataGridTextColumn"]
    actual = tuple((x.attrib.get("Header"), x.attrib.get("Binding")) for x in columns)
    if actual != expected_columns:
        errors.append("Model Health IssueGrid schema/bindings changed: " + repr(actual))

# Footer is deliberately valid: the right status pill is followed by the final explanatory fill child.
footer = None
if root is not None:
    for dock in root.iter(WPF + "DockPanel"):
        children = list(dock)
        if any(
            x.tag == WPF + "TextBlock"
            and x.attrib.get("Text", "").startswith("Lọc chỉ thay đổi danh sách đang xem.")
            for x in children
        ):
            footer = dock
            break
if footer is None:
    errors.append("Model Health footer DockPanel missing")
else:
    if footer.attrib.get("LastChildFill") != "True":
        errors.append("Model Health footer must intentionally retain LastChildFill=True")
    children = list(footer)
    if not children or children[-1].tag != WPF + "TextBlock" or not children[-1].attrib.get("Text", "").startswith("Lọc chỉ thay đổi danh sách đang xem."):
        errors.append("Model Health explanatory footer text must remain the final fill child")
    right_pill = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("DockPanel.Dock") == "Right"), None)
    if right_pill is None:
        errors.append("Model Health footer locate pill must remain right-docked before final fill text")
    elif not any(x.attrib.get("Text") == "ISSUE → CAD LOCATE" for x in right_pill.iter(WPF + "TextBlock")):
        errors.append("Model Health footer ISSUE → CAD LOCATE wording missing")

stale = '<TextBlock DockPanel.Dock="Right" Text="DOUBLE-CLICK → CAD LOCATE"'
if stale in text:
    errors.append("Model Health issue-list subheader still uses stale final-child right docking")

if errors:
    print("Model Health responsive-subheader preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Model Health issue-list header is deterministic/responsive while filters, read-only schema, locate handlers and intentional footer fill semantics remain intact."
)
