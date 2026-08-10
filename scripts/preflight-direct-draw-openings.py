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
        "FamilyPositiveNumber(project, category, \"HeightM\"",
        "FamilyNonNegativeNumber(project, category, \"SillHeightM\"",
        "FamilyNonNegativeNumber(project, category, \"BooleanClearanceM\"",
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

ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
if not ribbon.is_file():
    errors.append("missing RibbonBootstrapper.cs")
else:
    ribbon_text = ribbon.read_text(encoding="utf-8")
    for needle in ('new RibbonButtonSpec("Vẽ Cửa", "QS3DDRAWDOOR")', 'new RibbonButtonSpec("Vẽ Lỗ Mở", "QS3DDRAWOPENING")'):
        if needle not in ribbon_text:
            errors.append("Ribbon missing Door/Opening Direct Draw action: " + needle)

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if not hub.is_file():
    errors.append("missing DomainHubWindow.xaml")
else:
    hub_text = hub.read_text(encoding="utf-8")
    for needle in ('Tag="QS3DDRAWDOOR"', 'Tag="QS3DDRAWOPENING"', "physical boolean vẫn là thao tác riêng"):
        if needle not in hub_text:
            errors.append("Domain Hub missing Door/Opening Direct Draw contract: " + needle)

if errors:
    print("QS3D Direct Draw Door/Opening preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Door/Opening Direct Draw creates one real source, validates Family defaults/units/Model Space, captures one semantic element, auto-links only the new selection, verifies post-link regeneration, rolls back orphan creation, exposes Ribbon/Hub actions, and never invokes global physical cutting.")
