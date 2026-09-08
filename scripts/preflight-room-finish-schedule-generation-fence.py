from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/RoomFinishSchedule.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RoomFinishScheduleGenerationFenceSmoke.cs").read_text(encoding="utf-8")

for token in [
    "var snapshot = CaptureProjectRevision(project);",
    "foreach (var item in snapshot.WorkItems)",
    "EnsureProjectRevision(project, snapshot);",
    "project.Elements.ToList().AsReadOnly()",
    "project.Floors.ToList().AsReadOnly()",
    "project.Families.ToList().AsReadOnly()",
    "!SameInstances(project.Elements, snapshot.SourceElements)",
    "!SameInstances(project.Floors, snapshot.SourceFloors)",
    "!SameInstances(project.Families, snapshot.SourceFamilies)",
    "ReferenceEquals(current[index], source[index])",
    "!SameElements(project.Elements, snapshot.Elements)",
    "!SameFloors(project.Floors, snapshot.Floors)",
    "!SameFamilies(project.Families, snapshot.Families)",
    "!SameMaterialUnits(project, snapshot.MaterialUnits)",
    "internal IReadOnlyList<RoomFinishWorkItemSnapshot> WorkItems { get; }",
    "Project changed while the room finish schedule was being built",
]:
    if token not in source:
        raise SystemExit("Missing Room finish generation fence contract: " + token)

for token in [
    "[ModuleInitializer]",
    "EquivalentFinishReplacementWithoutTouchFailsClosed",
    "DirectSemanticMutationWithoutTouchFailsClosed",
    "project.Elements[1] = replacement",
    'project.Elements[1].Quantities["BottomAreaM2"] = 13.5d',
    "project.ChangeVersion != version",
    '"CaptureProjectRevision"',
    '"EnsureProjectRevision"',
    "Project changed while the room finish schedule was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Room finish generation fence smoke contract: " + token)

print("Room finish schedule generation fence preflight passed.")
