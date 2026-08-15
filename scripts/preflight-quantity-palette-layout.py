#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
errors = []

for path in (STORE, COORDINATOR):
    if not path.is_file():
        errors.append("missing Quantity palette layout source: " + str(path.relative_to(ROOT)))

if STORE.is_file():
    text = STORE.read_text(encoding="utf-8")
    required = (
        "public int WorkspacePaletteWidth { get; set; } = 640;",
        "public int RightPaletteWidth { get; set; } = 300;",
        "public int QuantityPaletteWidth { get; set; } = 330;",
        "public int QuantityPaletteHeight { get; set; } = 720;",
        "internal const int QuantityPaletteMinWidth = 280;",
        "internal const int QuantityPaletteMinHeight = 360;",
        'layout.QuantityPaletteWidth = Int(values, "QuantityPaletteWidth", layout.QuantityPaletteWidth);',
        'layout.QuantityPaletteHeight = Int(values, "QuantityPaletteHeight", layout.QuantityPaletteHeight);',
        'builder.Append("QuantityPaletteWidth=").AppendLine(layout.QuantityPaletteWidth.ToString(invariant));',
        'builder.Append("QuantityPaletteHeight=").AppendLine(layout.QuantityPaletteHeight.ToString(invariant));',
        "layout.QuantityPaletteWidth = Clamp(layout.QuantityPaletteWidth, QuantityPaletteMinWidth, 1200);",
        "layout.QuantityPaletteHeight = Clamp(layout.QuantityPaletteHeight, QuantityPaletteMinHeight, 2000);",
        "left.QuantityPaletteWidth == right.QuantityPaletteWidth",
        "left.QuantityPaletteHeight == right.QuantityPaletteHeight",
        "QuantityPaletteWidth = source.QuantityPaletteWidth,",
        "QuantityPaletteHeight = source.QuantityPaletteHeight,",
        "if (Equivalent(_current, next)) return;",
        'Path.Combine(root, "QS3D", "BricsCAD-V25", "ui-layout-v1.txt")',
    )
    for needle in required:
        if needle not in text:
            errors.append("UserUiLayoutStore missing Quantity palette persistence contract: " + needle)

    if text.count('Int(values, "QuantityPaletteWidth", layout.QuantityPaletteWidth)') != 1:
        errors.append("QuantityPaletteWidth must use one backward-compatible optional load path")
    if text.count('Int(values, "QuantityPaletteHeight", layout.QuantityPaletteHeight)') != 1:
        errors.append("QuantityPaletteHeight must use one backward-compatible optional load path")

    for forbidden in (".qsdb", "ProjectContextCoordinator", "ProjectState", "project.Metadata"):
        if forbidden in text:
            errors.append("per-user Quantity palette layout must remain outside project/QSDB state: " + forbidden)

if COORDINATOR.is_file():
    text = COORDINATOR.read_text(encoding="utf-8")
    required = (
        "private static PaletteSet? _quantityInsight;",
        "MinimumSize = new DrawingSize(UserUiLayoutStore.QuantityPaletteMinWidth, UserUiLayoutStore.QuantityPaletteMinHeight)",
        "_quantityInsight.DeviceIndependentSize = new WpfSize(layout.QuantityPaletteWidth, layout.QuantityPaletteHeight);",
        "var quantitySize = _quantityInsight?.DeviceIndependentSize;",
        "if (quantitySize.HasValue)",
        "layout.QuantityPaletteWidth = checked((int)Math.Round(quantitySize.Value.Width, MidpointRounding.AwayFromZero));",
        "layout.QuantityPaletteHeight = checked((int)Math.Round(quantitySize.Value.Height, MidpointRounding.AwayFromZero));",
        "if (_workspace == null && _right == null && _quantityInsight == null) return;",
        "var quantityVisible = IsQuantityInsightVisible;",
        "SetVisibility(workspaceVisible, rightVisible, quantityVisible);",
        "private static void SetVisibility(bool workspace, bool right, bool quantityInsight)",
        "if (_quantityInsight != null) _quantityInsight.Visible = quantityInsight;",
    )
    for needle in required:
        if needle not in text:
            errors.append("PaletteCoordinator missing independent Quantity palette layout contract: " + needle)

    stale = (
        "MinimumSize = new DrawingSize(280, 360)",
        "Math.Max(310, layout.RightPaletteWidth)",
        "new WpfSize(Math.Max(310, layout.RightPaletteWidth), layout.RightPaletteHeight)",
    )
    for needle in stale:
        if needle in text:
            errors.append("Quantity palette still borrows/hard-codes layout instead of using its own persisted policy: " + needle)

    if "_quantityInsight.Size =" in text:
        errors.append("Quantity palette must use DeviceIndependentSize for persisted WPF dimensions")

print("QS3D Quantity Insight palette layout persistence preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Quantity Insight has independent per-user dimensions and centralized visibility restore/persist wiring without QSDB mutation.")
