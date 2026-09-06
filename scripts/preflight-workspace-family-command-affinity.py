#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing WorkspacePanel source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

capture = method_body("private void OnCaptureSelectedClick", "private void OnView3DClick")
build = method_body("private void OnView3DClick", "private void OnWallJunctionsClick")
helper = method_body("private bool TryActivateFamilyForCommand", "private void OnWallJunctionsClick")

for label, body in [("capture", capture), ("build", build)]:
    if "TryActivateFamilyForCommand(family" not in body:
        errors.append(label + " command must verify selected Family affinity before dispatch")
    if "return;" not in body:
        errors.append(label + " command must fail closed when Family activation cannot be proven")

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
if "Send(" in helper:
    errors.append("affinity helper must not dispatch commands itself")

for label, body in [("capture", capture), ("build", build)]:
    activation = body.find("TryActivateFamilyForCommand")
    send = body.find("Send(")
    if activation >= 0 and send >= 0 and activation > send:
        errors.append(label + " command dispatch occurs before Family affinity validation")

print("QS3D Workspace Family command-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Family-scoped Workspace commands fail closed unless the selected Family belongs to and becomes active in the current active-document project.")
