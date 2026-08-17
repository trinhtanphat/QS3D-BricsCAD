#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing {relative}")
    return path.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL:", message)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(f"{label}: missing {needle!r}")


def require_order(text: str, needles, label: str) -> None:
    cursor = -1
    for needle in needles:
        index = text.find(needle, cursor + 1)
        if index < 0:
            fail(f"{label}: missing {needle!r}")
        if index <= cursor:
            fail(f"{label}: out-of-order {needle!r}")
        cursor = index


quantity = read("src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs")
quantity_icons = read("src/QS3D.BricsCAD.V25/Ribbon/BltQuantityIconPolisher.cs")
coordinator = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
topbar = read("src/QS3D.BricsCAD.V25/Ribbon/BltTopbarTabContract.cs")

# Owner-reference topbar identity and position: ĐỊNH LƯỢNG stays between XEM and BẢN SỬA ĐỔI.
require_order(
    topbar,
    (
        'new TabSpec("QS3D_VIEW", "XEM")',
        'new TabSpec("QS3D_QTY", "ĐỊNH LƯỢNG")',
        'new TabSpec("QS3D_REV", "BẢN SỬA ĐỔI")',
    ),
    "quantity topbar order",
)

# The reference augmenter owns exactly two visible panels after removing every historical QS3D
# quantity layout. Native/third-party content is not part of this owned-ID cleanup.
for source_id in (
    "QS3D_QTY_EXCEL_PANEL_SOURCE",
    "QS3D_QTY_OPENINGS_PANEL_SOURCE",
    "QS3D_QTY_REBAR_SCHEDULE_PANEL_SOURCE",
    "QS3D_QTY_REBAR_3D_PANEL_SOURCE",
    "QS3D_QTY_REBAR_HEALTH_PANEL_SOURCE",
    "QS3D_QTY_REFERENCE_PANEL_SOURCE",
    "QS3D_QTY_PANEL_SOURCE",
):
    require(quantity, f'"{source_id}"', "legacy quantity panel cleanup")

panel_calls = re.findall(r"AddPanel\(panels,\s*[A-Za-z]+PanelSourceId,\s*\"[^\"]+\"", quantity)
if panel_calls != [
    'AddPanel(panels, SettingsPanelSourceId, "Cài đặt"',
    'AddPanel(panels, QuantityPanelSourceId, "Khối lượng"',
]:
    fail("quantity augmenter must add exactly the Cài đặt and Khối lượng panels in reference order")

# Lock the exact owner-visible button order and production command routing.
button_ids = (
    "QS3D_QTY_BLT_SETTINGS",
    "QS3D_QTY_BLT_CALCULATE",
    "QS3D_QTY_BLT_EXPORT",
    "QS3D_QTY_BLT_VIEW",
    "QS3D_QTY_BLT_EXPLAIN",
    "QS3D_QTY_BLT_COMPARE",
)
require_order(
    quantity,
    (
        '"QS3D_QTY_BLT_SETTINGS",\n                "Cài đặt\\ntính toán",\n                "QS3DQUANTITYSETTINGS"',
        '"QS3D_QTY_BLT_CALCULATE",\n                "Tính khối lượng\\n(Engine2)",\n                "QS3DREGEN"',
        '"QS3D_QTY_BLT_EXPORT",\n                "Xuất\\n.blte2",\n                "QS3DED2"',
        '"QS3D_QTY_BLT_VIEW",\n                "Xem khối\\nlượng",\n                "QS3DBQ"',
        '"QS3D_QTY_BLT_EXPLAIN",\n                "Diễn\\ngiải",\n                "QS3DQUANTITYINSIGHT"',
        '"QS3D_QTY_BLT_COMPARE",\n                "Đối chiếu\\nCũ/Mới",\n                "QS3DREVDIFF"',
    ),
    "quantity reference buttons",
)

for token in (
    'SetProperty(button, "ShowText", true);',
    'SetProperty(button, "ShowImage", true);',
    'SetEnumProperty(button, "Size", "Large");',
    'SetProperty(button, "Image", RibbonIconFactory.Create(spec.Icon, 16));',
    'SetProperty(button, "LargeImage", RibbonIconFactory.Create(spec.Icon, 32));',
):
    require(quantity, token, "large icon-first quantity button")

