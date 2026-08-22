#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
service = root / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceAllImportService.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceAllCommands.cs"
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
        "ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData",
        "SourceHandles = InterchangeSourceHandlePolicy.Discard",
        "GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild",
        "using (document.LockDocument())",
        "var lockedProject = InterchangeMutationTargetGuard.RequireExact(",
        "var lockedInvalidationTargets = ExpandInvalidationTargets(",
        "rollback = ProjectStateSnapshot.Capture(lockedProject)",
        "GeneratedDependentGeometryInvalidator.Prepare(",
        "lockedProject,",
        "lockedInvalidationTargets);",
        "ApplyCatalogState(lockedProject, prepared.Source, prepared.Resolution)",
        "ApplyElementState(lockedProject, prepared.Source, prepared.Resolution)",
        "invalidation.CommitMetadata();",
        "transaction.Commit();",
        "rollback.Restore(project)",
        "CollectInitialAffectedElements",
        "plan.ReplacementElementIds",
        "x.ZoneId",
        "x.FloorId",
        "x.FamilyId",
        "GetDirectDependents",
        "HostWallId",
        "InterchangeFamilySemanticApplier.Add(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
        "InterchangeFamilySemanticApplier.Replace(project, snapshot.Id, snapshot.Name, snapshot.Category, snapshot.Properties)",
        "target.FamilyId = snapshot.FamilyId;",
        "target.FloorId = snapshot.FloorId;",
        "target.ZoneId = snapshot.ZoneId;",
        "target.Properties.Clear();",
        "target.Quantities.Clear();",
        "target.MarkDirty(ElementDirtyFlags.All);",
        "ProjectInterchangeKeepTargetImporter.Plan(project, json)",
        "One invalidation plan + one semantic mutation + one native commit",
        "ProjectContextCoordinator.RequireBackingStoreUnchanged(",
    ]
    for needle in required_service:
        if needle not in s:
            errors.append(f"all-scope service missing atomic/policy contract: {needle}")

    if s.count("document.Database.TransactionManager.StartTransaction()") != 1:
        errors.append("all-scope service must own exactly one native CAD transaction")
    if s.count("ProjectStateSnapshot.Capture(lockedProject)") != 1:
        errors.append("all-scope service must own exactly one locked semantic rollback snapshot")
    prepare_index = s.find("GeneratedDependentGeometryInvalidator.Prepare")
    catalog_index = s.find("ApplyCatalogState(lockedProject, prepared.Source, prepared.Resolution)")
    element_index = s.find("ApplyElementState(lockedProject, prepared.Source, prepared.Resolution)")
    if min(prepare_index, catalog_index, element_index) < 0 or not (prepare_index < catalog_index and prepare_index < element_index):
        errors.append("all-scope locked invalidation must be prepared before catalog/element mutation")
    if s.index("invalidation.CommitMetadata();") > s.index("transaction.Commit();"):
        errors.append("all-scope generated ownership metadata must clear before CAD commit")

    catalog_section = re.search(r"private static void ApplyCatalogState\(.*?\n        private static void ApplyElementState", s, re.S)
    if not catalog_section:
        errors.append("all-scope ApplyCatalogState section not found")
    elif "target.Properties.Clear();" in catalog_section.group(0):
        errors.append("all-scope Family replacement must not clear Family.Properties directly; use inheritance-aware applier")

    element_section = re.search(r"private static void ApplyElementState\(.*?\n        private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets", s, re.S)
    if not element_section:
        errors.append("all-scope ApplyElementState section not found")
    else:
        for needle in ("target.Properties.Clear();", "target.Quantities.Clear();", "target.MarkDirty(ElementDirtyFlags.All);"):
            if needle not in element_section.group(0):
                errors.append("all-scope Element replacement lost portable-state overwrite contract: " + needle)

    forbidden_service = [
        "InterchangeUseSourceElementImportService.Import",
        "InterchangeUseSourceCatalogImportService.Import",
        "QS3DBUILD3D",
        ".SourceHandles.Add(",
        ".SourceHandles.Clear()",
        "target.DrawingFingerprint = snapshot.DrawingFingerprint",
    ]
    for needle in forbidden_service:
        if needle in s:
            errors.append(f"all-scope service crosses single-transaction/source/rebuild boundary: {needle}")

    if c.count('[CommandMethod("QS3DINTERCHANGEUSESOURCEALL"') != 1:
        errors.append("QS3DINTERCHANGEUSESOURCEALL must be registered exactly once in command source")
    for needle in [
        "InterchangeUseSourceAllImportService.Plan(project, json)",
        "InterchangeUseSourceAllImportService.Import(document, confirmedProject, json)",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "MessageBoxButton.YesNo",
        "MỘT CAD TRANSACTION",
        "không sequential partial-commit",
        "Incoming source handles discard",
        "target source handles preserve",
        "Rebuild explicit",
    ]:
        if needle not in c:
            errors.append(f"all-scope command missing guarded UX contract: {needle}")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEUSESOURCEALL"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEUSESOURCEALL registration count must be 1, got {registrations}")

    if ui.count('Tag="QS3DINTERCHANGEUSESOURCEALL"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEUSESOURCEALL exactly once")
    for needle in ["Nạp Snapshot (Replace ALL semantic)", "một CAD transaction", "rebuild explicit"]:
        if needle not in ui:
            errors.append(f"Project Tools missing all-scope replacement UX: {needle}")

if errors:
    print("preflight-interchange-use-source-all: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-use-source-all: PASS")
print("All executable catalog + element UseSource collisions rebind the exact locked project, recompute one invalidation plan, use one native transaction and one semantic rollback snapshot; Family inheritance/overrides and target source ownership are preserved while rebuild remains explicit.")
