#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AUGMENTER = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
PLUGIN = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"
BOOTSTRAP = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
errors = []

for path in (AUGMENTER, PLUGIN, BOOTSTRAP):
    if not path.is_file():
        errors.append("missing quantity Ribbon parity source: " + str(path.relative_to(ROOT)))

button_specs = (
    ('QS3D_QTY_REF_SETTINGS', 'Cài đặt tính toán', 'QS3DQUANTITYSETTINGS'),
    ('QS3D_QTY_REF_CALCULATE', 'Tính khối lượng', 'QS3DREGEN'),
    ('QS3D_QTY_REF_ED2', 'Xuất ED2', 'QS3DED2'),
    ('QS3D_QTY_REF_VIEW', 'Xem khối lượng', 'QS3DBQ'),
    ('QS3D_QTY_REF_EXPLAIN', 'Diễn giải', 'QS3DBQ'),
    ('QS3D_QTY_REF_WALL', 'Khối lượng tường', 'QS3DWALLQTY'),
    ('QS3D_QTY_REF_EXCELLOCATE', 'Excel → CAD', 'QS3DEXCELLOCATE'),
    ('QS3D_QTY_REF_COMPARE', 'Đối chiếu Cũ/Mới', 'QS3DREVDIFF'),
)

if AUGMENTER.is_file():
    text = AUGMENTER.read_text(encoding="utf-8")
    required = (
        'private const string TabId = "QS3D_QTY";',
        'private const string PanelSourceId = "QS3D_QTY_REFERENCE_PANEL_SOURCE";',
        'private const string PanelTitle = "Tính khối lượng";',
        'var source = FindPanelSource(panelEnumerable, PanelSourceId) ?? CreatePanel(panels);',
        'var button = FindById(items, spec.Id);',
        'SetProperty(button, "CommandParameter", spec.Command);',
        'SetProperty(button, "CommandHandler", new CommandHandler());',
        'if (quantityTab == null) return false;',
        'Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(normalized + " ", true, false, false);',
    )
    for needle in required:
        if needle not in text:
            errors.append("QuantityReferenceRibbonAugmenter missing reconciliation contract: " + needle)

    for button_id, label, command in button_specs:
        token = f'new ButtonSpec("{button_id}", "{label}", "{command}")'
        if text.count(token) != 1:
            errors.append("expected exactly one quantity reference button spec: " + token)

    for forbidden in (".Clear()", "Remove(", "RibbonBootstrapper.Reset()", "new RibbonTab"):
        if forbidden in text:
            errors.append("quantity reference augmenter must remain additive/non-destructive: " + forbidden)

if PLUGIN.is_file():
    text = PLUGIN.read_text(encoding="utf-8")
    required = (
        "RibbonBootstrapper.TryInitialize();",
        "QuantityReferenceRibbonAugmenter.TryInitialize();",
        "QuantityReferenceRibbonAugmenter.Reset();",
        "RibbonBootstrapper.Reset();",
    )
    for needle in required:
        if needle not in text:
            errors.append("PluginEntry missing Quantity reference lifecycle hook: " + needle)
    bootstrap_pos = text.find("RibbonBootstrapper.TryInitialize();")
    quantity_pos = text.find("QuantityReferenceRibbonAugmenter.TryInitialize();")
    if bootstrap_pos < 0 or quantity_pos < 0 or bootstrap_pos >= quantity_pos:
        errors.append("Quantity reference augmenter must run after the canonical Ribbon bootstrap creates QS3D_QTY")

if BOOTSTRAP.is_file():
    text = BOOTSTRAP.read_text(encoding="utf-8")
    required_existing = (
        '"QS3D_QTY",',
        '"ĐỊNH LƯỢNG",',
        'Panel("QUANTITY", "Khối lượng",',
        'Button("Regenerate", "QS3DREGEN")',
        'Button("BQ", "QS3DBQ")',
        'Button("Takeoff", "QS3DTAKEOFF")',
        'Panel("EXCEL", "Excel ↔ CAD",',
        'Button("ED2 • Excel ↔ CAD", "QS3DED2")',
        'Button("Excel → CAD", "QS3DEXCELLOCATE")',
        'Panel("OPENINGS", "Cửa & lỗ mở",',
        'Panel("REBAR_SCHEDULE", "BBS",',
        'Panel("REBAR_3D", "Cốt thép 3D",',
        'Panel("REBAR_HEALTH", "Health cốt thép",',
        "ReconcileTab(tabs, tabSpec);",
        "EnsurePanelButtons(tabSpec, panelSpec, source);",
    )
    for needle in required_existing:
        if needle not in text:
            errors.append("canonical QS3D_QTY bootstrap contract disappeared: " + needle)

adapter_root = ROOT / "src/QS3D.BricsCAD.V25"
command_source = "\n".join(
    path.read_text(encoding="utf-8")
    for path in adapter_root.rglob("*.cs")
    if path.is_file() and path != AUGMENTER
)
for command in sorted({spec[2] for spec in button_specs}):
    marker = f'[CommandMethod("{command}"'
    if marker not in command_source:
        errors.append("quantity reference Ribbon points to an unregistered adapter command: " + command)

print("QS3D ĐỊNH LƯỢNG Ribbon reference-parity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the additive QS3D_QTY reference panel reconciles eight real workflows by stable IDs, preserves canonical quantity panels, dispatches on the active DWG, and does not clear/remove Ribbon state.")
