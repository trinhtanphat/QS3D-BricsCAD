#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReleaseReadinessCommands.cs"

errors = []

if not PATH.is_file():
    errors.append("missing ReleaseReadinessCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ModelHealthWindowPresenter.Show(document, issues, issue => Locate(document, issue))",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "currentProject.FindElement(issue.ElementId)",
    ):
        if token not in text:
            errors.append("ReleaseReadinessCommands.cs missing read-only token: " + token)
    for bypass in ("Application.ShowModelessWindow(", "new ModelHealthWindow("):
        if bypass in text:
            errors.append("QS3DRELEASECHECK must route Model Health publication through the transactional presenter: " + bypass)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("QS3DRELEASECHECK must never create/cache project state merely to inspect release readiness.")
    for stale in (
        "issue => Locate(document, project, issue)",
        "Locate(Document document, QS3D.Core.Domain.ProjectState project, ModelHealthIssue issue)",
        "project.FindElement(issue.ElementId)",
    ):
        if stale in text:
            errors.append("QS3DRELEASECHECK modeless Locate still captures stale project state: " + stale)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DRELEASECHECK stays read-only, re-resolves current project state, and routes fresh snapshots through transactional Model Health publication.")
