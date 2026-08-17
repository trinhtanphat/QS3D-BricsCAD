#!/usr/bin/env python3
from pathlib import Path
import re
import unicodedata

ROOT = Path(__file__).resolve().parents[1]
ACTION_REL = "src/QS3D.BricsCAD.V25/Ribbon/BltViewActionOverrideAugmenter.cs"
INIT_REL = "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"


def read(rel):
    text = (ROOT / rel).read_text(encoding="utf-8")
    return unicodedata.normalize("NFC", text.replace("\r\n", "\n").replace("\r", "\n"))


def fail(message):
    raise SystemExit("FAIL: " + message)


action = read(ACTION_REL)
init = read(INIT_REL)

action_contract = [
    (
        "QS3D_VIEW_SECTION_SECTIONBOX",
        "Tối ưu đồ họa",
        "QS3DOPTIMIZEGRAPHICS",
        "OptimizeGraphics",
    ),
    (
        "QS3D_VIEW_SECTION_SECTIONPLANE",
        "Section Box",
        "QS3DSECTIONBOX",
        "SectionBox",
    ),
    (
        "QS3D_VIEW_SECTION_CLIPDISPLAY",
        "Cắt theo đối tượng",
        "QS3DCUTBYOBJECT",
        "CutByObject",
    ),
]

last = -1
for button_id, label, command, icon in action_contract:
    pattern = re.compile(
        r'new\s+ActionSpec\(\s*'
        + re.escape(f'"{button_id}"')
        + r'\s*,\s*'
        + re.escape(f'"{label}"')
        + r'\s*,\s*'
        + re.escape(f'"{command}"')
        + r'\s*,\s*ActionIconKind\.'
        + re.escape(icon)
        + r'\s*\)',
        re.DOTALL,
    )
    match = pattern.search(action)
    if match is None:
        fail(f"{ACTION_REL} lost XEM action contract: {label} -> {command}")
    if match.start() <= last:
        fail(f"{ACTION_REL} XEM action order drifted at: {label}")
    last = match.start()

for token in (
    'private const string ViewTabId = "QS3D_VIEW";',
    'private const string DisplayPanelSourceId = "QS3D_VIEW_SECTION_PANEL_SOURCE";',
    'SetProperty(source, "Name", "Hiển thị");',
    'SetProperty(source, "Title", "Hiển thị");',
    'SetProperty(button, "ShowText", true);',
    'SetProperty(button, "ShowImage", true);',
    'SetEnumProperty(button, "Size", "Large");',
    'SetProperty(button, "Image", CreateIcon(spec.Icon, 16));',
    'SetProperty(button, "LargeImage", CreateIcon(spec.Icon, 32));',
    'private static ImageSource CreateIcon(ActionIconKind kind, int pixelSize)',
    'using System.Windows.Media.Imaging;',
    'new DrawingVisual()',
    'drawing.PushTransform(new ScaleTransform(pixelSize / 32.0, pixelSize / 32.0));',
    'drawing.DrawDrawing(group);',
    'new RenderTargetBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Pbgra32)',
    'bitmap.Render(visual);',
    'bitmap.Freeze();',
):
    if token not in action:
        fail(f"{ACTION_REL} missing XEM bitmap icon contract: {token}")

if "new DrawingImage(" in action:
    fail(f"{ACTION_REL} must not assign direct DrawingImage sources to BricsCAD Ribbon buttons")

apply_start = action.find("private static void ApplyAction")
create_start = action.find("private static ImageSource CreateIcon", apply_start)
if apply_start < 0 or create_start < 0:
    fail(f"{ACTION_REL} presentation boundary missing")
apply_body = action[apply_start:create_start]

for forbidden in (
    'SetProperty(button, "Image", icon);',
    'SetProperty(button, "LargeImage", icon);',
):
    if forbidden in apply_body:
        fail(f"{ACTION_REL} must not reuse one unsized image for both Ribbon slots: {forbidden}")

base_call = "ready = BltViewRibbonAugmenter.TryInitialize() && ready;"
override_call = "ready = BltViewActionOverrideAugmenter.TryInitialize() && ready;"
fallback_call = "ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;"
base_pos = init.find(base_call)
override_pos = init.find(override_call)
fallback_pos = init.find(fallback_call)
if min(base_pos, override_pos, fallback_pos) < 0:
    fail(f"{INIT_REL} missing XEM initialization/fallback lifecycle")
if not (base_pos < override_pos < fallback_pos):
    fail(f"{INIT_REL} must keep base XEM -> owner-reference override -> generic fallback order")

# Clean-room rule: keep locally generated vector art; do not add copied BLT raster/binary assets.
quoted_raster = re.compile(r'''["'][^"'\r\n]*\.(?:png|ico|bmp)["']''', re.IGNORECASE)
if quoted_raster.search(action):
    fail(f"{ACTION_REL} must not reference copied raster assets")
for forbidden in ("private-user-images", "BLT3D.exe", "BLT3D.dll"):
    if forbidden.lower() in action.lower():
        fail(f"{ACTION_REL} must not reference proprietary BLT3D asset/binary: {forbidden}")

print("PASS: XEM Hiển thị keeps the three owner-reference actions and rasterizes clean-room vector glyphs to distinct frozen 16/32 px Ribbon bitmaps.")
