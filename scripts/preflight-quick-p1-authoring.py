#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-P1-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, HUB, DOC):
    if not path.is_file():
        errors.append("missing quick-P1 dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    primary = ("QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER", "QS3DDRAWSTRUCTWALL", "QS3DDRAWFOUNDATION")
    advanced = tuple(name + "ADV" for name in primary)
    for name in primary + advanced:
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    order = [
        "QS3DDRAWGLASSWALL", "QS3DDRAWGLASSWALLADV",
        "QS3DDRAWWALLPIER", "QS3DDRAWWALLPIERADV",
        "QS3DDRAWSTRUCTWALL", "QS3DDRAWSTRUCTWALLADV",
        "QS3DDRAWFOUNDATION", "QS3DDRAWFOUNDATIONADV",
    ]
    positions = [text.find('[CommandMethod("' + name + '", CommandFlags.Modal)]') for name in order]
    if any(pos < 0 for pos in positions) or positions != sorted(positions):
        errors.append("quick/advanced P1 command ordering is missing or invalid")

    def command_slice(name, next_name=None):
        start = text.find('[CommandMethod("' + name + '", CommandFlags.Modal)]')
        if start < 0:
            return ""
        if next_name:
            end = text.find('[CommandMethod("' + next_name + '", CommandFlags.Modal)]', start + 1)
        else:
            end = text.find("private static void Execute(", start + 1)
        return text[start:end] if end > start else ""

    quick_specs = {
        "QS3DDRAWGLASSWALL": (
            "QS3DDRAWGLASSWALLADV",
            'AcquirePath(document, "Vách Kính nhanh", 2, false)',
            'FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "ThicknessM", 0.012d)',
            'FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "HeightM", 3.6d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.GlassWall, "BottomOffsetM", 0d)',
        ),
        "QS3DDRAWWALLPIER": (
            "QS3DDRAWWALLPIERADV",
            'AcquireFixedPath(document, "Trụ Tường nhanh", 2)',
            'FamilyNumber(defaultsProject!, ElementCategory.WallPier, "ThicknessM", 0.2d)',
            'FamilyNumber(defaultsProject!, ElementCategory.WallPier, "HeightM", 3.6d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.WallPier, "BottomOffsetM", 0d)',
        ),
        "QS3DDRAWSTRUCTWALL": (
            "QS3DDRAWSTRUCTWALLADV",
            'AcquireFixedPath(document, "Vách BTCT nhanh", 2)',
            'FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "ThicknessM", 0.2d)',
            'FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "HeightM", 3.6d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.StructuralWall, "BottomOffsetM", 0d)',
        ),
        "QS3DDRAWFOUNDATION": (
            "QS3DDRAWFOUNDATIONADV",
            'AcquirePath(document, "Móng nhanh", 3, true)',
            'FamilyNumber(defaultsProject!, ElementCategory.Foundation, "ThicknessM", 0.5d)',
            'FamilyFiniteNumber(defaultsProject!, ElementCategory.Foundation, "BottomOffsetM", 0d)',
        ),
    }
    for name, requirements in quick_specs.items():
        body = command_slice(name, requirements[0])
        for token in requirements[1:]:
            if token not in body:
                errors.append(name + " quick path missing: " + token)
        for forbidden in ("PromptPositiveMeters(", "PromptFiniteMeters("):
            if forbidden in body:
                errors.append(name + " quick path must not require numeric prompts: " + forbidden)
        if "Execute(" not in body or "element.SetProperty(" not in body:
            errors.append(name + " quick path must reuse canonical Execute/SetProperty lifecycle")

    advanced_specs = {
        "QS3DDRAWGLASSWALLADV": ("QS3DDRAWWALLPIER", "Bề dày Vách Kính", "Chiều cao Vách Kính", "Offset đáy Vách Kính"),
        "QS3DDRAWWALLPIERADV": ("QS3DDRAWSTRUCTWALL", "Bề dày Trụ Tường", "Chiều cao Trụ Tường", "Offset đáy Trụ Tường"),
        "QS3DDRAWSTRUCTWALLADV": ("QS3DDRAWFOUNDATION", "Bề dày Vách BTCT", "Chiều cao Vách BTCT", "Offset đáy Vách BTCT"),
        "QS3DDRAWFOUNDATIONADV": (None, "Bề dày Móng", "Offset đáy Móng"),
    }
    for name, spec in advanced_specs.items():
        body = command_slice(name, spec[0])
        if "PromptPositiveMeters(" not in body or "PromptFiniteMeters(" not in body:
            errors.append(name + " must preserve explicit numeric customization prompts")
        for label in spec[1:]:
            if label not in body:
                errors.append(name + " missing customization label: " + label)
        if "Execute(" not in body:
            errors.append(name + " must reuse canonical Execute lifecycle")

    wallpier_quick = command_slice("QS3DDRAWWALLPIER", "QS3DDRAWWALLPIERADV")
    wallpier_adv = command_slice("QS3DDRAWWALLPIERADV", "QS3DDRAWSTRUCTWALL")
    for body, label in ((wallpier_quick, "quick WallPier"), (wallpier_adv, "advanced WallPier")):
        if "CreateLine(document, points[0], points[1])" not in body or "CreatePolyline(document, points, false)" in body:
            errors.append(label + " must remain specialized two-point LINE-only")

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    for token in (
        'new RibbonButtonSpec("Vẽ Vách Kính", "QS3DDRAWGLASSWALL")',
        'new RibbonButtonSpec("Vẽ Trụ Tường", "QS3DDRAWWALLPIER")',
        'new RibbonButtonSpec("Vẽ Vách BTCT", "QS3DDRAWSTRUCTWALL")',
        'new RibbonButtonSpec("Vẽ Móng", "QS3DDRAWFOUNDATION")',
    ):
        if token not in text:
            errors.append("primary quick P1 Ribbon wiring missing: " + token)

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for command in ("QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER", "QS3DDRAWSTRUCTWALL", "QS3DDRAWFOUNDATION"):
        if 'Tag="' + command + '"' not in text:
            errors.append("Domain Hub missing primary quick P1 command: " + command)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWGLASSWALLADV",
        "QS3DDRAWWALLPIERADV",
        "QS3DDRAWSTRUCTWALLADV",
        "QS3DDRAWFOUNDATIONADV",
        "LOCAL-008",
        "QS3DBUILD3D",
        "Family / Type",
    ):
        if token not in text:
            errors.append("quick-P1 documentation missing: " + token)

if errors:
    print("Quick P1 authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick P1 authoring preflight PASS")
