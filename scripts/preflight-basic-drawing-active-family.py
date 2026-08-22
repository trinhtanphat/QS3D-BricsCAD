#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs"
DOC = ROOT / "docs/BASIC-DRAWING-ACTIVE-FAMILY.md"
errors = []

for path in (SOURCE, WORKSPACE, DOC):
    if not path.is_file():
        errors.append("missing basic-drawing dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in ("QS3DDRAWLINE", "QS3DDRAWRECT", "QS3DDRAWCIRCLE"):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectFamilyActivationService.GetActive(project)",
        "project.ChangeVersion",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs)",
        "project.ChangeVersion != expected.ChangeVersion",
        "family.Category != expected.Category",
        "project.ActiveFloorId",
        "project.ActiveZoneId",
        'private const string RegAppName = "QS3DBASICDRAW";',
        'private const string MarkerVersion = "1";',
        'IdentityToken("p1:", context.ProjectId)',
        'IdentityToken("f1:", context.FamilyId)',
        'IdentityToken("l1:", context.FloorId)',
        'IdentityToken("z1:", context.ZoneId)',
        "new Line(startResult.Value, endResult.Value)",
        "CreateRectangle(first, opposite)",
        "new Circle(centerResult.Value, Vector3d.ZAxis, radius)",
        "transaction.Commit();",
    ):
        if token not in text:
            errors.append("basic drawing source missing: " + token)

    if "SemanticCaptureService" in text:
        errors.append("basic drafting must not auto-capture arbitrary primitives as BIM semantics")
    if "GetOrCreate(" in text:
        errors.append("basic drafting must not silently bootstrap a project")

    commit_index = text.find("transaction.Commit();")
    marker_index = text.find("MarkContext(entity, context, kind);")
    if marker_index < 0 or commit_index < 0 or marker_index > commit_index:
        errors.append("context XData must be written before the native transaction commits")

if WORKSPACE.is_file():
    text = WORKSPACE.read_text(encoding="utf-8")
    for token in (
        'line.Tag = "QS3DDRAWLINE";',
        'rectangle.Tag = "QS3DDRAWRECT";',
        'circle.Tag = "QS3DDRAWCIRCLE";',
        'Key.D1 || e.Key == Key.NumPad1',
        'Key.D2 || e.Key == Key.NumPad2',
        'Key.D3 || e.Key == Key.NumPad3',
        '_viewModel.SetActiveFamily(family);',
        'ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường")',
        'ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật")',
        'ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn")',
    ):
        if token not in text:
            errors.append("Workspace basic-drawing wiring missing: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWLINE",
        "QS3DDRAWRECT",
        "QS3DDRAWCIRCLE",
        "Active Family",
        "QS3DBASICDRAW",
        "SemanticCaptureService",
        "Model Space",
        "LOCAL_PASS",
    ):
        if token not in text:
            errors.append("basic-drawing documentation missing: " + token)

if errors:
    print("Basic drawing active-Family preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Basic drawing active-Family preflight PASS")
