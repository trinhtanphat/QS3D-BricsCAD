#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RevisionWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Revision XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RevisionWindow.xaml is not well-formed: " + str(exc))
        root = None


def named(tag, name):
    if root is None:
        return None
    for node in root.iter(WPF + tag):
        if node.attrib.get(XAML_NS + "Name") == name:
            return node
    return None


header_grid = named("Grid", "RevisionReviewHeaderGrid")
if header_grid is None:
    errors.append("missing responsive Revision review header grid")
else:
    defs = header_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["*", "Auto"]:
        errors.append("RevisionReviewHeaderGrid must use ['*', 'Auto']; got " + repr(widths))

    children = list(header_grid)
    title_stack = next((x for x in children if x.tag == WPF + "StackPanel" and x.attrib.get("Grid.Column", "0") == "0"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "COMPARE • INSPECT • LOCATE"), None)
    if title_stack is None or title_stack.attrib.get("MinWidth") != "0":
        errors.append("Revision review title group must be shrinkable in flexible column 0")
    else:
        title = next((x for x in title_stack if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "REVISION REVIEW"), None)
        if title is None:
            errors.append("REVISION REVIEW title missing")
        elif title.attrib.get("TextWrapping") != "NoWrap" or title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("REVISION REVIEW title must use NoWrap + CharacterEllipsis")
    if gate is None:
        errors.append("COMPARE • INSPECT • LOCATE label missing")
    else:
        if gate.attrib.get("Grid.Column") != "1" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Revision review compare label must occupy right-aligned auto column 1")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Revision review compare label must remain NoWrap")

for token in (
    'x:Name="Header"',
    'x:Name="Tabs"',
    'x:Name="Grid"',
    'x:Name="SemanticGrid"',
    'x:Name="Totals"',
    'Content="Định vị" Click="OnLocateClick"',
    'MouseDoubleClick="OnGridDoubleClick"',
    'MouseDoubleClick="OnSemanticGridDoubleClick"',
    'Text="READ-ONLY REVIEW"',
    'Text="SEMANTIC + QUANTITY"',
):
    if token not in text:
        errors.append("Revision review/locate contract missing: " + token)

expected_quantity_columns = (
    ("Cấu kiện", "{Binding ElementId}"),
    ("Loại", "{Binding Category}"),
    ("Đại lượng", "{Binding QuantityName}"),
    ("Thay đổi", "{Binding Change}"),
    ("Trước", "{Binding Before, StringFormat=0.###}"),
    ("Sau", "{Binding After, StringFormat=0.###}"),
    ("Chênh lệch", "{Binding Delta, StringFormat=0.###}"),
    ("Chênh lệch %", "{Binding PercentChange, StringFormat=0.##}"),
)
expected_semantic_columns = (
    ("Cấu kiện", "{Binding ElementId}"),
    ("Loại", "{Binding Category}"),
    ("Thay đổi", "{Binding Change}"),
    ("Identity", "{Binding IdentityChangeCount}"),
    ("Property", "{Binding PropertyChangeCount}"),
    ("Quantity", "{Binding QuantityChangeCount}"),
    ("Source ref (ẩn)", "{Binding OmittedSourceReferenceChangeCount}"),
)


def grid_columns(grid):
    if grid is None:
        return None
    columns_node = grid.find(WPF + "DataGrid.Columns")
    columns = [] if columns_node is None else [
        x for x in list(columns_node) if x.tag == WPF + "DataGridTextColumn"
    ]
    return tuple((x.attrib.get("Header"), x.attrib.get("Binding")) for x in columns)


quantity_grid = named("DataGrid", "Grid")
semantic_grid = named("DataGrid", "SemanticGrid")
for label, grid, expected in (
    ("quantity", quantity_grid, expected_quantity_columns),
    ("semantic", semantic_grid, expected_semantic_columns),
):
    if grid is None:
        errors.append("Revision " + label + " DataGrid missing")
        continue
    if grid.attrib.get("AutoGenerateColumns") != "False" or grid.attrib.get("IsReadOnly") != "True" or grid.attrib.get("CanUserAddRows") != "False":
        errors.append("Revision " + label + " DataGrid must remain explicit/read-only/no-add")
    actual = grid_columns(grid)
    if actual != expected:
        errors.append("Revision " + label + " DataGrid schema/bindings changed: " + repr(actual))

# The footer intentionally uses LastChildFill=True correctly: the right pill is not final,
# and the final named Totals TextBlock is the fill child. Guard this so a broad docking fix
# does not accidentally rewrite valid layout semantics.
footer = None
if root is not None:
    for dock in root.iter(WPF + "DockPanel"):
        children = list(dock)
        if any(x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "Totals" for x in children):
            footer = dock
            break
if footer is None:
    errors.append("Revision footer DockPanel containing Totals missing")
else:
    if footer.attrib.get("LastChildFill") != "True":
        errors.append("Revision footer must intentionally retain LastChildFill=True")
    children = list(footer)
    if not children or children[-1].tag != WPF + "TextBlock" or children[-1].attrib.get(XAML_NS + "Name") != "Totals":
        errors.append("Revision footer Totals must remain the final fill child")
    right_pill = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("DockPanel.Dock") == "Right"), None)
    if right_pill is None:
        errors.append("Revision footer locate status pill must remain right-docked before Totals")
    else:
        label = next((x for x in right_pill.iter(WPF + "TextBlock") if x.attrib.get("Text") == "DOUBLE-CLICK ROW TO LOCATE"), None)
        if label is None:
            errors.append("Revision footer double-click locate wording missing")

stale = '<TextBlock DockPanel.Dock="Right" Text="COMPARE • INSPECT • LOCATE"'
if stale in text:
    errors.append("Revision review subheader still uses stale final-child right docking")

if errors:
    print("Revision Review responsive-header preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Revision review subheader is deterministic/responsive while read-only schemas, locate handlers and intentional footer fill semantics remain intact."
)
