from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ProjectQuantityReportPersistedGeometrySnapshotSmoke.cs").read_text(encoding="utf-8")
start = source.index("internal static ElementSnapshot Capture(")
end = source.index("private sealed class FloorSnapshot", start)
capture = source[start:end]
for token in [
    "clone.Properties as ProjectElementPropertyDictionary",
    'throw new InvalidOperationException("Quantity snapshot requires the canonical element property store.")',
    "foreach (var property in source.Properties) properties.SetPersistenceValue(property.Key, property.Value);",
    "source,\n                    clone,",
]:
    if token not in capture:
        raise SystemExit("Missing persistence-only quantity snapshot contract: " + token)
for forbidden in ["clone.Properties.Add(", "clone.SetProperty(", "source.SetProperty(", "source.MarkClean("]:
    if forbidden in capture:
        raise SystemExit("Quantity snapshot must not perform semantic property edits: " + forbidden)
for token in [
    "!SameElements(project, snapshot.Elements)",
    "!ReferenceEquals(current, frozen.SourceInstance)",
    "SameDictionary(current.Properties, frozen.Properties, StringComparer.Ordinal)",
]:
    if token not in source:
        raise SystemExit("Missing genuine generation-drift refusal: " + token)
for token in [
    "[ModuleInitializer]",
    "store.Save(warm, path);",
    "store.Load(path);",
    'keys.IndexOf("GeneratedSolidHandle") < keys.IndexOf("LengthM")',
    "ProjectStateSnapshot.CreateDetachedCopy(cold)",
    "RegenerateDirty(preview)",
    "ProjectQuantityReportBuilder.Group(project)",
    "ProjectQuantityReportBuilder.Detail(project)",
    "Describe(preview) == previewBefore",
    "Describe(cold) == before",
    "savedBytes.SequenceEqual(File.ReadAllBytes(path))",
    'current.Elements[0].SetProperty("LengthM", "5")',
    'project.Elements[0].Properties[ProjectElement.GeneratedSolidStateKey] == "stale"',
    "RejectsActualPostCaptureDrift(ProjectStateSnapshot.CreateDetachedCopy(cold), false)",
    "RejectsActualPostCaptureDrift(ProjectStateSnapshot.CreateDetachedCopy(cold), true)",
]:
    if token not in smoke:
        raise SystemExit("Missing executable persisted quantity snapshot regression: " + token)
print("PASS quantity report persisted generated-geometry snapshot contract")
