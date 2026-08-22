#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
service = root / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceCatalogImportService.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceCatalogCommands.cs"
project_tools = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"

errors = []
for path in (service, command, project_tools):
    if not path.exists():
        errors.append(f"missing required source: {path.relative_to(root)}")

if not errors:
    s = service.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    ui = project_tools.read_text(encoding="utf-8")

    required_service = [
        "ZoneCollision = InterchangeExistingIdentityAction.UseSourceSemanticData",
        "FloorCollision = InterchangeExistingIdentityAction.UseSourceSemanticData",
        "FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData",
        "ElementCollision = InterchangeExistingIdentityAction.KeepTarget",
        "SourceHandles = InterchangeSourceHandlePolicy.Discard",
        "using (document.LockDocument())",
        "var lockedProject = InterchangeMutationTargetGuard.RequireExact(",
        "var lockedInvalidationTargets = ExpandInvalidationTargets(",
        "rollback = ProjectStateSnapshot.Capture(lockedProject)",
        "GeneratedDependentGeometryInvalidator.Prepare(",
        "lockedProject,",
        "lockedInvalidationTargets);",
        "ApplyCatalogState(lockedProject, prepared.Source, prepared.Resolution)",
        "ApplyNewElementsOnly(lockedProject, prepared.Source, prepared.Resolution)",
        "invalidation.CommitMetadata();",
        "transaction.Commit();",
        "rollback.Restore(project)",
        "CollectInitialAffectedElements",
        "x.ZoneId",
        "x.FloorId",
        "x.FamilyId",
        "GetDirectDependents",
        "HostWallId",
        "target.Name = snapshot.Name;",
        "target.ElevationM = snapshot.ElevationM;",
        "InterchangeFamilySemanticApplier.Add(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
        "InterchangeFamilySemanticApplier.Replace(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
        "ApplyNewElementsOnly",
        "if (action == InterchangeImportResolutionAction.KeepTarget) continue;",
        "ProjectInterchangeKeepTargetImporter.Plan(project, json)",
        "ProjectContextCoordinator.RequireBackingStoreUnchanged(",
    ]
    for needle in required_service:
        if needle not in s:
            errors.append(f"catalog service missing atomic/policy contract: {needle}")

    prepare_index = s.find("GeneratedDependentGeometryInvalidator.Prepare")
    commit_index = s.find("transaction.Commit();")
    if min(prepare_index, commit_index) < 0 or prepare_index > commit_index:
        errors.append("catalog locked native invalidation must be prepared before CAD commit")
    if s.index("invalidation.CommitMetadata();") > s.index("transaction.Commit();"):
        errors.append("catalog generated ownership metadata must clear before CAD commit")
    if s.count("ProjectStateSnapshot.Capture(lockedProject)") != 1:
        errors.append("catalog service must own exactly one locked semantic rollback snapshot")

    catalog_section = re.search(r"private static void ApplyCatalogState\(.*?\n        private static void ApplyNewElementsOnly", s, re.S)
    if not catalog_section:
        errors.append("catalog ApplyCatalogState section not found")
    elif "target.Properties.Clear();" in catalog_section.group(0):
        errors.append("catalog Family replacement must not clear Family.Properties directly; use inheritance-aware applier")

    forbidden_service = [
        "QS3DBUILD3D",
        ".SourceHandles.Add(",
        ".SourceHandles.Clear()",
        "target.FamilyId = snapshot.FamilyId",
        "target.FloorId = snapshot.FloorId",
        "target.ZoneId = snapshot.ZoneId",
    ]
    for needle in forbidden_service:
        if needle in s:
            errors.append(f"catalog service crosses protected Element/source/rebuild boundary: {needle}")

    if c.count('[CommandMethod("QS3DINTERCHANGEUSESOURCECATALOG"') != 1:
        errors.append("QS3DINTERCHANGEUSESOURCECATALOG must be registered exactly once in command source")
    for needle in [
        "InterchangeUseSourceCatalogImportService.Plan(project, json)",
        "InterchangeUseSourceCatalogImportService.Import(document, confirmedProject, json)",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "MessageBoxButton.YesNo",
        "Floor: thay tên + elevation theo source",
        "Element trùng ID KHÔNG bị replace",
        "Không nhận incoming source CAD handles làm ownership",
        "Rebuild explicit",
    ]:
        if needle not in c:
            errors.append(f"catalog command missing guarded UX contract: {needle}")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEUSESOURCECATALOG"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEUSESOURCECATALOG registration count must be 1, got {registrations}")

    if ui.count('Tag="QS3DINTERCHANGEUSESOURCECATALOG"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEUSESOURCECATALOG exactly once")
    for needle in [
        "Nạp Snapshot (Replace Catalog semantic)",
        "Zone/Floor/Family",
        "Element collision giữ target",
        "rebuild explicit",
    ]:
        if needle not in ui:
            errors.append(f"Project Tools missing catalog replacement UX contract: {needle}")

if errors:
    print("preflight-interchange-use-source-catalog: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-use-source-catalog: PASS")
print("UseSource catalog replacement rebinds the exact locked project, recomputes and prepares native invalidation before semantic mutation/CAD commit, preserves Family inheritance/overrides, keeps existing Element collisions target-authoritative, discards incoming handles, and leaves rebuild explicit.")
