#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs"
HEALTH_ALL = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
errors = []

for path in (WINDOW, HEALTH_ALL):
    if not path.is_file():
        errors.append("missing Model Health locate contract file: " + str(path.relative_to(ROOT)))

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    if "_locate == null || !(IssueGrid.SelectedItem is ModelHealthIssue issue)" not in text:
        errors.append("ModelHealthWindow locate entry point changed; verify project-level issue routing")
    if "string.IsNullOrWhiteSpace(issue.ElementId)" in text:
        errors.append("ModelHealthWindow must not reject blank ElementId before the caller can locate project-owned artifacts")
    if "EnsureActiveAndCurrent();" not in text or "_locate(issue);" not in text:
        errors.append("ModelHealthWindow must validate source DWG/snapshot freshness before dispatching Locate")

if HEALTH_ALL.is_file():
    text = HEALTH_ALL.read_text(encoding="utf-8")
    for token in (
        "if (string.IsNullOrWhiteSpace(issue.ElementId))",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "LocateProjectArtifactHandles(currentProject, issue.Code)",
        "CadHandleService.Select(document, artifactHandles)",
    ):
        if token not in text:
            errors.append("HealthAllCommands.cs missing project-level artifact locate token: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Model Health forwards project-level issues so QS3DHEALTHALL can locate owned native artifacts")
