from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectQuantityGenerationFenceSmoke.cs").read_text(encoding="utf-8")

build_start = source.index("private static IReadOnlyList<QuantityReportRow> Build")
snapshot_start = source.index("private sealed class ProjectQuantityGenerationSnapshot", build_start)
build = source[build_start:snapshot_start]

for token in [
    "var snapshot = ProjectQuantityGenerationSnapshot.Capture(project);",
    "foreach (var elementSnapshot in elements)",
    "var element = elementSnapshot.Element;",
    "if (elementSnapshot.ExcludedFromQuantity) continue;",
    "AddHandles(row.SourceHandles, elementSnapshot.ResolvedSourceHandles);",
    "EnsureProjectRevision(project, snapshot);",
]:
    if token not in build:
        raise SystemExit("Missing Project Quantity immutable-generation contract: " + token)

if "SourceHandleResolver.Resolve(project, new[] { elementId })" in build:
    raise SystemExit("Project Quantity aggregation must publish frozen provenance, not resolve handles from live project state.")
if build.count("EnsureProjectRevision(project, snapshot);") < 4:
    raise SystemExit("Project Quantity must revalidate semantic generation during aggregation and before publication.")

fence_end = source.index("private static HashSet<string>? ResolveSelection", snapshot_start)
fence = source[snapshot_start:fence_end]
for token in [
    "project.ChangeVersion != snapshot.Version",
    "project.ProjectId",
    "project.DrawingFingerprint",
    "!SameElements(project, snapshot.Elements)",
    "!SameFloors(project.Floors, snapshot.Floors)",
    "!SameZones(project.Zones, snapshot.Zones)",
    "!SameFamilies(project.Families, snapshot.Families)",
    "foreach (var handle in source.SourceHandles) clone.SourceHandles.Add(handle);",
    "foreach (var dependency in source.DependsOn) clone.DependsOn.Add(dependency);",
    "foreach (var property in source.Properties) clone.Properties.Add(property.Key, property.Value);",
    "foreach (var quantity in source.Quantities) clone.Quantities.Add(quantity.Key, quantity.Value);",
    "SourceHandleResolver.Resolve(project, new[] { source.Id }).ToList().AsReadOnly()",
    "SameQuantityDictionary(current.Quantities, frozen.Quantities)",
    '"Project changed while the quantity report was being built; recompute the report against the current project state."',
]:
    if token not in fence:
        raise SystemExit("Missing Project Quantity fail-closed semantic evidence: " + token)

for token in [
    "[ModuleInitializer]",
    "StableGenerationPublishesFrozenValues",
    "RejectsInPlaceQuantityMutationWithoutProjectVersionChange",
    "RejectsInPlaceFamilyNameMutationWithoutProjectVersionChange",
    "RejectsInPlaceSourceHandleMutationWithoutProjectVersionChange",
    'Quantities["GrossConcreteM3"] = 9d',
    'field.SetValue(p.Families[0], "Wall Type Drifted")',
    'SourceHandles.Add("BEEF")',
    "Project changed while the quantity report was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Project Quantity generation-fence smoke contract: " + token)

print("Project Quantity semantic generation fence preflight passed.")
