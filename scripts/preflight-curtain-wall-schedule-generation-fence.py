from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/CurtainWallSchedule.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/CurtainWallScheduleGenerationFenceSmoke.cs").read_text(encoding="utf-8")

build_start = source.index("public static IReadOnlyList<CurtainWallScheduleRow> Build(ProjectState project)")
fence_start = source.index("private static CurtainScheduleSnapshot CaptureProjectRevision", build_start)
build = source[build_start:fence_start]

for token in [
    "var snapshot = CaptureProjectRevision(project);",
    "foreach (var element in snapshot.Elements.Where",
    "ProjectId = snapshot.ProjectId",
    "DrawingFingerprint = snapshot.DrawingFingerprint",
    "ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);",
    "EnsureProjectRevision(project, snapshot);",
]:
    if token not in build:
        raise SystemExit("Missing Curtain Wall schedule immutable-generation contract: " + token)

if "project.Elements.Where" in build:
    raise SystemExit("Curtain Wall schedule must aggregate the immutable element snapshot, not live project.Elements.")

if build.count("EnsureProjectRevision(project, snapshot);") < 4:
    raise SystemExit("Curtain Wall schedule must recheck the semantic generation during iteration and before publication.")

fence_end = source.index("private sealed class CurtainWallAggregateState", fence_start)
fence = source[fence_start:fence_end]
for token in [
    "project.ChangeVersion != snapshot.Version",
    "project.ProjectId",
    "project.DrawingFingerprint",
    "!SameElements(project.Elements, snapshot.Elements)",
    "!SameFloors(project.Floors, snapshot.Floors)",
    "!SameFamilies(project.Families, snapshot.Families)",
    "element.SourceHandles.ToList().AsReadOnly()",
    "SameSourceHandles(current.SourceHandles, SourceHandles)",
    "SameQuantity(current, \"LengthM\", LengthM)",
    "string.Equals(current.Name, Name, StringComparison.Ordinal)",
    '"Project changed while the curtain wall schedule was being built; recompute the schedule against the current project state."',
]:
    if token not in fence:
        raise SystemExit("Missing Curtain Wall schedule fail-closed semantic evidence: " + token)

for token in [
    "[ModuleInitializer]",
    "StableGenerationRemainsAccepted",
    "DirectElementReplacementIsRejectedWithoutProjectVersionHelp",
    "InPlaceCurtainQuantityMutationIsRejected",
    "InPlaceFloorNameMutationIsRejected",
    "InPlaceFamilyNameMutationIsRejected",
    "InPlaceProvenanceMutationIsRejected",
    "SetQuantity(\"LengthM\", 4d)",
    "project.Floors[0].Name = \"Changed floor\"",
    "project.Families[0].Name = \"Changed family\"",
    "wall.SourceHandles[0] = \"BB02\"",
    "Project changed while the curtain wall schedule was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Curtain Wall semantic generation-fence smoke contract: " + token)

print("Curtain Wall schedule semantic generation fence preflight passed.")