#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Workspace source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]


workspace = read(WORKSPACE)
capture = method_body(workspace, "private void OnCaptureSelectedClick", "private void OnView3DClick")
build3d = method_body(workspace, "private void OnView3DClick", "private bool TryActivateFamilyForWorkspaceAction")
affinity = method_body(workspace, "private bool TryActivateFamilyForWorkspaceAction", "private void OnWallJunctionsClick")

for label, body in [("Capture Selected", capture), ("Vẽ/Cập nhật 3D", build3d)]:
    if "family != null" not in body:
        errors.append(label + " must preserve category-only operation while fencing every selected Family")
    if "TryActivateFamilyForWorkspaceAction(family" not in body:
        errors.append(label + " must fail closed through the selected-Family affinity fence")
    activation = body.find("TryActivateFamilyForWorkspaceAction")
    send = body.find("Send(")
    if activation >= 0 and send >= 0 and activation > send:
        errors.append(label + " dispatch occurs before selected-Family affinity validation")

restore = build3d.find("SelectInspectionSemanticSourcesForBuild()")
activation = build3d.find("TryActivateFamilyForWorkspaceAction")
if restore >= 0 and activation >= 0 and activation > restore:
    errors.append("Vẽ/Cập nhật 3D restores inspection semantic sources before selected-Family affinity validation")

for required, message in [
    ("Application.DocumentManager.MdiActiveDocument", "affinity fence must bind to the current active document"),
    ("ExistingProjectMutationContext.TryGet(document, out var project)", "affinity fence must require an existing current-document project"),
    ("project.FindFamily(family.Id)", "affinity fence must resolve the Family in the current project generation"),
    ("ReferenceEquals(ownedFamily, family)", "affinity fence must require exact Family object identity"),
    ("_viewModel.SetActiveFamily(family)", "affinity fence must activate through the canonical Workspace view-model path"),
    ("ProjectFamilyActivationService.GetActive(project)", "affinity fence must read back the canonical active Family"),
    ("ReferenceEquals(activeFamily, ownedFamily)", "affinity fence must verify activation by exact object identity"),
    ("catch (Exception)", "affinity fence must fail closed instead of surfacing catalog/reload exceptions through WPF event dispatch"),
    ("Chi tiết nội bộ đã được ẩn", "affinity fence failure must redact internal exception details"),
]:
    if required not in affinity:
        errors.append(message)

print("QS3D Workspace Capture/Build3D Family affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Capture Selected and Vẽ/Cập nhật 3D fail closed before side effects unless a selected Family is current and active, and affinity-fence exceptions remain redacted.")
