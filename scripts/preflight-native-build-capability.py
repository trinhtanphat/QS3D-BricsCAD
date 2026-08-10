#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

capability = ROOT / "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs"
build = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
structural = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"

for path in (capability, build, workspace, structural):
    if not path.is_file(): errors.append("missing native-build capability file: " + str(path.relative_to(ROOT)))

if capability.is_file():
    text = capability.read_text(encoding="utf-8")
    for needle in (
        "internal static class NativeBuildCapability",
        "public static bool Supports(ElementCategory category)",
        "IsWallCategory(category) || StructuralSolidBuilder.Supports(category)",
        "ElementCategory.ArchitecturalWall",
        "ElementCategory.GlassWall",
        "ElementCategory.WallPier",
        "UnsupportedMessage",
    ):
        if needle not in text: errors.append("NativeBuildCapability missing contract: " + needle)

if build.is_file():
    text = build.read_text(encoding="utf-8")
    for needle in (
        "!NativeBuildCapability.Supports(x.Category)",
        "NativeBuildCapability.IsWallCategory(category)",
        "StructuralSolidBuilder.Supports(category)",
        "ValidateWallSourceBatch",
        "AreAllModelSpaceEntities",
    ):
        if needle not in text: errors.append("Build3D must consume shared capability while retaining deep guards: " + needle)
    if "private static bool IsNativeBuildCategory" in text or "private static bool IsWallCategory" in text:
        errors.append("Build3D must not retain a duplicate native-category capability list")

if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    for needle in (
        "Cad.NativeBuildCapability.Supports(category)",
        "Cad.NativeBuildCapability.UnsupportedMessage(category)",
        "Cad.NativeBuildCapability.Supports(category.Value)",
        "Cad.NativeBuildCapability.UnsupportedMessage(category.Value)",
        'Send("QS3DBUILD3D")',
    ):
        if needle not in text: errors.append("Workspace build compatibility messaging missing: " + needle)
    guard = text.find("if (!Cad.NativeBuildCapability.Supports(category.Value))")
    send = text.find('Send("QS3DBUILD3D")', guard)
    if guard < 0 or send < guard:
        errors.append("Workspace must reject unsupported native category before dispatching QS3DBUILD3D")

if structural.is_file():
    text = structural.read_text(encoding="utf-8")
    if "public static bool Supports(ElementCategory category)" not in text:
        errors.append("StructuralSolidBuilder.Supports remains the structural capability source")

print("QS3D native-build capability preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Workspace and QS3DBUILD3D share one native-category capability while command-level source/Model-Space/atomic guards remain authoritative.")
