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

# Stop/Start must be a real lifecycle boundary. Without Reset(), the static _initialized flag can
# skip reapplication after BricsCAD recreates the Ribbon tree during a coordinator restart.
require(quantity, "public static void Reset() => _initialized = false;", "quantity reset primitive")
require(coordinator, "QuantityReferenceRibbonAugmenter.Reset();", "coordinator quantity teardown")
require(coordinator, "ready = QuantityReferenceRibbonAugmenter.TryInitialize() && ready;", "coordinator quantity initialization")

stop_start = coordinator.find("public static void Stop()")
reset = coordinator.find("QuantityReferenceRibbonAugmenter.Reset();", stop_start)
try_initialize_all = coordinator.find("private static bool TryInitializeAll()")
if stop_start < 0 or reset < 0 or try_initialize_all < 0 or reset >= try_initialize_all:
    fail("quantity Reset must execute from RibbonInitializationCoordinator.Stop before the next initialization lifecycle")

print(
    "PASS: ĐỊNH LƯỢNG keeps the BLT3D topbar position, exactly two reference panels, exact owner button/command order, large icon-first presentation, legacy-panel cleanup, and restart-safe augmenter reset/reapply lifecycle."
)
