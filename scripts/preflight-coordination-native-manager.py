#!/usr/bin/env python3
from pathlib import Path
import sys

# Lane issue-3494: guard only the persisted Coordination Manager integration seam.
ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerWindow.cs"
CORE_PROJECTION = ROOT / "src/QS3D.Core/Coordination/CoordinationManagerProjection.cs"
PERSISTENCE = ROOT / "src/QS3D.Core/Coordination/CoordinationIssuePersistence.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"

errors = []

def text(path: Path) -> str:
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(blob: str, needle: str, label: str) -> None:
    if needle not in blob:
        errors.append(f"missing {label}: {needle}")

command = text(COMMAND)
window = text(WINDOW)
projection = text(CORE_PROJECTION)
persistence = text(PERSISTENCE)
v26 = text(V26)

require(command, 'CommandMethod("QS3DCOORDINATIONMANAGER"', "native manager command")
require(command, "ProjectContextCoordinator.TryGetReadOnly", "read-only existing-project boundary")
require(window, "CoordinationIssuePersistence.Load", "canonical persisted issue load")
require(window, "CoordinationManagerProjection.Build", "canonical deterministic manager projection")
require(window, "EvaluateRelink", "canonical relink evaluation")
require(window, "SourceHandleResolver.Resolve", "semantic-to-current-source resolution")
require(window, "CadHandleService.Resolve", "resolve-all native validation")
require(window, "if (resolved.Count != handles.Count)", "full live handle set check")
require(window, "SetImpliedSelection", "selection only after validation")
require(window, "ExistingProjectMutationContext.Require", "canonical mutation bind")
require(window, "CoordinationIssue.CanTransition", "canonical lifecycle transition validation")
require(window, ".TransitionTo(", "canonical status mutation")
require(window, ".Assign(", "canonical assignee mutation")
require(window, ".AddComment(", "canonical comment mutation")
require(window, "CoordinationIssuePersistence.Save", "canonical persistence write")
require(window, "ProjectContextCoordinator.Save", "existing QSDB save path")
require(window, "project.Metadata.Clear()", "metadata rollback on save failure")
require(window, "DrawingFingerprint", "drawing provenance revalidation")
require(projection, "public static class CoordinationManagerProjection", "host-neutral manager projection foundation")
require(persistence, "public static class CoordinationIssuePersistence", "canonical coordination persistence foundation")
require(v26, r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"', "V26 shared C# source parity")

for forbidden, label in [
    ("Assembly.Load", "runtime assembly probing"),
    ("BLT3D", "BLT3D dependency"),
    ("BooleanOperation", "second clash detector/native boolean mutation"),
    ("ForWrite", "unnecessary CAD write-open"),
]:
    if forbidden in command or forbidden in window:
        errors.append(f"forbidden {label} found in native manager source: {forbidden}")

resolve_at = window.find("CadHandleService.Resolve")
select_at = window.find("SetImpliedSelection")
full_at = window.find("if (resolved.Count != handles.Count)")
if min(resolve_at, select_at, full_at) < 0 or not (resolve_at < full_at < select_at):
    errors.append("fail-closed locate ordering must be Resolve -> full-count check -> SetImpliedSelection")

if errors:
    print("ERROR: issue-3494 Coordination Manager source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: issue-3494 native persisted Coordination Manager source wiring is fail-closed and V25/V26-shared.")
print("NOTE: licensed interactive V25/V26 runtime remains LOCAL_ONLY under #72; this guard is not runtime evidence.")
