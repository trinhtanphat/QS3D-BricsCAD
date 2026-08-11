#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
planner = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
importer = root / "src/QS3D.Core/Export/ProjectInterchangeRemapAppendImporter.cs"
policy = root / "src/QS3D.Core/Export/ProjectInterchangeSemanticReferencePolicy.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs"
planner_smoke = root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapPlannerSmoke.cs"
level_smoke = root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapLevelReferenceSmoke.cs"

errors = []
for path in (planner, importer, policy, command, planner_smoke, level_smoke):
    if not path.exists():
        errors.append(f"missing remap-append contract source: {path.relative_to(root)}")

if not errors:
    p = planner.read_text(encoding="utf-8")
    i = importer.read_text(encoding="utf-8")
    r = policy.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    ps = planner_smoke.read_text(encoding="utf-8")
    ls = level_smoke.read_text(encoding="utf-8")

    required_importer = [
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        "var plan = Plan(target, json);",
        "ValidateExecutionSafety(source, plan);",
        "ProjectStateSnapshot.Capture(target)",
        "rollback.Restore(target)",
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
        "ValidateRegisteredPropertyReferences(target, element)",
        "ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences",
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelIdKey",
        "has TopLevelId without BottomLevelId",
        "IsImportedOwnershipMetadata(property.Key)",
        'k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        'k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0',
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(k)",
        "DrawingFingerprint = string.Empty",
        "added.MarkDirty(ElementDirtyFlags.All)",
        "ValidateCombinedTarget(target)",
        "graph.Rebuild(target.Elements)",
        "graph.TopologicalDirtyOrder(target.Elements)",
        "No imported handle/fingerprint became target DWG ownership",
    ]
    for needle in required_importer:
        if needle not in i:
            errors.append("remap append importer missing atomic/ownership/rewrite contract: " + needle)

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
        "NextName(sourceItem.Name, sourceItem.NameScope, occupiedNames)",
        "public string NameScope { get; }",
        "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference)",
        "MapFor(reference.Kind, zoneMap, floorMap, familyMap, elementMap)",
        '"Property" + reference.Kind + "Id"',
        "does not resolve inside the source snapshot",
        "ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)",
    ]
    for needle in required_planner:
        if needle not in p:
            errors.append("remap planner missing family/ownership/name-scope/reference preview contract: " + needle)

    required_policy = [
        'public const string HostWallIdKey = "HostWallId"',
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelIdKey",
        "InterchangeRemapIdentityKind.Element",
        "InterchangeRemapIdentityKind.Floor",
        "TryGetPropertyReference",
        "LooksLikeSemanticReferenceKey",
    ]
    for needle in required_policy:
        if needle not in r:
            errors.append("semantic reference policy missing portable relation contract: " + needle)

    # Planner preview and executor must consume one canonical conservative ID/ref suffix policy.
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

    required_smoke = [
        "PortableLevelReferencesAreTypedAndRemapped",
        "RegisteredReferenceMissingFromSourceBlocksPreview",
        "ProjectFloorService.BottomLevelIdKey",
        "ProjectFloorService.TopLevelIdKey",
        'Equal("L0-import", bottom.TargetReferenceId)',
        'Equal("L1-import", top.TargetReferenceId)',
    ]
    for needle in required_smoke:
        if needle not in ps:
            errors.append("remap planner smoke missing typed level-reference regression: " + needle)

    required_apply_smoke = [
        "ImportAsNewRemapsPortableLevelReferences",
        "InvalidTopOnlyLevelRelationRollsBack",
        "ProjectInterchangeRemapAppendImporter.Import(target, json)",
        'Equal("L0-import", imported.Properties[ProjectFloorService.BottomLevelIdKey])',
        'Equal("L1-import", imported.Properties[ProjectFloorService.TopLevelIdKey])',
        "Equal(0, imported.SourceHandles.Count)",
        "Equal(string.Empty, imported.DrawingFingerprint)",
        "Equal(beforeVersion, target.ChangeVersion)",
    ]
    for needle in required_apply_smoke:
        if needle not in ls:
            errors.append("remap append smoke missing level-reference apply/rollback regression: " + needle)

    # Re-plan must happen before snapshot capture/mutation, not just rely on an old UI preview.
    if i.index("var plan = Plan(target, json);") > i.index("ProjectStateSnapshot.Capture(target)"):
        errors.append("Import As New must re-plan against current target before mutation snapshot")

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
        "ProjectInterchangeRemapAppendImporter.Import(project, json)",
        "if (!plan.CanImport)",
        "if (plan.IdRemapCount == 0 && plan.NameRemapCount == 0)",
        "QS3DINTERCHANGEAPPEND",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "MessageBoxButton.YesNo",
        "Existing target Zone/Floor/Family/Element KHÔNG bị replace hoặc rename",
        "Property ID/ref chưa có rewrite policy sẽ BLOCK",
        "SourceHandles, drawing fingerprint và Generated*/PhysicalOpeningCut*/handle owner metadata không trở thành CAD ownership",
        "semantic-only import",
        "re-plan ngay trước mutation",
        "EnsureActive(document",
    ]
    for needle in required_command:
        if needle not in c:
            errors.append("remap append command missing guarded UX contract: " + needle)

    all_cs = "\n".join(path.read_text(encoding="utf-8", errors="ignore") for path in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEREMAPAPPEND"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEREMAPAPPEND registration count must be 1, got {registrations}")

    # Planner must remain dry-run and executor must depend on it instead of inventing a second remap scheme.
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
print("Import As New re-plans immediately before semantic mutation, uses one typed property-reference registry for HostWall/BottomLevel/TopLevel, keeps unknown ID/ref properties fail-closed, scopes Family display-name collisions by category, strips incoming native ownership, preserves existing target identities, and rolls back semantic state on failure.")
