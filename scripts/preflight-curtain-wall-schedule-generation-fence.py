from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/CurtainWallSchedule.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/CurtainWallScheduleGenerationFenceSmoke.cs").read_text(encoding="utf-8")

build_start = source.index("public static IReadOnlyList<CurtainWallScheduleRow> Build(ProjectState project)")
fence_start = source.index("private static void EnsureProjectRevision", build_start)
build = source[build_start:fence_start]

for token in [
    "var reportVersion = project.ChangeVersion;",
    "var elementInstances = project.Elements.ToList();",
    "var elementUpdatedUtc = elementInstances.Select(x => x.UpdatedUtc).ToList();",
    "var floorInstances = project.Floors.ToList();",
    "var familyInstances = project.Families.ToList();",
    "var drawingFingerprint = project.DrawingFingerprint;",
    "foreach (var element in elementInstances.Where",
    "DrawingFingerprint = drawingFingerprint",
    "EnsureProjectRevision(project, reportVersion, elementInstances, elementUpdatedUtc, floorInstances, familyInstances, drawingFingerprint);",
]:
    if token not in build:
        raise SystemExit("Missing Curtain Wall schedule frozen-generation contract: " + token)

if "project.Elements.Where" in build:
    raise SystemExit("Curtain Wall schedule must enumerate the frozen element snapshot, not live project.Elements.")

if build.count("EnsureProjectRevision(project, reportVersion, elementInstances, elementUpdatedUtc, floorInstances, familyInstances, drawingFingerprint);") < 4:
    raise SystemExit("Curtain Wall schedule must recheck the frozen generation during iteration and before publication.")

fence_end = source.index("private static void RequireClearPanelEnvelope", fence_start)
fence = source[fence_start:fence_end]
for token in [
    "project.ChangeVersion != expectedVersion",
    "project.DrawingFingerprint",
    "!SameInstances(project.Elements, elements)",
    "!SameElementRevisions(elements, elementUpdatedUtc)",
    "!SameInstances(project.Floors, floors)",
    "!SameInstances(project.Families, families)",
    "elements[index].UpdatedUtc != updatedUtc[index]",
    '"Project changed while the curtain wall schedule was being built; recompute the schedule against the current project state."',
]:
    if token not in fence:
        raise SystemExit("Missing Curtain Wall schedule fail-closed generation evidence: " + token)

for token in [
    "[ModuleInitializer]",
    "StableGenerationRemainsAccepted",
    "DirectElementReplacementIsRejectedWithoutProjectVersionHelp",
    "InPlaceCurtainQuantityMutationIsRejected",
    "CatalogGenerationMutationIsRejected",
    "SetQuantity(\"LengthM\", 4d)",
    "Project changed while the curtain wall schedule was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Curtain Wall generation-fence smoke contract: " + token)

print("Curtain Wall schedule generation fence preflight passed.")