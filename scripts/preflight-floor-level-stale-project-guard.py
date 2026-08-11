#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    print("ERROR: missing " + str(SOURCE.relative_to(ROOT)))
    sys.exit(1)

text = SOURCE.read_text(encoding="utf-8")

for token in (
    "private ProjectState _boundProject;",
    "_boundProject = null;",
    "_boundProject = project;",
    "private ProjectState RequireBoundProjectForRead(string operation)",
    "ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject)",
    "!ReferenceEquals(currentProject, _boundProject)",
    "private ProjectState RequireBoundProjectForMutation(string operation, string mutationContext)",
    "var currentProject = RequireBoundProjectForRead(operation);",
    "var project = ExistingProjectMutationContext.Require(_document, mutationContext);",
    "!ReferenceEquals(project, currentProject)",
    "!ReferenceEquals(project, _boundProject)",
    'RequireBoundProjectForMutation("lưu tầng", "Lưu Floor/Level")',
    'RequireBoundProjectForMutation("xóa tầng", "Xóa Floor/Level")',
    'RequireBoundProjectForMutation("đặt tầng hoạt động", "Đặt Floor/Level active")',
    'var previewProject = RequireBoundProjectForRead("gán tầng cho selection");',
    'ExistingProjectMutationContext.Require(_document, "Gán Floor/Level cho selection")',
    "!ReferenceEquals(project, previewProject)",
    "QS3D project đã thay đổi từ lần Refresh gần nhất. Hãy Refresh Level Picker",
):
    if token not in text:
        errors.append("FloorLevel stale-project contract missing: " + token)

if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
    errors.append("FloorLevel modeless callbacks must not create/replacement-bootstrap project state.")

for legacy in (
    'EnsureBoundDrawingIsActive("lưu tầng");\n                var project = ExistingProjectMutationContext.Require(_document, "Lưu Floor/Level");',
    'EnsureBoundDrawingIsActive("xóa tầng");\n                var project = ExistingProjectMutationContext.Require(_document, "Xóa Floor/Level");',
    'EnsureBoundDrawingIsActive("đặt tầng hoạt động");\n                var project = ExistingProjectMutationContext.Require(_document, "Đặt Floor/Level active");',
    'EnsureBoundDrawingIsActive("gán tầng cho selection");\n                if (!(FloorList.SelectedItem is FloorDefinition selectedFloor))',
):
    if legacy in text:
        errors.append("document-only FloorLevel mutation guard returned: " + legacy.split(";")[0])

# Read-only inspection intentionally stays document-bound and may inspect the newly-current
# project without mutating it; stale project binding is enforced only before writes.
inspect_start = text.find("private void OnInspectSelectionClick")
refresh_start = text.find("private void RefreshAll", inspect_start)
if inspect_start < 0 or refresh_start < 0:
    errors.append("FloorLevel inspect/refresh methods missing")
else:
    inspect = text[inspect_start:refresh_start]
    if "ExistingProjectMutationContext.Require" in inspect or "RequireBoundProjectForMutation" in inspect:
        errors.append("read-only FloorLevel inspection must not bind a mutation context")
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" not in inspect:
        errors.append("read-only FloorLevel inspection must resolve current project read-only")

refresh_end = text.find("private FloorDefinition RequireSelectedFloor", refresh_start)
if refresh_start >= 0 and refresh_end > refresh_start:
    refresh = text[refresh_start:refresh_end]
    no_project = refresh.find("if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))")
    clear_binding = refresh.find("_boundProject = null;", no_project)
    bind_project = refresh.find("_boundProject = project;", no_project)
    if no_project < 0 or clear_binding < no_project or bind_project < clear_binding:
        errors.append("RefreshAll must clear stale binding on unavailable project and bind only after a successful project refresh")
else:
    errors.append("unable to inspect RefreshAll binding order")

if errors:
    print("Floor/Level stale-project preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Floor/Level stale-project preflight PASS: modeless writes require the exact project instance from the latest successful RefreshAll, while inspection stays read-only.")
