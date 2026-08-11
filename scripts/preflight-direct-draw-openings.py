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
        'createdElement.SetProperty("WidthM"',
        'createdElement.SetProperty("HeightM"',
        'createdElement.SetProperty("SillHeightM"',
        'createdElement.SetProperty("BottomOffsetM"',
        'createdElement.SetProperty("BooleanClearanceM"',
        "ProjectStateSnapshot.Capture(project)",
        "EnsureActive(document, \"Direct Draw \" + label + \" / Auto Host\")",
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'createdElement.Properties.TryGetValue("HostWallId"',
        "regenerated += new RegenerationEngine",
        "EraseSource(document, sourceId)",
        "rollback.Restore(project)",
        "CadHandleService.GetLiveHandles(document, new[] { handle })",
        "PlanarityToleranceM = 0.005d",
        "CadGeometryGuard.ToMeters(document, widthDrawing",
        "DirectDrawProjectPreviewContext.Capture(document)",
        "FamilyPositiveNumber(defaultsProject!, category, \"HeightM\"",
        "FamilyNonNegativeNumber(defaultsProject!, category, \"BottomOffsetM\"",
        "FamilyNonNegativeNumber(defaultsProject!, category, \"SillHeightM\"",
        "FamilyNonNegativeNumber(defaultsProject!, category, \"BooleanClearanceM\"",
        "FamilyConfiguredNumber",
        "PreferredFamily(project, category)",
        "Sửa Family trước khi Direct Draw.",
        "default phải là số hữu hạn > 0",
        "default phải là số hữu hạn >= 0",
        "FinalizeUi(document, sourceId, label, widthM, hostId, regenerated)",
        "UI sync warning",
        "QS3DCUTSELECTEDOPENINGS khi muốn khoét đúng Cửa/Lỗ đang chọn",
    )
    for needle in required:
        if needle not in text:
            errors.append("DirectDrawOpeningCommands missing contract: " + needle)

    forbidden = (
        "return value > 0d ? value : fallback;",
        "defaultValue = 0d;",
        "FamilyFiniteNumber(",
        "FamilyNumber(",
        'createdElement.Properties["WidthM"] =',
        'createdElement.Properties["HeightM"] =',
    )
    for token in forbidden:
        if token in text:
            errors.append("Door/Opening Direct Draw contains a forbidden lifecycle shortcut: " + token)

    if "OpeningBooleanService.CutLinkedOpenings" in text or "new OpeningBooleanCommands().CutOpenings" in text:
        errors.append("Door/Opening Direct Draw must not invoke the global physical-cut path")
    if "SendStringToExecute(\"QS3DCUTOPENINGS" in text:
        errors.append("Door/Opening Direct Draw must not queue global QS3DCUTOPENINGS")
    first_subset = '.RegenerateDirtySubset(project, new[] { createdElementId });'
    second_subset = '.RegenerateDirtySubset(project, new[] { createdElementId, hostId });'
    if first_subset not in text or second_subset not in text or text.find(first_subset) >= text.find(second_subset):
        errors.append("Door/Opening Direct Draw must validate only the authored opening, then the opening+host closure after Auto Host")
    if ".RegenerateDirty(project)" in text:
        errors.append("Door/Opening Direct Draw must not clean unrelated dirty semantic elements")
    if text.count("Sửa Family trước khi Direct Draw.") < 3:
        errors.append("Configured positive/non-negative Door/Opening Family values must fail closed with a repair message")

    erase = text.find("EraseSource(document, sourceId)")
    restore = text.find("rollback.Restore(project)")
    finalize = text.find("FinalizeUi(document, sourceId, label, widthM, hostId, regenerated)")
    if min(erase, restore, finalize) < 0 or not (erase < restore < finalize):
        errors.append("Door/Opening rollback must clean operation-owned CAD before project restore, and UI sync must happen only after successful operation scope")

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
    for needle in ('Button("Vẽ Cửa", "QS3DDRAWDOOR")', 'Button("Vẽ Lỗ Mở", "QS3DDRAWOPENING")'):
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
print("PASS: Door/Opening Direct Draw uses canonical SetProperty writes, active-DWG guards, operation-owned ObjectId cleanup before project restore, post-link semantic verification and non-destructive post-commit UI sync; it exposes Ribbon/Hub actions and never invokes global physical cutting.")
