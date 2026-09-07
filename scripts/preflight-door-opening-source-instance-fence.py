from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Reporting/DoorOpeningSchedule.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/DoorOpeningSourceInstanceFenceSmoke.cs").read_text(encoding="utf-8")

for token in [
    "project.Elements.ToList().AsReadOnly()",
    "!SameElementInstances(project.Elements, snapshot.SourceElements)",
    "private static bool SameElementInstances",
    "ReferenceEquals(current[index], sourceElements[index])",
    "internal IReadOnlyList<ProjectElement> SourceElements { get; }",
    "SourceElements = sourceElements",
    "!SameElements(project.Elements, snapshot.Elements)",
]:
    if token not in source:
        raise SystemExit("Missing Door/opening dual generation fence contract: " + token)

for token in [
    "[ModuleInitializer]",
    "EquivalentElementReplacementWithoutTouchFailsClosed",
    "project.Elements[0] = replacement",
    "project.ChangeVersion != version",
    '"CaptureProjectRevision"',
    '"EnsureProjectRevision"',
    "Project changed while the door/opening schedule was being built",
]:
    if token not in smoke:
        raise SystemExit("Missing deterministic Door/opening source-instance fence smoke contract: " + token)

print("Door/opening source-instance generation fence preflight passed.")
