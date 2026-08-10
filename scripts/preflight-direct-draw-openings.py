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
        "EnsureActive(document, \"Direct Draw \" + label + \" / QS3DAUTOLINKHOSTS\")",
        "SemanticCaptureService.Capture(document, category)",
        'createdElement.SetProperty("WidthM"',
        'createdElement.SetProperty("HeightM"',
        'createdElement.SetProperty("SillHeightM"',
        'createdElement.SetProperty("BottomOffsetM"',
        'createdElement.SetProperty("BooleanClearanceM"',
        "ProjectStateSnapshot.Capture(project)",
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'createdElement.Properties.TryGetValue("HostWallId"',
        "regeneratedAfterLink",
        "EraseSource(document, sourceId, sourceHandle)",
        "rollback.Restore(project)",
        "FinalizeUi(document, label, sourceId, widthM, hostId",
        "sourceId.IsNull || !sourceId.IsValid",
        "từ chối erase theo textual handle để tránh xóa nhầm CAD",
        "CadHandleService.GetLiveHandles(document, new[] { handle })",
        "UI sync warning",
        "PlanarityToleranceM = 0.005d",
        "CadGeometryGuard.ToMeters(document, widthDrawing",
        "FamilyPositiveNumber(project, category, \"HeightM\"",
        "FamilyNonNegativeNumber(project, category, \"BottomOffsetM\"",
        "FamilyNonNegativeNumber(project, category, \"SillHeightM\"",
        "FamilyNonNegativeNumber(project, category, \"BooleanClearanceM\"",
        "FamilyConfiguredNumber",
        "PreferredFamily(project, category)",
        "Sửa Family trước khi Direct Draw.",
        "default phải là số hữu hạn > 0",
        "default phải là số hữu hạn >= 0",
        "QS3DCUTOPENINGS khi muốn khoét physical host",
    )
    for needle in required:
        if needle not in text:
            errors.append("DirectDrawOpeningCommands missing contract: " + needle)

    forbidden = (
        "return value > 0d ? value : fallback;",
        "defaultValue = 0d;",
        "FamilyFiniteNumber(",
        "FamilyNumber(",
        'element.Properties["WidthM"]',
        'element.Properties["HeightM"]',
        'element.Properties["SillHeightM"]',
        'element.Properties["BottomOffsetM"]',
        'element.Properties["BooleanClearanceM"]',
    )
    for token in forbidden:
        if token in text:
            errors.append("Door/Opening Direct Draw contains stale/unsafe authoring behavior: " + token)

    if "OpeningBooleanService.CutLinkedOpenings" in text or "new OpeningBooleanCommands().CutOpenings" in text:
        errors.append("Door/Opening Direct Draw must not invoke the global physical-cut path")
    if "SendStringToExecute(\"QS3DCUTOPENINGS" in text:
        errors.append("Door/Opening Direct Draw must not queue global QS3DCUTOPENINGS")
    if text.count("new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)") < 2:
        errors.append("Door/Opening Direct Draw must validate semantic state both before and after Auto Host")
    if text.count("Sửa Family trước khi Direct Draw.") < 3:
        errors.append("Configured positive/non-negative Door/Opening Family values must fail closed with a repair message")
    if text.count("createdElement.SetProperty(") < 5:
        errors.append("Door/Opening geometry/boolean parameters must flow through canonical ProjectElement.SetProperty semantics")

    create = text.find("sourceId = CreateLine(document, start, end)")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    pre_regen = text.find("regeneratedBeforeLink = new RegenerationEngine")
    active_check = text.find('EnsureActive(document, "Direct Draw " + label + " / QS3DAUTOLINKHOSTS")')
    auto_host = text.find("new AutoHostLinkCommands().AutoLinkHosts()")
    host_verify = text.find('createdElement.Properties.TryGetValue("HostWallId"')
    post_regen = text.find("regeneratedAfterLink = new RegenerationEngine")
    erase = text.find("EraseSource(document, sourceId, sourceHandle)")
    restore = text.find("rollback.Restore(project)")
    finalize = text.find("FinalizeUi(document, label, sourceId, widthM, hostId")
    if min(create, capture, pre_regen, active_check, auto_host, host_verify, post_regen, erase, restore, finalize) < 0:
        errors.append("Door/Opening Direct Draw lifecycle ordering tokens are incomplete")
    elif not (create < capture < pre_regen < active_check < auto_host < host_verify < post_regen < erase < restore < finalize):
        errors.append("Door/Opening lifecycle must create/capture/regenerate -> re-check active DWG -> Auto Host/verify/regenerate; failure erases exact source before project restore; UI finalization runs after rollback-critical scope")

    erase_body = text.split("private static void EraseSource", 1)[-1].split("private static void FinalizeUi", 1)[0]
    if "sourceId" not in erase_body or "textual handle" not in erase_body:
        errors.append("Door/Opening rollback must use the exact operation-created ObjectId and refuse textual-handle fallback")
    if "transaction.Commit();" not in erase_body or "CadHandleService.GetLiveHandles" not in erase_body:
        errors.append("Door/Opening rollback must commit source erase and verify the source no longer remains live")

    finalize_body = text.split("private static void FinalizeUi", 1)[-1].split("private static double? PromptPositiveMeters", 1)[0]
    if "try" not in finalize_body or "UI sync warning" not in finalize_body:
        errors.append("Door/Opening post-commit UI synchronization must be best-effort and non-destructive")

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
print("PASS: Door/Opening Direct Draw creates one real source, uses canonical SetProperty semantics, rejects invalid configured Family numerics, validates Model Space/units and the originating DWG before Auto Host, verifies host/regeneration, erases the exact operation source before semantic rollback, keeps post-commit UI failures non-destructive, exposes Ribbon/Hub actions, and never invokes global physical cutting.")
