#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"
AUDIT_COMMAND = ROOT / "src/QS3D.BricsCAD.V25/AuditCommands.cs"
FILES = [
    "AuditLogWindow.xaml.cs",
    "CurtainWallWindow.xaml.cs",
    "DoorOpeningScheduleWindow.xaml.cs",
    "FamilyManagerWindow.xaml.cs",
    "FloorLevelWindow.xaml.cs",
    "MaterialCatalogWindow.xaml.cs",
    "ModelHealthWindow.xaml.cs",
    "ProjectToolsWindow.xaml.cs",
    "QuantitySummaryWindow.xaml.cs",
    "RebarMeshSetupWindow.xaml.cs",
    "RebarScheduleWindow.xaml.cs",
    "RecognitionWindow.xaml.cs",
    "RevisionWindow.xaml.cs",
    "RoomFinishScheduleWindow.xaml.cs",
    "ScheduleHubWindow.xaml.cs",
    "ZoneManagerWindow.xaml.cs",
]
errors = []

for name in FILES:
    path = UI / name
    if not path.is_file():
        errors.append("missing document-bound modeless source: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    if "DocumentBoundWindowLifetime.Attach(this, _document);" not in text:
        errors.append(name + " must close automatically when its source DWG is destroyed.")

audit = UI / "AuditLogWindow.xaml.cs"
if audit.is_file():
    text = audit.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "Chưa có QS3D project hiện hữu; Audit Log không tạo project mới",
        "_rows = Array.Empty<AuditEvent>()",
    ):
        if token not in text:
            errors.append("AuditLogWindow missing read-only viewer token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("AuditLogWindow must not create/cache project state merely to display audit history.")

if not AUDIT_COMMAND.is_file():
    errors.append("missing AuditCommands.cs read-only entrypoint")
else:
    text = AUDIT_COMMAND.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "new AuditLogWindow(document)",
        "chưa có QS3D project hiện hữu; không tạo project mới",
    ):
        if token not in text:
            errors.append("AuditCommands missing read-only entrypoint token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("QS3DAUDIT entrypoint must not create/cache project state before opening the read-only Audit Log.")

curtain = UI / "CurtainWallWindow.xaml.cs"
if curtain.is_file():
    text = curtain.read_text(encoding="utf-8")
    for token in (
        "if (!(FamilyCombo.SelectedItem is ProjectFamily selectedFamily)) return;",
        "var family = project.FindFamily(selectedFamily.Id)",
        "family.Category != ElementCategory.GlassWall",
    ):
        if token not in text:
            errors.append("CurtainWallWindow missing stale-family fail-closed token: " + token)

mesh = UI / "RebarMeshSetupWindow.xaml.cs"
if mesh.is_file():
    text = mesh.read_text(encoding="utf-8")
    for token in (
        "if (!ReferenceEquals(project, _project))",
        "Project của DWG này đã được reload/thay thế",
        "var element = project.FindElement(_element.Id)",
    ):
        if token not in text:
            errors.append("RebarMeshSetupWindow missing stale-project fail-closed token: " + token)

health = UI / "ModelHealthWindow.xaml.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var current)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "_projectIdAtOpen = projectAtOpen.ProjectId",
        "_updatedUtcAtOpen = projectAtOpen.UpdatedUtc",
        "_changeVersionAtOpen = projectAtOpen.ChangeVersion",
        "_drawingFingerprintAtOpen = projectAtOpen.DrawingFingerprint ?? string.Empty",
        "current.UpdatedUtc == _updatedUtcAtOpen",
        "current.ChangeVersion == _changeVersionAtOpen",
        "MatchesSnapshot(current)",
        "Model Health cần một QS3D project hiện hữu; cửa sổ kiểm tra không tạo project mới",
    ):
        if token not in text:
            errors.append("ModelHealthWindow missing read-only semantic-snapshot freshness token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("ModelHealthWindow must not create/cache a project merely to display or freshness-check a read-only Health snapshot.")
    if "ReferenceEquals(current, _projectAtOpen)" in text:
        errors.append("ModelHealthWindow must compare semantic snapshot stamps, not ProjectState object identity; read-only sidecar loads may produce equivalent detached instances.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: document-bound modeless windows close with their DWG; Audit command/viewer and Model Health remain read-only, Model Health uses semantic snapshot stamps, Curtain re-resolves selected Family, and Rebar Mesh rejects replaced project state.")
