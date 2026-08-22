#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

paths = {
    "engine": ROOT / "src/QS3D.Core/Recognition/RecognitionEngine.cs",
    "project": ROOT / "src/QS3D.Core/Recognition/ProjectRecognitionService.cs",
    "snapshot": ROOT / "src/QS3D.Core/Model/EntitySnapshot.cs",
    "reader": ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs",
    "capture": ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "reconcile": ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs",
    "policy": ROOT / "src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs",
    "regen": ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs",
    "review": ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "logic_smoke": ROOT / "tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs",
    "quantity_smoke": ROOT / "tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs",
}
texts = {}
for name, path in paths.items():
    if not path.is_file():
        errors.append("missing B4D recognition/mass file: " + str(path.relative_to(ROOT)))
    else:
        texts[name] = path.read_text(encoding="utf-8")

def require(name, tokens):
    for token in tokens:
        if token not in texts.get(name, ""):
            errors.append(paths[name].name + " missing B4D recognition/mass token: " + token)

require("engine", (
    "public static bool IsEntityTypeCompatible",
    "rule.EntityTypes.Any(x => string.Equals(x, entityType, StringComparison.OrdinalIgnoreCase))",
    "if (!typeMatch) return result;",
))
require("project", (
    "ExactLayerMapping(project, snapshot)",
    "if (mapped != null) return new RecognitionResult(snapshot, new[] { mapped });",
    "if (!TryParseNamedCategory(item.Value, out var category))",
    'RuleId = "project-layer:" + pattern, Category = category, Confidence = 0.99d',
))
project = texts.get("project", "")
exact_start = project.find("private static RecognitionCandidate? ExactLayerMapping")
exact_end = project.find("private static List<KeyValuePair<string, string>> CaptureLayerMappingState", exact_start)
exact_block = project[exact_start:exact_end] if exact_start >= 0 and exact_end > exact_start else ""
if not exact_block:
    errors.append("ProjectRecognitionService ExactLayerMapping block was not found.")
elif "RecognitionEngine.IsEntityTypeCompatible" in exact_block:
    errors.append("Exact project layer mappings must remain authoritative and must not be rejected by fallback entity-type compatibility.")
require("snapshot", ("SurfaceAreaDrawingUnitsSquared", "VolumeDrawingUnitsCubed"))
require("reader", (
    "snapshot.SurfaceAreaDrawingUnitsSquared = area",
    "snapshot.VolumeDrawingUnitsCubed = volume",
))
reader = texts.get("reader", "")
solid_start = reader.find("if (entity is Solid3d solid)")
solid_end = reader.find("private static void PopulateMetadata", solid_start)
solid_block = reader[solid_start:solid_end] if solid_start >= 0 and solid_end > solid_start else ""
if "snapshot.AreaDrawingUnitsSquared = area" in solid_block:
    errors.append("Solid3d total surface area is still being stored as planar AreaDrawingUnitsSquared.")

for name in ("capture", "reconcile"):
    require(name, (
        "MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
        "MeasuredSolidQuantityPolicy.VolumeProperty",
        '"CAD.SolidMetricSource"',
        '"Solid3d.MassProperties"',
        'Properties.Remove("VolumeM3")',
    ))
require("policy", (
    'public const string VolumeProperty = "MeasuredSolidVolumeM3"',
    'public const string SurfaceAreaProperty = "MeasuredSolidSurfaceAreaM2"',
    'element.SetQuantity("GrossVolumeM3", volume)',
    'element.SetQuantity("NetVolumeM3", volume)',
    'element.SetQuantity("MeasuredSurfaceAreaM2", surfaceArea)',
    "case ElementCategory.WallFinish:",
))
require("regen", ("MeasuredSolidQuantityPolicy.Apply(element)",))
require("review", (
    "if (!ExistingProjectMutationContext.TryGet(doc, out var currentProject))",
    "SemanticCaptureService.CaptureSnapshot(doc, refreshed.Snapshot, candidate.Category)",
    "var captured = SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(currentProject, result.Handle)",
    'AuditTrail.ForProject(currentProject).Record("recognition.apply", captured.Id',
))
require("logic_smoke", (
    "RecognitionKeepsFallbackTypeGateWithAuthoritativeProjectMapping",
    'new EntitySnapshot("TXT", "DBText", "A-WALL")',
    "fallback.TopCandidate == null",
    "mapped.TopCandidate != null",
    "Equal(ElementCategory.ArchitecturalWall, mapped.TopCandidate!.Category)",
))
require("quantity_smoke", (
    "MeasuredSolidMassOverridesDefaultPrismVolume",
    "MeasuredWallFinishSolidPreservesVolumeAndSurface",
    "MeasuredSolidQuantityPolicy.VolumeProperty",
    "MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
    'slab.Quantities["NetVolumeM3"] - 1.75d',
))

print("QS3D B4D recognition and Solid3d mass preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: B4D re-resolves and canonically binds current project/source ownership while fallback recognition keeps entity-type gates and exact project layer mappings remain authoritative, with native Solid3d mass provenance preserved.")
