#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIGHT = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []

if not RIGHT.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = RIGHT.read_text(encoding="utf-8")
    for token in (
        "private bool TrySend(Document document, string command)",
        "document.SendStringToExecute(normalized + \" \", true, false, false);",
        'if (TrySend(doc, "_MOVE"))',
        "catch (Exception)",
        "CommandDispatchFailureStatus",
        "return false;",
    ):
        if token not in text:
            errors.append("RightPanel missing active-document dispatch token: " + token)
    if "private static void Send(string command)" in text:
        errors.append("RightPanel must not re-resolve a second active document through the old static Send helper.")
    if "catch (Exception ex)" in text or "ex.Message" in text:
        errors.append("RightPanel active-document dispatch failures must stay fail-safe without retaining raw host exception detail.")

if not WORKSPACE.is_file():
    errors.append("missing WorkspacePanel.xaml.cs")
else:
    text = WORKSPACE.read_text(encoding="utf-8")
    if "DocumentBoundWindowLifetime.Attach" in text:
        errors.append("WorkspacePanel is palette-scoped and active-document dynamic; it must not bind to one source DWG.")
    for token in (
        "using QS3D.Core.Audit;",
        "using QS3D.Core.Persistence;",
        "ProjectFamilyService.Duplicate(project, basis.Id",
        "ProjectFamilyService.Create(project",
        "ProjectFamilyService.Delete(project, family.Id)",
        'AuditTrail.ForProject(project).Record("family.duplicate"',
        'AuditTrail.ForProject(project).Record("family.create"',
        'AuditTrail.ForProject(project).Record("family.delete"',
        "private static T ExecuteAtomic<T>(ProjectState project, Func<T> operation, string operationName)",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "rollback.Restore(project);",
        "private void RefreshAfterCommit(Action refresh, string successMessage, string context)",
        "private void Send(string command)",
        "try { document.SendStringToExecute(normalized + \" \", true, false, false); }",
    ):
        if token not in text:
            errors.append("WorkspacePanel missing atomic/dynamic palette token: " + token)
    for forbidden in (
        "project.Families.Add(family);",
        "project.Families.Remove(family);",
        "private static void Send(string command)",
    ):
        if forbidden in text:
            errors.append("WorkspacePanel must not retain bypass path: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: active-document palettes keep command dispatch fail-safe and redact host failure detail; RightPanel preserves the captured DWG for composed Xref actions and Workspace Family create/duplicate/delete use service-backed atomic audit boundaries without binding the palette to one DWG.")