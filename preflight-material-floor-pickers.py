#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs",
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs",
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing material/floor picker file: " + relative)

checks = {
    "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs": [
        'MetadataKey = "QS3D.MaterialCatalog.v1"',
        "MaxCustomMaterials = 500",
        '"Bê tông"', '"Thép"', '"Gạch"', '"Kính"', '"Nhôm"',
        "UpsertCustom",
        "DeleteCustom",
        "ReferencedMaterialNames",
        "RenameReferences",
        "inheritedMaterialFamilies",
        "inheritedFrameFamilies",
        "RenameElementReference",
        "element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity)",
        "is still referenced by a Family or Instance and cannot be deleted",
        "Convert.ToBase64String",
        "Convert.FromBase64String",
        "Duplicate material id",
        "Duplicate material name",
    ],
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs": [
        "SelectImplied",
        "SourceHandles",
        '"GeneratedSolidHandle"',
        '"GeneratedRebarHandles"',
        '"GeneratedSlabMeshHandles"',
        '"GeneratedWallMeshHandles"',
        '"GeneratedCurtainFrameHandles"',
        "BuildOwnershipIndex",
        "ambiguously owned by semantic elements",
        "Resolve project ownership before bulk property edits",
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.MaterialCatalogWindow"',
        'x:Name="MaterialList"',
        'x:Name="TargetCombo"',
        'Tag="Material"',
        'Tag="CurtainFrameMaterial"',
        'Click="OnSaveClick"',
        'Click="OnDeleteClick"',
        'Click="OnApplyClick"',
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": [
<<<<<<< HEAD
        "_document = document",
        "ReferenceEquals",
=======
        "private readonly Document _document",
        "MaterialCatalogWindow(Document document)",
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        "SemanticSelectionResolver.ResolveImplied(_document, project)",
>>>>>>> origin/main
        "ProjectMaterialCatalog.UpsertCustom",
        "ProjectMaterialCatalog.DeleteCustom",
        'element.SetProperty(target, material.Name)',
        '"CurtainFrameMaterial"',
        "ElementCategory.GlassWall",
        'AuditTrail.ForProject(project).Record("material.assign"',
        'AuditTrail.ForProject(project).Record("material.catalog.upsert"',
    ],
    "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs": [
        'CommandMethod("QS3DMATERIALS"',
        "new MaterialCatalogWindow(document)",
        "ShowModelessWindow",
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.FloorLevelWindow"',
        'x:Name="FloorList"',
        'x:Name="ActiveFloorText"',
        'Click="OnActivateClick"',
        'Click="OnAssignClick"',
        "KHÔNG tự Move/Translate source CAD",
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": [
        "private readonly Document _document",
        "FloorLevelWindow(Document document)",
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "EnsureBoundDrawingIsActive",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        "SemanticSelectionResolver.ResolveImplied(_document, project)",
        "project.ActiveFloorId = floor.Id",
        "element.FloorId = floor.Id",
        "element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)",
        'AuditTrail.ForProject(project).Record("floor.assign"',
        "CAD source không bị Move",
    ],
    "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs": [
        'CommandMethod("QS3DLEVELS"',
        "new FloorLevelWindow(document)",
        "ShowModelessWindow",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs": [
        "CustomRoundTripAndUpdate",
        "ReferencedMaterialsAreDiscovered",
        "RenamePropagatesReferencesAndStaleState",
        "RenameStalesInheritedConsumersButPreservesOverrides",
        "ReferencedMaterialCannotBeDeleted",
        "RejectsDuplicateBuiltInAndCorruptStorage",
        "inherited.IsGeneratedSolidStale()",
        "overridden.IsGeneratedSolidStale()",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs": [
        "ProjectMaterialCatalogSmoke.Run();",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing material/floor guard/token: " + needle)

# Modeless project editors must be document-bound rather than resolving MDI document on every click.
for relative in (
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
):
    path = ROOT / relative
    if path.is_file():
        text = path.read_text(encoding="utf-8")
        if "var document = Application.DocumentManager.MdiActiveDocument" in text:
            errors.append(relative + " must not switch project ownership through MdiActiveDocument inside modeless event handlers")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DMATERIALS", "QS3DLEVELS"):
    if commands.count(command) != 1:
        errors.append(command + " must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: persisted material catalog, inherited/reference-safe rename+delete, ownership-safe semantic selection, and document-bound material/floor modeless pickers are present.")
