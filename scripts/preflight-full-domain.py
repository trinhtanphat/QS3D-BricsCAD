#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Export/RebarCsvExporter.cs",
    "src/QS3D.Core/Services/StructuralRegenerator.cs",
    "src/QS3D.Core/Geometry/RoomBoundaryEngine.cs",
    "src/QS3D.Core/Geometry/BulgeArcTessellator.cs",
    "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs",
    "src/QS3D.BricsCAD.V25/DomainHubCommands.cs",
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs",
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml.cs",
    "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs",
    "scripts/package-v25.ps1",
    "scripts/install-v25-autoload.ps1",
    "scripts/uninstall-v25-autoload.ps1",
    "docs/COMMANDS.md",
    "docs/V25-INSTALL.md",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing full-domain/release file: " + rel)

command_owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("([^\"]+)"', text):
        command_owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, owners in sorted(command_owners.items()):
    if len(owners) > 1: errors.append("duplicate CommandMethod " + command + ": " + ", ".join(owners))
if "QS3DROOMAUTO" not in command_owners: errors.append("QS3DROOMAUTO command is not registered in source")

solid = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
if solid.exists():
    text = solid.read_text(encoding="utf-8")
    for needle in ("ElementCategory.Stair", "ElementCategory.Railing", "ElementCategory.Earthwork", "DownwardFootprintMass"):
        if needle not in text: errors.append("full-domain native mass adapter missing: " + needle)

regen = ROOT / "src/QS3D.Core/Services/StructuralRegenerator.cs"
if regen.exists():
    text = regen.read_text(encoding="utf-8")
    for needle in ("RegenerateStair", "RegenerateRailing", "RegenerateEarthwork", "BulkedVolumeM3", "NetExportM3"):
        if needle not in text: errors.append("full-domain quantity regenerator missing: " + needle)

csv = ROOT / "src/QS3D.Core/Export/RebarCsvExporter.cs"
if csv.exists():
    text = csv.read_text(encoding="utf-8")
    for needle in ("TrimStart", "double.IsNaN", "double.IsInfinity", "new UTF8Encoding(true)"):
        if needle not in text: errors.append("BBS CSV safety guard missing: " + needle)

room_engine = ROOT / "src/QS3D.Core/Geometry/RoomBoundaryEngine.cs"
if room_engine.exists():
    text = room_engine.read_text(encoding="utf-8")
    for needle in ("CollectPairCuts", "FindBridges", "NextFaceVertex", "BuildBoundaryKey", "MaxInputSegments", "MaxSubdividedEdges"):
        if needle not in text: errors.append("room boundary graph guard missing: " + needle)
    if "signedArea > minimumArea" not in text: errors.append("room boundary discovery must keep only positive bounded faces above minimum area")
    if "var stack = new Stack<int>();" not in text or "void Visit(" in text:
        errors.append("room boundary bridge detection must remain iterative so large graphs cannot overflow the call stack")
    if "BuildSourceLookup" not in text or "IReadOnlyDictionary<string, ISet<string>> sourceLookup" not in text:
        errors.append("room boundary source evidence lookup must avoid scanning all edges during each face step")
    if "SegmentBounds" not in text or "other.Overlaps(current)" not in text:
        errors.append("room boundary pair subdivision must retain the tolerance-aware broad-phase bounds rejection")

bulge = ROOT / "src/QS3D.Core/Geometry/BulgeArcTessellator.cs"
if bulge.exists():
    text = bulge.read_text(encoding="utf-8")
    for needle in ("4d * Math.Atan(bulge)", "maximumSagitta", "MaxSegments", "MaximumSegmentAngle", "centerOffset", "sagittaRatio", "4d * Math.Asin"):
        if needle not in text: errors.append("bulge arc tessellation guard missing: " + needle)

room_reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs"
if room_reader.exists():
    text = room_reader.read_text(encoding="utf-8")
    for needle in (
        "CadUnitService.GetPolicy", "GetBulgeAt", "BulgeArcTessellator.Tessellate", "arcSagittaM", "entity.IsErased",
        "planarityToleranceM", "entity is Arc arc", "ARC plan-view có normal +Z", "RequireElevation", "polyline.Normal"
    ):
        if needle not in text: errors.append("room boundary CAD reader guard missing: " + needle)

room_command = ROOT / "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs"
if room_command.exists():
    text = room_command.read_text(encoding="utf-8")
    if 'BoundaryMode"] = "AutoNetwork"' not in text and "AutoRoomLifecycle.BoundaryModeAutoNetwork" not in text:
        errors.append("QS3DROOMAUTO workflow missing: AutoNetwork boundary mode assignment")
    for needle in (
        "BoundarySourceHandles", "BoundaryArcSagittaM", "RoomBoundaryArcSagittaM", "AuditTrail.ForProject", "RegeneratorCatalog.CreateDefault",
        "ReadCurrentSelection(document, arcSagitta, tolerance, splineChord)", "LINE, POLYLINE, ARC hoặc SPLINE plan-view"
    ):
        if needle not in text: errors.append("QS3DROOMAUTO workflow missing: " + needle)
    if "SourceHandles.Add" in text: errors.append("auto-room discovery must not claim wall/source handles as Room semantic ownership")
    if "ProjectStateSnapshot.Capture(project)" not in text or "rollback.Restore(project)" not in text:
        errors.append("QS3DROOMAUTO must rollback semantic/audit changes when regeneration fails")

room_hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if room_hub.exists() and 'Tag="QS3DROOMAUTO"' not in room_hub.read_text(encoding="utf-8"):
    errors.append("Full Domain Hub does not expose QS3DROOMAUTO")
room_ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
if room_ribbon.exists():
    text = room_ribbon.read_text(encoding="utf-8")
    if 'Button("Phòng Auto", "QS3DROOMAUTO")' not in text:
        errors.append("Ribbon does not expose QS3DROOMAUTO")

completion = ROOT / "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs"
if completion.exists():
    text = completion.read_text(encoding="utf-8")
    for needle in ("StairQuantities();", "RailingQuantities();", "EarthworkQuantities();", "CsvIsExcelSafeAndFinite();", "VietnameseRecognition();"):
        if needle not in text: errors.append("completion regression coverage missing: " + needle)

room_smoke = ROOT / "tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs"
if room_smoke.exists():
    text = room_smoke.read_text(encoding="utf-8")
    for needle in ("RectangleBoundary();", "TjunctionCreatesAdjacentRooms();", "EndpointToleranceClosesGap();", "DanglingBridgeIsIgnored();", "LongDanglingChainIsIgnored();", "SparseBroadPhasePreservesRoom();", "DuplicateSegmentsKeepSourceEvidence();", "BulgeSemicircleTessellation();", "BulgeDirectionMirrors();", "CurvedRoomBoundary();", "LargeRadiusTinySagittaHonorsLimit();", "InvalidCoordinatesRejected();", "InvalidBulgeToleranceRejected();"):
        if needle not in text: errors.append("room boundary regression coverage missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists():
    text = registration.read_text(encoding="utf-8")
    if "CompletionRegressionSmoke.Run();" not in text: errors.append("CompletionRegressionSmoke is not registered")
    if "RoomBoundaryRegressionSmoke.Run();" not in text: errors.append("RoomBoundaryRegressionSmoke is not registered")

package = (ROOT / "scripts/package-v25.ps1").read_text(encoding="utf-8")
for needle in ("COMMANDS.txt", "SHA256SUMS.txt", "Get-AuthenticodeSignature", "install-v25-autoload.ps1", "uninstall-v25-autoload.ps1"):
    if needle not in package: errors.append("V25 package safety/wiring missing: " + needle)

installer = (ROOT / "scripts/install-v25-autoload.ps1").read_text(encoding="utf-8")
for needle in ("HKCU:\\Software\\Bricsys\\BricsCAD", "Applications\\QS3D", "LoadCtrls", "Loader", "Get-FileHash", "Get-AuthenticodeSignature", "RequireSigned", "Get-Process -Name bricscad", "SupportsShouldProcess"):
    if needle not in installer: errors.append("V25 DemandLoad installer guard missing: " + needle)
if re.search(r'(?i)SECURELOAD\s*[=:]|setvar[^\n]*SECURELOAD', installer):
    errors.append("V25 installer must not lower SECURELOAD")

uninstaller = (ROOT / "scripts/uninstall-v25-autoload.ps1").read_text(encoding="utf-8")
for needle in ("Applications\\QS3D", "Get-Process -Name bricscad", "SupportsShouldProcess", "LOCALAPPDATA"):
    if needle not in uninstaller: errors.append("V25 DemandLoad uninstaller guard missing: " + needle)

for workflow_name in ("ci.yml", "bricscad-v25.yml"):
    workflow = (ROOT / ".github/workflows" / workflow_name).read_text(encoding="utf-8")
    if "python scripts/preflight-full-domain.py" not in workflow and "python scripts/preflight-all.py" not in workflow:
        errors.append(workflow_name + ": full-domain/release preflight is not wired")

print("QS3D full-domain/release preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: full-domain files, unique commands, structural quantities/native mass adapters, BBS CSV safety, tolerance-aware room broad-phase with planar LINE/POLYLINE/ARC/SPLINE discovery/rollback/UI wiring, DemandLoad packaging/install guards and regression registration are present.")
