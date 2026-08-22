#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml"
BOOTSTRAP = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveBootstrap.cs"
HANDLER = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.FirstSaveHandler.cs"
errors = []

for path in (XAML, BOOTSTRAP, HANDLER):
    if not path.is_file(): errors.append("missing " + str(path.relative_to(ROOT)))
if errors:
    print("Floor/Level first-save bootstrap preflight FAILED:")
    for error in errors: print("- " + error)
    sys.exit(1)

xaml = XAML.read_text(encoding="utf-8")
bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
handler = HANDLER.read_text(encoding="utf-8")
creation = "ProjectContextCoordinator.GetOrCreate(_document)"

def need(text, token, label):
    if token not in text: errors.append(label + " missing: " + token)

need(xaml, 'Content="Lưu" Style="{StaticResource AccentButton}" Click="OnSaveFloorFirstBootstrapClick"', "xaml")
for token in (
    "private ProjectState RequireProjectForFirstSave(bool creatingNewFloor, out bool bootstrappedProject)",
    "bootstrappedProject = false;",
    "if (!creatingNewFloor || _boundProject != null)",
    'return RequireBoundProjectForMutation("lưu tầng", "Lưu Floor/Level");',
    'EnsureBoundDrawingIsActive("lưu tầng");',
    "ProjectContextCoordinator.TryGetReadOnly(_document, out _)",
    creation,
    "bootstrappedProject = true;",
): need(bootstrap, token, "bootstrap")
if bootstrap.count(creation) != 1: errors.append("bootstrap must contain exactly one canonical creation call")

for token in (
    "var creatingNewFloor = string.IsNullOrWhiteSpace(_editingFloorId);",
    "var name = (FloorNameBox.Text ?? string.Empty).Trim();",
    "ValidateFirstSaveFloorDraft(name);",
    "var elevation = ParseElevation(FloorElevationBox.Text);",
    "RequireProjectForFirstSave(creatingNewFloor, out var bootstrappedProject)",
    "ProjectStateSnapshot.Capture(project)",
    "if (creatingNewFloor)",
    "ProjectFloorService.Create(project",
    "ProjectFloorService.Update(project",
    "RestoreOrThrow(project, rollback, operationError, \"Lưu Floor/Level\")",
    "if (bootstrappedProject)",
    "ProjectContextCoordinator.Forget(_document);",
    "RefreshAfterCommit(",
): need(handler, token, "handler")

validation = handler.find("ValidateFirstSaveFloorDraft(name);")
parse = handler.find("var elevation = ParseElevation(FloorElevationBox.Text);")
acquire = handler.find("RequireProjectForFirstSave(creatingNewFloor, out var bootstrappedProject)")
if min(validation, parse, acquire) < 0 or not (validation < parse < acquire):
    errors.append("name/elevation validation must complete before project acquisition")

restore = handler.find("RestoreOrThrow(project, rollback, operationError, \"Lưu Floor/Level\")")
forget = handler.find("ProjectContextCoordinator.Forget(_document);", restore)
if restore < 0 or forget < restore: errors.append("failed first-save mutation must restore before forgetting the new context")

if creation in handler: errors.append("handler must not create a project directly")
if "GetOrCreate" in xaml: errors.append("XAML must not contain project-creation semantics")

if errors:
    print("Floor/Level first-save bootstrap preflight FAILED:")
    for error in errors: print("- " + error)
    sys.exit(1)
print("Floor/Level first-save bootstrap preflight PASS: explicit new-floor Save validates first, creates only from a still-projectless refresh state, and rolls back/forgets failed first-save mutations.")
