#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
paths = {
    "source": ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs",
    "xaml": ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml",
    "bootstrap": ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveBootstrap.cs",
    "handler": ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveHandler.cs",
}
errors = []
for label, path in paths.items():
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))
if errors:
    print("Floor/Level stale-project preflight FAILED:")
    for error in errors: print("- " + error)
    sys.exit(1)

source = paths["source"].read_text(encoding="utf-8")
xaml = paths["xaml"].read_text(encoding="utf-8")
bootstrap = paths["bootstrap"].read_text(encoding="utf-8")
handler = paths["handler"].read_text(encoding="utf-8")
creation = "ProjectContextCoordinator.GetOrCreate(_document)"

def need(text, token, label):
    if token not in text: errors.append(label + " missing: " + token)

for token in (
    "private ProjectState? _boundProject;",
    "private ProjectState RequireBoundProjectForRead(string operation)",
    "private ProjectState RequireBoundProjectForMutation(string operation, string mutationContext)",
    "!ReferenceEquals(currentProject, _boundProject)",
    "var project = ExistingProjectMutationContext.Require(_document, mutationContext);",
    'RequireBoundProjectForMutation("xóa tầng", "Xóa Floor/Level")',
    'RequireBoundProjectForMutation("đặt tầng hoạt động", "Đặt Floor/Level active")',
    'var previewProject = RequireBoundProjectForRead("gán tầng cho selection");',
): need(source, token, "source")

need(xaml, 'Content="Lưu" Style="{StaticResource AccentButton}" Click="OnSaveFloorFirstBootstrapClick"', "xaml")
if 'Click="OnSaveFloorClick"' in xaml: errors.append("xaml still routes Save to legacy handler")
if creation in source or creation in handler: errors.append("project creation escaped focused bootstrap helper")
if bootstrap.count(creation) != 1: errors.append("bootstrap helper must contain exactly one canonical creation call")
for token in (
    "if (!creatingNewFloor || _boundProject != null)",
    'return RequireBoundProjectForMutation("lưu tầng", "Lưu Floor/Level");',
    'EnsureBoundDrawingIsActive("lưu tầng");',
    "ProjectContextCoordinator.TryGetReadOnly(_document, out _)",
    creation,
    "bootstrappedProject = true;",
): need(bootstrap, token, "bootstrap")
for token in (
    "ValidateFirstSaveFloorDraft(name);",
    "var elevation = ParseElevation(FloorElevationBox.Text);",
    "RequireProjectForFirstSave(creatingNewFloor, out var bootstrappedProject)",
    "ProjectStateSnapshot.Capture(project)",
    "RestoreOrThrow(project, rollback, operationError, \"Lưu Floor/Level\")",
    "ProjectContextCoordinator.Forget(_document);",
): need(handler, token, "handler")

inspect_start = source.find("private void OnInspectSelectionClick")
refresh_start = source.find("private void RefreshAll", inspect_start)
refresh_end = source.find("private FloorDefinition RequireSelectedFloor", refresh_start)
if inspect_start < 0 or refresh_start < 0 or refresh_end < 0:
    errors.append("unable to inspect read-only callbacks")
else:
    inspect = source[inspect_start:refresh_start]
    refresh = source[refresh_start:refresh_end]
    if creation in inspect or creation in refresh: errors.append("read-only callback may create a project")
    need(inspect, "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "inspect")
    need(refresh, "_boundProject = null;", "refresh")
    need(refresh, "_boundProject = project;", "refresh")

if errors:
    print("Floor/Level stale-project preflight FAILED:")
    for error in errors: print("- " + error)
    sys.exit(1)
print("Floor/Level stale-project preflight PASS: only explicit first-floor Save may create; all other writes stay exact-bound and read-only callbacks stay non-creating.")
