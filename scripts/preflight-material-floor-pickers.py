#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs",
    "src/QS3D.Core/Domain/ProjectFloorService.cs",
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs",
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs",
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs",
    "tests/QS3D.Core.SmokeTests/ProjectFloorServiceSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectFloorServiceRegistration.cs",
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing material/floor picker file: " + relative)

checks = {
    "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs": [
        'MetadataKey = "QS3D.MaterialCatalog.v1"', "MaxCustomMaterials = 500",
        '"Bê tông"', '"Thép"', '"Gạch"', '"Kính"', '"Nhôm"',
        "UpsertCustom", "DeleteCustom", "ReferencedMaterialNames", "RenameReferences",
        "inheritedMaterialFamilies", "inheritedFrameFamilies", "RenameElementReference",
        "element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity)",
        "is still referenced by a Family or Instance and cannot be deleted",
        "Convert.ToBase64String", "Convert.FromBase64String", "Duplicate material id", "Duplicate material name",
    ],
    "src/QS3D.Core/Domain/ProjectFloorService.cs": [
        "MaxFloors = 2000", "Create(ProjectState project", "Update(ProjectState project", "SetActive(ProjectState project",
        "Assign(ProjectState project", "Delete(ProjectState project", "ReferenceCount(ProjectState project", "EnsureUniqueName",
        "ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity", "flags |= ElementDirtyFlags.Geometry",
        "Cannot delete the active floor", "Reassign or clear Floor/Level references before deletion", "Value must be finite",
        "ReferenceEquals(owned, element)", "Element does not belong to the project instance", "Project contains duplicate semantic element id",
    ],
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs": [
        "selected.Contains(handle)", "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "ambiguously owned by semantic elements", "Resolve project semantic ownership before continuing",
    ],
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs": [
        "SelectImplied", "StartOpenCloseTransaction", "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)",
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.MaterialCatalogWindow"', 'x:Name="MaterialList"', 'x:Name="TargetCombo"',
        'Tag="Material"', 'Tag="CurtainFrameMaterial"', 'Click="OnSaveClick"', 'Click="OnDeleteClick"', 'Click="OnApplyClick"',
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": [
        "private readonly Document _document", "MaterialCatalogWindow(Document document)", "_document = document",
        "DocumentBoundWindowLifetime.Attach(this, _document)",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "ReferenceEquals(",
        "SemanticSelectionResolver.ResolveImplied(_document, project)", "ProjectMaterialCatalog.UpsertCustom", "ProjectMaterialCatalog.DeleteCustom",
        'element.SetProperty(target, material.Name)', '"CurtainFrameMaterial"', "ElementCategory.GlassWall",
        'AuditTrail.ForProject(project).Record("material.assign"', 'AuditTrail.ForProject(project).Record("material.catalog.upsert"',
    ],
    "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs": [
        'CommandMethod("QS3DMATERIALS"', "ExistingProjectMutationContext.TryGet(document, out var project)",
        "new MaterialCatalogWindow(document, project)", "ShowModelessWindow",
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.FloorLevelWindow"', 'x:Name="FloorList"', 'x:Name="ActiveFloorText"',
        'x:Name="FloorNameBox"', 'x:Name="FloorElevationBox"', 'x:Name="ReferenceCountText"',
        'Click="OnNewFloorClick"', 'Click="OnSaveFloorClick"', 'Click="OnDeleteFloorClick"',
        'Click="OnActivateClick"', 'Click="OnAssignClick"', "KHÔNG tự Move/Translate source CAD",
        "Không thể xóa tầng active hoặc tầng còn semantic element tham chiếu",
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": [
        "private readonly Document _document", "FloorLevelWindow(Document document)", "DocumentBoundWindowLifetime.Attach(this, _document)",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "ExistingProjectMutationContext.Require(_document",
        "EnsureBoundDrawingIsActive", "ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document)",
        "ProjectFloorService.Create", "ProjectFloorService.Update", "ProjectFloorService.Delete",
        "ProjectFloorService.SetActive", "ProjectFloorService.Assign", "ProjectFloorService.ReferenceCount",
        "SemanticSelectionResolver.ResolveImplied(_document, project)",
        'AuditTrail.ForProject(project).Record("floor.create"', 'AuditTrail.ForProject(project).Record("floor.update"',
        'AuditTrail.ForProject(project).Record("floor.delete"', 'AuditTrail.ForProject(project).Record("floor.assign"',
        "CAD source không bị Move", "ParseElevation",
    ],
    "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs": [
        'CommandMethod("QS3DLEVELS"', "new FloorLevelWindow(document)", "ShowModelessWindow",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs": [
        "CustomRoundTripAndUpdate", "ReferencedMaterialsAreDiscovered", "RenamePropagatesReferencesAndStaleState",
        "RenameStalesInheritedConsumersButPreservesOverrides", "ReferencedMaterialCannotBeDeleted",
        "RejectsDuplicateBuiltInAndCorruptStorage", "inherited.IsGeneratedSolidStale()", "overridden.IsGeneratedSolidStale()",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs": ["ProjectMaterialCatalogSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/ProjectFloorServiceSmoke.cs": [
        "CreateUpdateAssignAndDelete", "ElevationChangeMarksGeneratedGeometryStale", "DeleteGuardsActiveAndReferencedFloors",
        "RejectsDuplicateNamesAndInvalidElevation", "RejectsDetachedSameIdElements", "ProjectFloorService.Assign", "IsGeneratedSolidStale()",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectFloorServiceRegistration.cs": ["ProjectFloorServiceSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs": [
        "ModuleInitializer", "UnrelatedAmbiguityDoesNotBlockCleanSelection", "SelectedAmbiguityIsRejected", "GeneratedMultiHandleResolvesOwner",
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

resolver = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs"
if resolver.is_file():
    text = resolver.read_text(encoding="utf-8")
    for obsolete in ("BuildOwnershipIndex(project)", "private static readonly string[] SingleHandleKeys"):
        if obsolete in text:
            errors.append("SemanticSelectionResolver still contains obsolete whole-project ownership logic: " + obsolete)

material_command = ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs"
if material_command.is_file():
    text = material_command.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("opening Material Catalog must not create/cache project state")

material_window = ROOT / "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs"
if material_window.is_file():
    text = material_window.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Material Catalog modeless callbacks must not create/cache replacement project state")

material_command = ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs"
if material_command.is_file():
    text = material_command.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("opening Material Catalog must not create/cache mutable project state")

floor_window = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs"
if floor_window.is_file():
    text = floor_window.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Floor/Level modeless callbacks must not create/cache replacement project state; reads use TryGetReadOnly and writes bind ExistingProjectMutationContext")

for relative in (
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
):
    path = ROOT / relative
    if path.is_file() and "var document = Application.DocumentManager.MdiActiveDocument" in path.read_text(encoding="utf-8"):
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
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: persisted material catalog, non-creating canonical Material launcher, selection-scoped ownership, constructor-bound modeless windows, read-only refresh paths, canonical existing-project Floor mutations, and Core-backed floor CRUD/active/assignment semantics are present.")
