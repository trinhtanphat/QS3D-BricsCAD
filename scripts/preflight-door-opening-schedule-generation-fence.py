from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/DoorOpeningSchedule.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/DoorOpeningScheduleGenerationFenceSmoke.cs").read_text(encoding="utf-8")

build_start = source.index("public static IReadOnlyList<DoorOpeningScheduleRow> Build(ProjectState project)")
fence_start = source.index("private static DoorOpeningScheduleSnapshot CaptureProjectRevision", build_start)
build = source[build_start:fence_start]

for token in [
    "var snapshot = CaptureProjectRevision(project);",
    "foreach (var element in snapshot.Elements",
    "ProjectId = snapshot.ProjectId",
    "DrawingFingerprint = snapshot.DrawingFingerprint",
    "CanonicalOptionalHostId(elementsById, hostRaw, element.Id)",
    "ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);",
    "EnsureProjectRevision(project, snapshot);",
]:
    if token not in build:
        raise SystemExit("Missing Door/Opening immutable-generation contract: " + token)

if "foreach (var element in project.Elements" in build:
    raise SystemExit("Door/Opening schedule must aggregate immutable element snapshots, not live project.Elements.")

if build.count("EnsureProjectRevision(project, snapshot);") < 4:
    raise SystemExit("Door/Opening schedule must recheck generation during aggregation and before publication.")

fence = source[fence_start:]
for token in [
    "project.ChangeVersion != snapshot.Version",
    "!SameElements(project.Elements, snapshot.Elements)",
    "!SameFloors(project.Floors, snapshot.Floors)",
    "!SameFamilies(project.Families, snapshot.Families)",
    "CaptureProperties(element.Properties)",
    "element.SourceHandles.ToList().AsReadOnly()",
    "SameSourceHandles(current.SourceHandles, SourceHandles)",
    "hasArea == HasOpeningAreaM2",
    "current.Category == Category",
    '"Project changed while the door/opening schedule was being built; recompute the schedule against the current project state."',
]:
    if token not in fence:
        raise SystemExit("Missing Door/Opening fail-closed semantic evidence: " + token)

for token in [
    "[ModuleInitializer]",
    "StableGenerationRemainsAccepted",
    "DirectElementReplacementIsRejectedWithoutProjectVersionHelp",
    "InPlaceOpeningQuantityMutationIsRejected",
    "InPlaceDoorPropertyMutationIsRejected",
    "InPlaceFloorNameMutationIsRejected",
    "InPlaceFamilyPropertyMutationIsRejected",
    "InPlaceHostCategoryMutationIsRejected",
    "InPlaceProvenanceMutationIsRejected",
    "Project changed while the door/opening schedule was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Door/Opening generation-fence smoke contract: " + token)

print("Door/Opening schedule semantic generation fence preflight passed.")
