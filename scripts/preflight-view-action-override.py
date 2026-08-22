#!/usr/bin/env python3
from pathlib import Path
import re
import unicodedata

ROOT = Path(__file__).resolve().parents[1]
override_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/BltViewActionOverrideAugmenter.cs"
init_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
fallback_path = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonCommandParameterFallback.cs"
section_path = ROOT / "src/QS3D.BricsCAD.V25/SectionReviewCommands.cs"
graphics_path = ROOT / "src/QS3D.BricsCAD.V25/GraphicsOptimizationCommands.cs"
planner_path = ROOT / "src/QS3D.Core/Geometry/SectionDetailVolumePlanner.cs"
planner_smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/SectionDetailVolumePlannerSmoke.cs"
smoke_registration_path = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"


def source(path):
    text = path.read_text(encoding="utf-8")
    return unicodedata.normalize("NFC", text.replace("\r\n", "\n").replace("\r", "\n"))


errors = []
for path in (
    override_path,
    init_path,
    fallback_path,
    section_path,
    graphics_path,
    planner_path,
    planner_smoke_path,
    smoke_registration_path,
):
    if not path.is_file():
        errors.append("missing required source: " + str(path.relative_to(ROOT)))

if not errors:
    override = source(override_path)
    init = source(init_path)
    fallback = source(fallback_path)
    section = source(section_path)
    graphics = source(graphics_path)
    planner = source(planner_path)
    planner_smoke = source(planner_smoke_path)
    smoke_registration = source(smoke_registration_path)

    if '"Hiển thị"' not in override:
        errors.append("XEM display panel title must be Hiển thị")

    action_contract = [
        ("QS3D_VIEW_SECTION_SECTIONBOX", "Tối ưu đồ họa", "QS3DOPTIMIZEGRAPHICS", "OptimizeGraphics"),
        ("QS3D_VIEW_SECTION_SECTIONPLANE", "Section Box", "QS3DSECTIONBOX", "SectionBox"),
        ("QS3D_VIEW_SECTION_CLIPDISPLAY", "Cắt theo đối tượng", "QS3DCUTBYOBJECT", "CutByObject"),
    ]
    previous = -1
    for stable_id, label, command, icon in action_contract:
        pos = override.find(f'"{stable_id}"')
        if pos < 0:
            errors.append("stable XEM action slot missing: " + stable_id)
        elif pos <= previous:
            errors.append("XEM display action order drifted at: " + label)
        previous = max(previous, pos)
        for token, description in (
            (f'"{label}"', "label"),
            (f'"{command}"', "command"),
            (f'ActionIconKind.{icon}', "icon"),
        ):
            if token not in override:
                errors.append(f"XEM {description} missing for {label}: {token}")

    required_override_tokens = (
        'SetProperty(source, "Name", "Hiển thị");',
        'SetProperty(source, "Title", "Hiển thị");',
        'SetProperty(button, "ShowText", true);',
        'SetProperty(button, "ShowImage", true);',
        'SetEnumProperty(button, "Size", "Large");',
        'SetProperty(button, "CommandParameter", spec.Command);',
        'SetProperty(button, "Image", CreateIcon(spec.Icon, 16));',
        'SetProperty(button, "LargeImage", CreateIcon(spec.Icon, 32));',
    )
    for token in required_override_tokens:
        if token not in override:
            errors.append("XEM owner-reference override contract missing: " + token)

    if 'SetProperty(button, "CommandHandler"' in override:
        errors.append("XEM action override must not replace the bootstrap ICommand handler")
    if "SendStringToExecute(" in override:
        errors.append("XEM action override must route through CommandParameter, not execute commands itself")

    base_call = "ready = BltViewRibbonAugmenter.TryInitialize() && ready;"
    override_call = "ready = BltViewActionOverrideAugmenter.TryInitialize() && ready;"
    fallback_call = "ready = RibbonCommandParameterFallback.TryInitialize() && ready;"
    base_pos = init.find(base_call)
    override_pos = init.find(override_call)
    fallback_pos = init.find(fallback_call)
    if base_pos < 0 or override_pos < 0 or fallback_pos < 0:
        errors.append("XEM action override lifecycle call is incomplete")
    elif not (base_pos < override_pos < fallback_pos):
        errors.append("XEM action override must run after base view decoration and before command fallback capture")
    if "BltViewActionOverrideAugmenter.Reset();" not in init:
        errors.append("XEM action override is not reset on plugin shutdown")

    fallback_requirements = (
        'GetProperty(item, "CommandParameter") as string',
        "new CommandParameterFallbackHandler(handler, command)",
    )
    for token in fallback_requirements:
        if token not in fallback:
            errors.append("command parameter fallback contract drifted: " + token)

    graphics_requirements = (
        '[CommandMethod("QS3DOPTIMIZEGRAPHICS", CommandFlags.Modal)]',
        'ApplySetting("RETAINEDGRAPHICS", 1',
        'ApplySetting("RENDERUSINGHARDWARE", 1',
        "Application.GetSystemVariable(name)",
        "Application.SetSystemVariable(name, CoerceTarget(current, target))",
        "document.Editor.Regen();",
    )
    for token in graphics_requirements:
        if token not in graphics:
            errors.append("Tối ưu đồ họa implementation contract missing: " + token)

    cut_requirements = (
        '[CommandMethod("QS3DCUTBYOBJECT", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        "editor.SelectImplied()",
        "editor.GetSelection()",
        "entity.GeometricExtents",
        "editor.CurrentUserCoordinateSystem.Inverse()",
        ".TransformBy(worldToUcs)",
        "!Finite(minX)",
        "!Finite(maxZ)",
        "SectionDetailVolumePlanner.TryCreate(",
        "BuildDetailCommand(minPoint, maxPoint)",
        'private const string BimDetailCommand = "_BIMSECTION _Detail ";',
        "document.SendStringToExecute(command, true, false, true);",
    )
    for token in cut_requirements:
        if token not in section:
            errors.append("Cắt theo đối tượng implementation contract missing: " + token)

    planner_requirements = (
        "!Finite(spanX)",
        "!Finite(spanY)",
        "!Finite(spanZ)",
        "firstX < minX",
        "firstY < minY",
        "baseZ < minZ",
        "oppositeX > maxX",
        "oppositeY > maxY",
        "paddedTopZ > maxZ",
        "paddedHeight > spanZ",
        "representedTopZ > maxZ",
    )
    for token in planner_requirements:
        if token not in planner:
            errors.append("Cắt theo đối tượng precision planner contract missing: " + token)

    smoke_requirements = (
        "LargeCoordinatePaddingCollapseFailsClosed",
        "SpanOverflowFailsClosed",
        "NonFiniteInputFailsClosed",
        "RepresentableLargeCoordinatesRemainSupported",
        "SectionDetailVolumePlannerSmoke.Run();",
    )
    combined_smoke = planner_smoke + "\n" + smoke_registration
    for token in smoke_requirements:
        if token not in combined_smoke:
            errors.append("Cắt theo đối tượng deterministic precision regression missing: " + token)

    # Locally generated WPF vectors only. Do not pull proprietary/reference raster assets into QS3D.
    quoted_raster = re.compile(r'''["'][^"'\r\n]*\.(?:png|ico|bmp)["']''', re.IGNORECASE)
    if quoted_raster.search(override):
        errors.append("XEM action override must not embed proprietary raster asset references")
    for forbidden in ("private-user-images", "BLT3D.exe", "BLT3D.dll"):
        if forbidden.lower() in override.lower():
            errors.append("XEM action override embeds forbidden reference asset: " + forbidden)

if errors:
    print("VIEW ACTION OVERRIDE PREFLIGHT: FAIL")
    for error in errors:
        print("- " + error)
    raise SystemExit(1)

print("VIEW ACTION OVERRIDE PREFLIGHT: PASS")
print("- XEM Hiển thị exposes Tối ưu đồ họa, Section Box, Cắt theo đối tượng in reference order.")
print("- All three actions are large icon-forward buttons with deterministic command routing.")
print("- Tối ưu đồ họa applies documented graphics preferences and regenerates the viewport.")
print("- Cắt theo đối tượng fails closed on non-finite/overflow/collapsed-padding bounds before native BIM Detail dispatch.")
