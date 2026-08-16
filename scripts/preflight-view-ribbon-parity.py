#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
view_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/BltViewRibbonAugmenter.cs"
bootstrap_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
init_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"

errors = []
for path in (view_path, bootstrap_path, init_path):
    if not path.is_file():
        errors.append(f"missing required source: {path.relative_to(ROOT)}")

if not errors:
    view = view_path.read_text(encoding="utf-8")
    bootstrap = bootstrap_path.read_text(encoding="utf-8")
    init = init_path.read_text(encoding="utf-8")

    panel_contract = [
        ("QS3D_VIEW_ORIENTATION_PANEL_SOURCE", "Góc nhìn"),
        ("QS3D_VIEW_FOCUS_PANEL_SOURCE", "Tập trung"),
        ("QS3D_VIEW_SECTION_PANEL_SOURCE", "Mặt cắt"),
        ("QS3D_VIEW_ZOOM_PANEL_SOURCE", "Điều hướng"),
        ("QS3D_VIEW_WORKSPACE_PANEL_SOURCE", "Workspace"),
    ]
    last = -1
    for source_id, title in panel_contract:
        pos = view.find(f'"{source_id}"')
        if pos < 0:
            errors.append(f"XEM panel source missing: {source_id}")
        elif pos <= last:
            errors.append(f"XEM panel order drifted at: {source_id}")
        last = max(last, pos)
        if f'"{title}"' not in view:
            errors.append(f"XEM panel title missing: {title}")

    button_contract = [
        ("QS3D_VIEW_ORIENTATION_3D", "3D", "QS3DVIEW3D"),
        ("QS3D_VIEW_ORIENTATION_TOP", "Top", "QS3DVIEWTOP"),
        ("QS3D_VIEW_ORIENTATION_ORBIT", "Orbit", "QS3DORBIT"),
        ("QS3D_VIEW_FOCUS_FOCUS", "Focus", "QS3DFOCUS"),
        ("QS3D_VIEW_FOCUS_CÔLẬP", "Cô lập", "QS3DISOLATE"),
        ("QS3D_VIEW_FOCUS_KHÔIPHỤC", "Khôi phục", "QS3DUNISOLATE"),
        ("QS3D_VIEW_SECTION_SECTIONBOX", "Section Box", "QS3DSECTIONBOX"),
        ("QS3D_VIEW_SECTION_SECTIONPLANE", "Section Plane", "QS3DSECTIONPLANE"),
        ("QS3D_VIEW_SECTION_CLIPDISPLAY", "Clip Display", "QS3DCLIPDISPLAY"),
        ("QS3D_VIEW_ZOOM_ZOOMCHỌN", "Zoom chọn", "QS3DZOOMSELECTED"),
        ("QS3D_VIEW_ZOOM_ZOOMALL", "Zoom all", "QS3DZOOMALL"),
        ("QS3D_VIEW_WORKSPACE_WORKSPACE", "Workspace", "QS3D"),
        ("QS3D_VIEW_WORKSPACE_BQ", "BQ", "QS3DBQ"),
        ("QS3D_VIEW_WORKSPACE_REFRESH", "Refresh", "QS3DREFRESH"),
    ]
    for button_id, text, command in button_contract:
        if f'"{button_id}"' not in view:
            errors.append(f"XEM visual button missing: {button_id}")
        if f'"{text}"' not in view:
            errors.append(f"XEM button label missing: {text}")
        if f'Button("{text}", "{command}")' not in bootstrap:
            errors.append(f"XEM bootstrap command route drifted: {text} -> {command}")

    required_visual_tokens = (
        'SetProperty(button, "ShowText", true);',
        'SetProperty(button, "ShowImage", true);',
        'SetEnumProperty(button, "Size", "Standard");',
        'SetProperty(button, "Image", CreateIcon(spec.Icon));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Icon));',
        'new DrawingImage(group)',
        'Brushes.Transparent',
    )
    for token in required_visual_tokens:
        if token not in view:
            errors.append(f"XEM icon-forward presentation contract missing: {token}")

    # Presentation-only: do not silently change any of the already-qualified command routes.
    decorate_start = view.find("private static void DecorateButton")
    decorate_end = view.find("private static ImageSource CreateIcon", decorate_start)
    if decorate_start < 0 or decorate_end < 0:
        errors.append("XEM DecorateButton presentation boundary missing")
    else:
        decorate = view[decorate_start:decorate_end]
        for forbidden in ("CommandParameter", "CommandHandler", "SendStringToExecute"):
            if forbidden in decorate:
                errors.append(f"XEM presentation augmenter must not rewrite routing: {forbidden}")

    icon_names = [
        "View3d", "Top", "Orbit", "Focus", "Isolate", "Restore", "SectionBox",
        "SectionPlane", "ClipDisplay", "ZoomSelected", "ZoomAll", "Workspace",
        "Quantity", "Refresh",
    ]
    for icon in icon_names:
        if f"case ViewIconKind.{icon}:" not in view:
            errors.append(f"distinct XEM semantic icon case missing: {icon}")

    if "BltViewRibbonAugmenter.Reset();" not in init:
        errors.append("XEM augmenter is not reset on plugin shutdown")
    init_call = "ready = BltViewRibbonAugmenter.TryInitialize() && ready;"
    fallback_call = "ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;"
    init_pos = init.find(init_call)
    fallback_pos = init.find(fallback_call)
    if init_pos < 0:
        errors.append("XEM augmenter is not initialized")
    elif fallback_pos < 0 or init_pos > fallback_pos:
        errors.append("XEM augmenter must run before generic bootstrap icon fallback")

    if '"QS3D_VIEW",\n                "XEM"' not in bootstrap:
        errors.append("canonical QS3D_VIEW / XEM tab contract missing from RibbonBootstrapper")

    # Clean-room boundary: vector recreation only; no copied BLT raster/binary asset paths.
    for forbidden in (".png", ".ico", ".bmp", "private-user-images", "BLT3D.exe", "BLT3D.dll"):
        if forbidden.lower() in view.lower():
            errors.append(f"XEM augmenter must not embed/copy proprietary asset reference: {forbidden}")

if errors:
    print("VIEW RIBBON PARITY PREFLIGHT: FAIL")
    for error in errors:
        print(f"- {error}")
    raise SystemExit(1)

print("VIEW RIBBON PARITY PREFLIGHT: PASS")
print("- XEM preserves the canonical five-panel / fourteen-action view workflow.")
print("- Every visible XEM action receives a distinct locally-generated vector icon.")
print("- The augmenter changes presentation only and preserves existing command routing.")
print("- Lifecycle order lets XEM icons win over generic bootstrap fallback imagery.")
