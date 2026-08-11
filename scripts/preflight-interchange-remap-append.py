#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
planner = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
importer = root / "src/QS3D.Core/Export/ProjectInterchangeRemapAppendImporter.cs"
policy = root / "src/QS3D.Core/Export/ProjectInterchangeSemanticReferencePolicy.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs"
dry_command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapCommands.cs"
planner_smoke = root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapPlannerSmoke.cs"
level_smoke = root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapLevelReferenceSmoke.cs"

errors = []
for path in (planner, importer, policy, command, dry_command, planner_smoke, level_smoke):
    if not path.exists():
        errors.append(f"missing remap-append contract source: {path.relative_to(root)}")

if not errors:
    p = planner.read_text(encoding="utf-8")
    i = importer.read_text(encoding="utf-8")
    r = policy.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    d = dry_command.read_text(encoding="utf-8")
    ps = planner_smoke.read_text(encoding="utf-8")
    ls = level_smoke.read_text(encoding="utf-8")

    required_importer = [
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        "var plan = Plan(target, json);",
        "ValidateExecutionSafety(source, plan);",
        "ProjectStateSnapshot.Capture(target)",
        "rollback.Restore(target)",
        "new AggregateException(operationError, restoreError)",
        "ProjectZoneService.Create(target, item.TargetId, item.TargetName)",
        "ProjectFloorService.Create(target, item.TargetId, item.TargetName, snapshot.ElevationM)",
        "ProjectFamilyService.Create(target, item.TargetId, item.TargetName, snapshot.Category)",
        "plan.Remap.MapId(InterchangeRemapIdentityKind.Element, snapshot.Id)",
        "MapOptional(plan.Remap, InterchangeRemapIdentityKind.Family",
        "MapOptional(plan.Remap, InterchangeRemapIdentityKind.Floor",
        "MapOptional(plan.Remap, InterchangeRemapIdentityKind.Zone",
        "plan.Remap.MapId(InterchangeRemapIdentityKind.Element, dependency)",
        "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference)",
        "MapPropertyReference(plan.Remap, reference, property.Value, ref rewrites)",
        "ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)",
        "addedElementIds.Add(added.Id)",
        "ValidateCombinedTarget(target, addedElementIds)",
        "ValidateRegisteredPropertyReferences(target, element)",
        "ValidateLevelReferenceConsistency(target, element)",
        "ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences",
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelIdKey",
        "ElementVerticalPlacementService.ReadLevelOffset",
        "has TopLevelId without BottomLevelId",
        "has a level offset without its level reference",
        "has TopLevelOffsetM without TopLevelId",
        "top level elevation must be above bottom level elevation",
        "AddFinite(",
        "IsImportedOwnershipMetadata(property.Key)",
        'k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        'k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0',
        "DrawingFingerprint = string.Empty",
        "added.MarkDirty(ElementDirtyFlags.All)",
        "graph.Rebuild(target.Elements)",
        "graph.TopologicalDirtyOrder(target.Elements)",
        "No imported handle/fingerprint became target DWG ownership",
        "EvaluateCompatibility(target, source)",
        "CompatibilityBlockers",
        "public int BlockerCount",
        "Remap.CanAppendAsNew && CompatibilityBlockers.Count == 0",
        "private const int MaxZones = 2000;",
        "private const int MaxFloors = 2000;",
        "private const int MaxFamilies = 10000;",
        "private const int FamilyMaxPropertyKeyLength = 120;",
        "private const int FamilyMaxPropertyValueLength = 1000;",
        "EnsureFamilyPropertyRuntimeCompatible",
        "Import As New will not truncate semantic data",
    ]
    for needle in required_importer:
        if needle not in i:
            errors.append("remap append importer missing atomic/ownership/compatibility/reference contract: " + needle)

    plan_method = re.search(
        r"public static ProjectInterchangeRemapAppendPlan Plan\(ProjectState target, string json\)(.*?)public static ProjectInterchangeRemapAppendResult Import",
        i,
        re.S,
    )
    if not plan_method:
        errors.append("unable to locate remap append Plan method")
    else:
        plan_body = plan_method.group(1)
        if "ValidateExecutionSafety" in plan_body:
            errors.append("remap append Plan must remain inspectable for blocked plans; execution safety belongs in Import")
        if "return new ProjectInterchangeRemapAppendPlan(remap, ownershipProperties, compatibilityBlockers)" not in plan_body:
            errors.append("remap append Plan must return blocked/ready compatibility metadata without mutation")

    required_planner = [
        "foreach (var family in source.Families.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))",
        "foreach (var property in family.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))",
        'OwnerElementSourceId = "Family " + family.Id',
        "Family property looks like a semantic identity/reference",
        "if (IsImportedOwnershipMetadata(property.Key)) continue;",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(k)",
        'k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        'k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0',
        "source.Families.Select(x => new NamedIdentity(x.Id, x.Name, x.Category.ToString()))",
        "target.Families.Select(x => new NamedIdentity(x.Id, x.Name, x.Category.ToString()))",
        "NameKey(x.NameScope, x.Name)",
        "NextName(sourceName, sourceItem.NameScope, occupiedNames, maxNameLength)",
        "public string NameScope { get; }",
        "private const int ZoneMaxIdLength = 64;",
        "private const int ZoneMaxNameLength = 120;",
        "private const int FloorMaxIdLength = 64;",
        "private const int FloorMaxNameLength = 120;",
        "private const int FamilyMaxIdLength = 80;",
        "private const int FamilyMaxNameLength = 160;",
        "private const int ElementMaxIdLength = 128;",
        "sourceId.Length > maxIdLength",
        "sourceName.Length > maxNameLength",
        "assignedIds.Contains(sourceId)",
        "assignedNames.Contains(sourceNameKey)",
        "NextId(sourceId, occupiedIds, maxIdLength)",
        "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference)",
        "MapFor(reference.Kind, zoneMap, floorMap, familyMap, elementMap)",
        '"Property" + reference.Kind + "Id"',
        "does not resolve inside the source snapshot",
        "ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)",
    ]
    for needle in required_planner:
        if needle not in p:
            errors.append("remap planner missing family/ownership/name-scope/runtime-bound/reference contract: " + needle)

    required_policy = [
        'public const string HostWallIdKey = "HostWallId"',
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelIdKey",
        "InterchangeRemapIdentityKind.Element",
        "InterchangeRemapIdentityKind.Floor",
        "TryGetPropertyReference",
        "KnownPropertyReferences",
        "LooksLikeSemanticReferenceKey",
    ]
    for needle in required_policy:
        if needle not in r:
            errors.append("semantic reference policy missing portable relation contract: " + needle)

    reference_suffixes = ["Id", "Ids", "Ref", "Refs", "RefId", "RefIds"]
    for suffix in reference_suffixes:
        needle = f'key.EndsWith("{suffix}", StringComparison.OrdinalIgnoreCase)'
        if needle not in r:
            errors.append("central semantic reference policy missing suffix: " + suffix)
    if "EndsWith(" in p:
        errors.append("remap planner must not maintain a second ID/ref suffix policy")
    if "EndsWith(" in i:
        errors.append("remap executor must not maintain a second ID/ref suffix policy")
    if "private const string HostWallIdKey" in p or "private const string HostWallIdKey" in i:
        errors.append("HostWallId registration must live only in ProjectInterchangeSemanticReferencePolicy")

    required_planner_smoke = [
        "BlockedAppendPlanRemainsInspectableAndImportFailsClosed",
        "OverLimitCatalogIdentitiesAreBoundedBeforeImport",
        "IncomingDuplicateNamesAreRemappedWithinBatch",
        "PortableLevelReferencesAreTypedAndRemapped",
        "RegisteredReferenceMissingFromSourceBlocksPreview",
        'Equal("L0-import", bottom.TargetReferenceId)',
        'Equal("L1-import", top.TargetReferenceId)',
    ]
    for needle in required_planner_smoke:
        if needle not in ps:
            errors.append("remap planner smoke missing regression: " + needle)

    required_level_smoke = [
        "ImportAsNewRemapsPortableLevelReferences",
        "InvalidTopOnlyLevelRelationRollsBack",
        "ProjectInterchangeRemapAppendImporter.Import(target, json)",
        'Equal("L0-import", imported.Properties[ProjectFloorService.BottomLevelIdKey])',
        'Equal("L1-import", imported.Properties[ProjectFloorService.TopLevelIdKey])',
        "Equal(0, imported.SourceHandles.Count)",
        "Equal(string.Empty, imported.DrawingFingerprint)",
        "Equal(beforeVersion, target.ChangeVersion)",
    ]
    for needle in required_level_smoke:
        if needle not in ls:
            errors.append("remap level-reference smoke missing apply/rollback regression: " + needle)

    if i.index("var plan = Plan(target, json);") > i.index("ValidateExecutionSafety(source, plan);"):
        errors.append("Import As New must build plan before execution-safety validation")
    if i.index("ValidateExecutionSafety(source, plan);") > i.index("ProjectStateSnapshot.Capture(target)"):
        errors.append("Import As New must block unsafe remaps before mutation snapshot")

    forbidden_importer = [
        ".SourceHandles.Add(",
        ".SourceHandles.Clear()",
        "snapshot.DrawingFingerprint",
        "GeneratedDependentGeometryInvalidator",
        "TransactionManager.StartTransaction",
        "QS3DBUILD3D",
    ]
    for needle in forbidden_importer:
        if needle in i:
            errors.append("semantic-only remap append crosses target/native ownership boundary: " + needle)

    required_command = [
        '[CommandMethod("QS3DINTERCHANGEREMAPAPPEND", CommandFlags.Modal)]',
        "ProjectInterchangeRemapAppendImporter.Plan(project, json)",
        "var previewChangeVersion = project.ChangeVersion;",
        "var currentProject = ProjectContextCoordinator.GetOrCreate(document);",
        "ReferenceEquals(currentProject, project)",
        "currentProject.ChangeVersion != previewChangeVersion",
        "ProjectInterchangeRemapAppendImporter.Import(currentProject, json)",
        "if (!plan.CanImport)",
        "Interchange Import As New BLOCKED",
        "plan.BlockerCount",
        "plan.Remap.OpaqueReferenceWarnings.Count",
        "plan.CompatibilityBlockers.Count",
        "runtime compatibility",
        "không truncate semantic data",
        "QS3DINTERCHANGEREMAPPLAN",
        "chưa mutate project/DWG",
        "if (plan.IdRemapCount == 0 && plan.NameRemapCount == 0)",
        "QS3DINTERCHANGEAPPEND",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "MessageBoxButton.YesNo",
        "Existing target Zone/Floor/Family/Element KHÔNG bị replace hoặc rename",
        "SourceHandles, drawing fingerprint và Generated*/PhysicalOpeningCut*/handle owner metadata không trở thành CAD ownership",
        "semantic-only import",
        "re-plan ngay trước mutation",
        "EnsureActive(document",
    ]
    for needle in required_command:
        if needle not in c:
            errors.append("remap append command missing guarded UX/freshness contract: " + needle)
    if "ProjectInterchangeRemapAppendImporter.Import(project, json)" in c:
        errors.append("confirmed remap append must mutate the re-resolved current project, not the stale preview reference")
    if "Import As New plan is not executable. Run QS3DINTERCHANGEREMAPPLAN" in c:
        errors.append("blocked remap plans should surface as normal BLOCKED status, not generic command exceptions")

    required_dry_run = [
        '[CommandMethod("QS3DINTERCHANGEREMAPPLAN", CommandFlags.Modal)]',
        "ProjectInterchangeRemapAppendImporter.Plan(project, json)",
        "var plan = appendPlan.Remap;",
        "appendPlan.CompatibilityBlockers.Count",
        "BLOCK RUNTIME",
        "appendPlan.CanImport ? \"READY\" : \"BLOCKED\"",
        "target runtime compatibility",
    ]
    for needle in required_dry_run:
        if needle not in d:
            errors.append("remap dry-run missing execution-compatibility preview contract: " + needle)

    all_cs = "\n".join(path.read_text(encoding="utf-8", errors="ignore") for path in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEREMAPAPPEND"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEREMAPAPPEND registration count must be 1, got {registrations}")

    for needle in [
        'suffix == 1 ? "-import" : "-import-" + suffix',
        'suffix == 1 ? " (Imported)" : " (Imported " + suffix + ")"',
        "public bool CanAppendAsNew => OpaqueReferenceWarnings.Count == 0;",
    ]:
        if needle not in p:
            errors.append("remap append lost canonical planner contract: " + needle)

if errors:
    print("preflight-interchange-remap-append: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-remap-append: PASS")
print("Import As New keeps blocked plans inspectable, previews runtime compatibility, binds confirmation to the reviewed project/version, uses one typed HostWall/BottomLevel/TopLevel reference registry, rejects invalid level relations before success, strips native ownership, and preserves rollback diagnostics.")
