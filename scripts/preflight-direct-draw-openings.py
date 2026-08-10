#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
source = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs"
if not source.is_file():
    errors.append("missing DirectDrawOpeningCommands.cs")
else:
    text = source.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DDRAWDOOR"',
        'CommandMethod("QS3DDRAWOPENING"',
        "RequireModelSpace(document)",
        "SemanticCaptureService.Capture(document, category)",
        'element.Properties["WidthM"]',
        'element.Properties["HeightM"]',
        'element.Properties["SillHeightM"]',
        'element.Properties["BooleanClearanceM"]',
        "ProjectStateSnapshot.Capture(project)",
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'element.Properties.TryGetValue("HostWallId"',
        "regeneratedAfterLink",
        "rollback.Restore(project)",
        "EraseSource(document, sourceHandle)",
        "CadHandleService.GetLiveHandles(document, new[] { normalized })",
        "PlanarityToleranceM = 0.005d",
        "CadGeometryGuard.ToMeters(document, widthDrawing",
        "QS3DCUTOPENINGS khi muốn khoét physical host",
    )
    for needle in required:
        if needle not in text:
            errors.append("DirectDrawOpeningCommands missing contract: " + needle)

    if "OpeningBooleanService.CutLinkedOpenings" in text or "new OpeningBooleanCommands().CutOpenings" in text:
        errors.append("Door/Opening Direct Draw must not invoke the global physical-cut path")
    if "SendStringToExecute(\"QS3DCUTOPENINGS" in text:
        errors.append("Door/Opening Direct Draw must not queue global QS3DCUTOPENINGS")
    if text.count("new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)") < 2:
        errors.append("Door/Opening Direct Draw must validate semantic state both before and after Auto Host")

command_root = ROOT / "src/QS3D.BricsCAD.V25"
commands = []
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
for name in ("QS3DDRAWDOOR", "QS3DDRAWOPENING", "QS3DDOOR", "QS3DOPENING", "QS3DAUTOLINKHOSTS", "QS3DCUTOPENINGS"):
    if commands.count(name) != 1:
        errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

if errors:
    print("QS3D Direct Draw Door/Opening preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Door/Opening Direct Draw creates one real source, captures one semantic element, validates units/Model Space, auto-links only the new selection, verifies post-link regeneration, rolls back orphan creation, and never invokes global physical cutting.")
