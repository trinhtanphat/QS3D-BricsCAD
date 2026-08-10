#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append("missing B4D unit/proxy safety file: " + rel)
        return ""
    return path.read_text(encoding="utf-8")

policy = read("src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs")
cad_units = read("src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs")
workflow = read("src/QS3D.BricsCAD.V25/Services/DrawingUnitWorkflow.cs")
commands = read("src/QS3D.BricsCAD.V25/Commands.cs")
review = read("src/QS3D.BricsCAD.V25/ReviewCommands.cs")
capture = read("src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs")
reconcile = read("src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs")
eligibility = read("src/QS3D.Core/Recognition/EntitySnapshotCaptureEligibility.cs")
engine = read("src/QS3D.Core/Recognition/RecognitionEngine.cs")
window = read("src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml.cs")
tools_xaml = read("src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
proxy_smoke = read("tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs")

def require(label, text, tokens):
    for token in tokens:
        if token not in text:
            errors.append(label + " missing token: " + token)

require("unit policy", policy, (
    "OverrideMetadataKey", "BoundMetadataKey", "BindingSourceMetadataKey",
    "ValidateQuantityCompatibility", "BindQuantityUnit", "ProjectOverride",
))
require("CAD unit resolver", cad_units, (
    "TryGetNativeLengthUnit", "TryGetPolicy", "Drawing units are unresolved",
    "ValidateQuantityCompatibility",
))
if "default: return LengthUnit.Millimeter" in cad_units:
    errors.append("CadUnitService still silently falls back to millimeter.")
require("unit workflow", workflow, (
    "ProjectStateSnapshot.Capture", "rollback.Restore(project)",
    "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    "ProjectContextCoordinator.Save(document)", "USSurveyMile",
))
require("commands", commands, (
    'CommandMethod("QS3DUNITS"',
    'DrawingUnitWorkflow.EnsureResolved(doc, "QS3DBQ")',
    'DrawingUnitWorkflow.EnsureResolved(doc, "QS3DED2")',
))
ed2_start = commands.find('CommandMethod("QS3DED2"')
ed2_end = commands.find('CommandMethod("QS3DBBS"', ed2_start)
ed2 = commands[ed2_start:ed2_end]
if ed2.find("DrawingUnitWorkflow.EnsureResolved") < 0 or ed2.find("DrawingUnitWorkflow.EnsureResolved") > ed2.find("GetKeywords"):
    errors.append("ED2 must resolve units before scope prompting/regeneration/export.")
b4d_start = review.find("private static void RecognizeInternal")
b4d_end = review.find('CommandMethod("QS3DREVBASE"', b4d_start)
b4d = review[b4d_start:b4d_end]
if b4d.find("DrawingUnitWorkflow.EnsureResolved") < 0 or b4d.find("DrawingUnitWorkflow.EnsureResolved") > b4d.find("ReadCurrentSpace"):
    errors.append("B4D must resolve units before scanning Current Space.")
require("semantic capture", capture, (
    "EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category)",
    "DrawingUnitResolutionPolicy.BindQuantityUnit",
))
require("source reconcile", reconcile, ("DrawingUnitResolutionPolicy.BindQuantityUnit",))
require("proxy eligibility", eligibility, (
    '"ProxyEntity"', "VolumeDrawingUnitsCubed",
    "finite positive primary metric", "EnsureReady",
))
require("proxy smoke", proxy_smoke, ("SurfaceAreaDrawingUnitsSquared", "double.NaN", "double.PositiveInfinity"))
require("recognition engine", engine, (
    "public bool IsCaptureReady", "!IsCaptureReady", "x.IsCaptureReady",
    "capture-blocked:",
))
require("recognition UI", window, ("x.IsCaptureReady",))
require("Project Tools", tools_xaml, ('Tag="QS3DUNITS"', 'x:Name="UnitText"'))
require("smoke registration", registration, (
    "DrawingUnitResolutionSmoke.Run();", "ProxyCaptureEligibilitySmoke.Run();",
))

print("QS3D B4D unit/proxy safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: unresolved units fail closed with persisted quantity binding, and unmeasured ProxyEntity candidates remain review-only across auto-apply, UI, and capture.")
