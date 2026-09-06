#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
QUICK = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Workspace source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


quick = read(QUICK)


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]


quick_draw = method_body(quick, "private void ExecuteWorkspaceDraw(bool advanced)", "private void ExecuteWorkspaceRepeatedDraw")
repeat_draw = method_body(quick, "private void ExecuteWorkspaceRepeatedDraw()", "private void ExecuteWorkspaceBasicDraw")
basic_draw = method_body(quick, "private void ExecuteWorkspaceBasicDraw", "private bool TryActivateFamilyForCommand")
helper_start = quick.find("private bool TryActivateFamilyForCommand")
helper = quick[helper_start:] if helper_start >= 0 else ""
if helper_start < 0:
    errors.append("missing TryActivateFamilyForCommand")

for label, body in [
    ("quick draw", quick_draw),
    ("repeated draw", repeat_draw),
    ("basic draw", basic_draw),
]:
    if "TryActivateFamilyForCommand(family" not in body:
        errors.append(label + " command must verify selected Family affinity before dispatch")
    activation = body.find("TryActivateFamilyForCommand")
    send = body.find("Send(")
    if activation >= 0 and send >= 0 and activation > send:
        errors.append(label + " command dispatch occurs before Family affinity validation")

for needle in [
    "Application.DocumentManager.MdiActiveDocument",
    "ExistingProjectMutationContext.TryGet",
    "project.FindFamily(family.Id)",
    "ReferenceEquals(ownedFamily, family)",
    "_viewModel.SetActiveFamily(family);",
    "ProjectFamilyActivationService.GetActive(project)",
    "ReferenceEquals(activeFamily, ownedFamily)",
]:
    if needle not in helper:
        errors.append("family command-affinity helper missing token: " + needle)

if "ProjectContextCoordinator.GetOrCreate" in helper:
    errors.append("command-affinity validation must never create a replacement project")

print("QS3D Workspace Quick Draw Family command-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: all Family-scoped Workspace Quick Draw routes fail closed unless the selected Family belongs to and becomes active in the current active-document project.")
