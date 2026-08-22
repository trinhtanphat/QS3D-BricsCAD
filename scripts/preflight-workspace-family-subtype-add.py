#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtype.cs"
quick_draw_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs"
panel_path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []


def read(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


source = read(source_path, "WorkspacePanel.FamilySubtype.cs")
quick_draw = read(quick_draw_path, "WorkspacePanel.QuickDraw.cs")
panel = read(panel_path, "WorkspacePanel.xaml.cs")

for token in (
    '"Móng Băng", "Móng Bè"',
    'CreateMenuItem("Tham số", OnAddParameterFamilyClick)',
    'CreateMenuItem("Solid3D", OnAddSolid3dFamilyClick)',
    "private void CreateFamilyFromWorkspaceSubtype(bool launchSolid3D)",
    "private static bool FamilyNameHasSubtype(string familyName, string subtype)",
    "private static string NextSubtypeFamilyName(string subtype, ISet<string> existingNames)",
    "FamilyList.ScrollIntoView(selected);",
    'TryFindResource("AccentBrush")',
):
    if token not in source:
        errors.append("Workspace subtype/Add contract missing: " + token)

method_start = source.find("private void CreateFamilyFromWorkspaceSubtype(bool launchSolid3D)")
method_end = source.find("private static void SeedQuickSchemaDefaults", method_start)
method = source[method_start:method_end] if method_start >= 0 and method_end > method_start else ""

capability_tokens = (
    "var category = _categoryFilter ?? selected?.Category ?? ElementCategory.Room;",
    "if (!string.IsNullOrWhiteSpace(subtype)) category = ElementCategory.Foundation;",
    "if (launchSolid3D && !Cad.NativeBuildCapability.Supports(category))",
    "SetStatus(Cad.NativeBuildCapability.UnsupportedMessage(category));",
)
last = -1
for token in capability_tokens:
    pos = method.find(token)
    if pos < 0:
        errors.append("Workspace Solid3D fail-closed contract missing: " + token)
    elif pos <= last:
        errors.append("Workspace Solid3D capability setup is out of order: " + token)
    else:
        last = pos

capability = method.find("if (launchSolid3D && !Cad.NativeBuildCapability.Supports(category))")
for mutation_token in (
    "ProjectContextCoordinator.GetOrCreate(doc)",
    "ExistingProjectMutationContext.Require(doc",
    "ProjectFamilyService.Create(project",
    "ProjectFamilyService.Duplicate(project",
    "ProjectFamilyActivationService.SetActive(project",
):
    pos = method.find(mutation_token)
    if capability < 0 or pos < 0 or capability > pos:
        errors.append("Unsupported Solid3D must refuse before mutation boundary: " + mutation_token)

if "AttachFamilySubtypeInteractions();" not in quick_draw:
    errors.append("Workspace subtype interactions are not attached")

for token in (
    "private void OnView3DClick(object sender, RoutedEventArgs e)",
    "if (!Cad.NativeBuildCapability.Supports(category.Value))",
    'Send("QS3DBUILD3D")',
):
    if token not in panel:
        errors.append("Workspace native builder route missing: " + token)

print("QS3D Workspace subtype/Add preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: subtype filtering, Add mode routing, unsupported-Solid3D pre-mutation refusal, native builder delegation, and selected-row highlight are source-guarded.")