# v160 had source-level Image/LargeImage assignments but no runtime read-back gate. The dedicated
# final pass must own all six exact reference IDs, reapply distinct 16/32 bitmap image properties,
# request native Large presentation, and refuse to mark the pass initialized unless BricsCAD
# exposes both images back.
for button_id in button_ids:
    require(quantity_icons, f'case "{button_id}":', "quantity icon ID ownership")

for token in (
    'if (polished != 6)',
    'SetProperty(item, "Image", CreateIcon(kind, 16));',
    'SetProperty(item, "LargeImage", CreateIcon(kind, 32));',
    'SetProperty(item, "ShowImage", true);',
    'SetEnumProperty(item, "Size", "Large");',
    'private static bool HasCompleteVisibleIcon(object item)',
    'GetProperty(item, "Image") != null',
    'GetProperty(item, "LargeImage") != null',
    'private static ImageSource CreateIcon(IconKind kind, int pixelSize)',
    'using System.Windows.Media.Imaging;',
    'new DrawingVisual()',
    'drawing.PushTransform(new ScaleTransform(pixelSize / 32.0, pixelSize / 32.0));',
    'new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32)',
    'bitmap.Render(visual);',
    'bitmap.Freeze();',
):
    require(quantity_icons, token, "quantity final bitmap/read-back contract")

if "new DrawingImage(" in quantity_icons:
    fail("quantity final icon pass must not assign direct DrawingImage sources to BricsCAD Ribbon buttons")

for forbidden in (
    'SetProperty(item, "Image", image);',
    'SetProperty(item, "LargeImage", image);',
):
    if forbidden in quantity_icons:
        fail(f"quantity final icon pass must not reuse one unsized image for both Ribbon slots: {forbidden}")

# Distinct screenshot-familiar cues are clean-room vector geometry, not embedded owner assets.
for cue in (
    "AddGear(group, blueDeep, bluePale",
    "Amber lightning bolt",
    "Green upward export arrow",
    "Four-column quantity chart",
    "White report/table sheet",
    "Blue balance with amber pivot/knob",
):
    require(quantity_icons, cue, "quantity clean-room visual cue")

require(quantity, "if (!BltQuantityIconPolisher.TryInitialize())", "quantity host-tree icon finalization")
require(quantity, "BltQuantityIconPolisher.Reset();", "quantity icon lifecycle reset")

polish_call = quantity.find("if (!BltQuantityIconPolisher.TryInitialize())")
initialized = quantity.find("_initialized = true;", polish_call)
if polish_call < 0 or initialized < 0 or polish_call >= initialized:
    fail("quantity icon host read-back must succeed before QuantityReferenceRibbonAugmenter becomes initialized")

# Stop/Start must be a real lifecycle boundary. Without Reset(), the static _initialized flags can
# skip reapplication after BricsCAD recreates the Ribbon tree during a coordinator restart.
require(quantity, "public static void Reset()", "quantity reset primitive")
require(coordinator, "QuantityReferenceRibbonAugmenter.Reset();", "coordinator quantity teardown")
require(coordinator, "ready = QuantityReferenceRibbonAugmenter.TryInitialize() && ready;", "coordinator quantity initialization")

stop_start = coordinator.find("public static void Stop()")
reset = coordinator.find("QuantityReferenceRibbonAugmenter.Reset();", stop_start)
try_initialize_all = coordinator.find("private static bool TryInitializeAll()")
if stop_start < 0 or reset < 0 or try_initialize_all < 0 or reset >= try_initialize_all:
    fail("quantity Reset must execute from RibbonInitializationCoordinator.Stop before the next initialization lifecycle")

print(
    "PASS: ĐỊNH LƯỢNG keeps the two-panel BLT3D-reference layout and exact command order, "
    "reapplies six clean-room screenshot-familiar vector icons as distinct frozen 16/32 bitmaps, "
    "verifies native Image/LargeImage read-back before initialization, and remains restart-safe."
)
