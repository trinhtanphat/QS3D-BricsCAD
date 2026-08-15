#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing required source: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


structural = read("src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs")
snapshots = read("src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs")
command_surface = read("src/QS3D.BricsCAD.V25/Commands.cs")
capture = read("src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs")
build3d = read("src/QS3D.BricsCAD.V25/Build3DCommands.cs")
eligibility = read("src/QS3D.Core/Recognition/EntitySnapshotCaptureEligibility.cs")
bootstrapper = read("src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs")
update_commands = read("src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs")

for token, message in {
    "entity is Arc arc": "Beam ARC source routing missing",
    "entity is Circle circle": "round structural CIRCLE source routing missing",
    "CadPolylinePathReader.ReadOpenWcsXy": "curved Beam POLYLINE tessellation missing",
    "SampleCircularPath": "curved/round Beam circular tessellation missing",
    "BooleanOperation(BooleanOperationType.BoolUnite": "curved Beam segmented solid union missing",
    "BuildClosedProfilePrism(document, project, circle": "Slab/Column CIRCLE extrusion routing missing",
    "solid.CreateExtrudedSolid(profile": "closed structural profile extrusion missing",
}.items():
    if token not in structural:
        errors.append(message)

if "if (entity is Circle circle)" not in snapshots or "Math.PI * circle.Radius * circle.Radius" not in snapshots:
    errors.append("CIRCLE snapshot area metric missing")

for token, message in {
    '[CommandMethod("QS3DBEAM", CommandFlags.UsePickSet)] public void CaptureBeam() => Capture(ElementCategory.Beam, "Dầm");': "QS3DBEAM semantic capture route missing",
    '[CommandMethod("QS3DSLAB", CommandFlags.UsePickSet)] public void CaptureSlab() => Capture(ElementCategory.Slab, "Sàn");': "QS3DSLAB semantic capture route missing",
    '[CommandMethod("QS3DCOLUMN", CommandFlags.UsePickSet)] public void CaptureColumn() => Capture(ElementCategory.Column, "Cột");': "QS3DCOLUMN semantic capture route missing",
}.items():
    if token not in command_surface:
        errors.append(message)

for token, message in {
    "EntitySnapshotReader.ReadCurrentSelection(document)": "semantic capture must read the selected native source",
    "EntitySnapshotCaptureEligibility.EnsureReady(snapshot, category)": "semantic capture eligibility boundary missing",
    "CaptureSnapshotCore(document, project, snapshot, category)": "semantic capture batch route missing",
}.items():
    if token not in capture:
        errors.append(message)

if "case ElementCategory.Beam:" not in eligibility or "ready = hasLength || hasVolume;" not in eligibility:
    errors.append("Beam capture eligibility must accept a finite positive curve length")
if "case ElementCategory.Column:" not in eligibility or "case ElementCategory.Slab:" not in eligibility or "ready = hasArea || hasVolume;" not in eligibility:
    errors.append("Column/Slab capture eligibility must accept a finite positive profile area")

for token, message in {
    "return StructuralSolidBuilder.Supports(category)": "QS3DBUILD3D structural capability dispatch missing",
    "StructuralSolidBuilder.BuildSelected(document, project, category)": "QS3DBUILD3D structural builder dispatch missing",
    "document.Editor.SetImpliedSelection(sourceIds.ToArray())": "QS3DBUILD3D validated source handoff missing",
}.items():
    if token not in build3d:
        errors.append(message)

if "UpdateCenterWindowHost.Show" in bootstrapper:
    errors.append("automatic update discovery must remain non-modal")
if "UpdateCenterWindowHost.Show();" not in update_commands:
    errors.append("explicit QS3DUPDATE command must still open Update Center")
if "AutomaticUpdateFound += OnAutomaticUpdateFound" not in bootstrapper:
    errors.append("automatic non-modal update notification subscription missing")

if errors:
    print("QS3D Sheet residual structural/update preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(1)

print("PASS: curved/round structural capture-to-build routes and non-modal automatic update discovery are source-guarded.")
raise SystemExit(0)
