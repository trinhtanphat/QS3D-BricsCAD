#!/usr/bin/env python3
from pathlib import Path
import sys

# Lane issue-3504: source-only guard for transient Coordination Manager CAD review actions.
# Licensed interactive behavior remains LOCAL_ONLY under #72.
ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"
REVIEW = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerWindow.cs"
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
review = text(REVIEW)
window = text(WINDOW)
v26 = text(V26)

require(command, "CoordinationManagerReviewUi.Attach", "review controller attachment to canonical manager")
require(window, "CoordinationIssuePersistence.Load", "existing canonical manager persistence seam")

for needle, label in [
    ("ResolveReviewTargets", "single validated review target gate"),
    ("ProjectContextCoordinator.TryGetReadOnly", "read-only existing-project boundary"),
    ("DrawingFingerprint", "drawing fingerprint revalidation"),
    ("CoordinationIssuePersistence.Load", "canonical persisted issue reload"),
    ("UpdatedAtUtc != selected.UpdatedAtUtc", "fresh-row revision check"),
    ("EvaluateRelink", "canonical relink evaluation"),
    ("ReadyForHostValidation", "relink-ready gate"),
    ("Relinked", "relinked gate"),
    ("SourceHandleResolver.Resolve", "semantic-to-current-source resolution"),
    ("CadHandleService.Resolve", "live CAD resolution"),
    ("if (resolved.Count != handles.Count)", "full-pair resolve-all check"),
    ("var resolved = ResolveReviewTargets();", "validation before effect"),
    ("effect(resolved);", "native effect dispatch after validation"),
    ("entity.Highlight();", "native transient highlight"),
    ("entity.Unhighlight();", "native highlight cleanup after non-null Entity validation"),
    ('"_.ISOLATEOBJECTS "', "native isolate action"),
    ('"_.UNISOLATEOBJECTS "', "native isolation restore"),
    ('"OBJECTISOLATIONMODE"', "non-persistent isolation mode guard"),
    ("ViewTableRecord", "native view snapshot"),
    ("BackClipEnabled = true", "real back clipping section plane"),
    ("FrontClipEnabled = true", "real front clipping section plane"),
    ("SetCurrentView", "native section/focus view application"),
    ("ResetTransientStateBestEffort", "manager-owned transient cleanup"),
    ("DocumentActivated", "document-change cleanup"),
    ("DocumentToBeDestroyed", "document-destroy cleanup"),
    ("SelectionChanged", "row-change cleanup"),
    ("Dispose()", "window/session cleanup"),
]:
    require(review, needle, label)

require(v26, r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"', "V26 shared-source parity")

resolve_at = review.find("var resolved = ResolveReviewTargets();")
effect_at = review.find("effect(resolved);")
if min(resolve_at, effect_at) < 0 or resolve_at >= effect_at:
    errors.append("review action ordering must be ResolveReviewTargets -> native effect")

cad_resolve_at = review.find("CadHandleService.Resolve")
full_at = review.find("if (resolved.Count != handles.Count)")
return_at = review.find("return resolved.ToList().AsReadOnly()")
if min(cad_resolve_at, full_at, return_at) < 0 or not (cad_resolve_at < full_at < return_at):
    errors.append("target gate ordering must be live Resolve -> full-count check -> return validated ObjectIds")

for forbidden, label in [
    ("CoordinationIssuePersistence.Save", "second/presentation persistence write"),
    ("ProjectContextCoordinator.Save", "project write from visual review lane"),
    ("OpenMode.ForWrite", "persistent CAD write-open"),
    (".Color =", "persistent entity color mutation"),
    ("ColorIndex =", "persistent entity color-index mutation"),
    ("Material =", "persistent entity material mutation"),
    ("Assembly.Load", "runtime assembly probing"),
    ("BLT3D", "BLT3D dependency"),
    ("BooleanOperation", "second clash detector/native boolean mutation"),
    ('SendStringToExecute("QS3DZOOMSELECTED', "zoom masquerading as section/focus"),
]:
    if forbidden in review:
        errors.append(f"forbidden {label} found in review action source: {forbidden}")

if errors:
    print("ERROR: issue-3504 Coordination Manager review-action source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: issue-3504 manager review actions revalidate provenance/full-pair before transient native CAD effects and clean document-bound state.")
print("NOTE: licensed interactive V25/V26 behavior remains LOCAL_ONLY under #72; this guard is not runtime evidence.")
