#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
planner = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
importer = root / "src/QS3D.Core/Export/ProjectInterchangeRemapAppendImporter.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs"

errors = []
for path in (planner, importer, command):
    if not path.exists():
        errors.append(f"missing remap-append contract source: {path.relative_to(root)}")

if not errors:
    p = planner.read_text(encoding="utf-8")
    i = importer.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")

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
        "string.Equals(property.Key, HostWallIdKey, StringComparison.OrdinalIgnoreCase)",
        "plan.Remap.MapId(InterchangeRemapIdentityKind.Element, sourceHost)",
        "LooksLikeUnregisteredSemanticReference(property.Key, property.Value)",
        "IsImportedOwnershipMetadata(property.Key)",
        'k.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'k.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
        'k.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0',
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
    ]
    for needle in required_planner:
        if needle not in p:
            errors.append("remap planner missing family/ownership/name-scope preview contract: " + needle)

    # Plan is a preview contract: blocked plans must remain inspectable by the adapter.
    plan_start = i.index("public static ProjectInterchangeRemapAppendPlan Plan")
    import_start = i.index("public static ProjectInterchangeRemapAppendResult Import", plan_start)
    plan_body = i[plan_start:import_start]
    if "ValidateExecutionSafety(" in plan_body:
        errors.append("Import As New Plan must return BLOCKED plans for inspection instead of enforcing mutation safety")

    # Import must re-plan and enforce execution safety before any rollback-capable mutation snapshot.
    import_end = i.index("private static void ValidateExecutionSafety", import_start)
    import_body = i[import_start:import_end]
    safety_at = import_body.find("ValidateExecutionSafety(source, plan);")
    snapshot_at = import_body.find("ProjectStateSnapshot.Capture(target)")
    if safety_at < 0 or snapshot_at < 0 or safety_at > snapshot_at:
        errors.append("Import As New must enforce the fresh plan before capturing/mutating target state")

    # Planner preview and executor must recognize the exact same conservative ID/ref suffix set.
    reference_suffixes = ["Id", "Ids", "Ref", "Refs", "RefId", "RefIds"]
    for suffix in reference_suffixes:
        planner_needle = f'trimmedKey.EndsWith("{suffix}", StringComparison.OrdinalIgnoreCase)'
        importer_needle = f'k.EndsWith("{suffix}", StringComparison.OrdinalIgnoreCase)'
        if planner_needle not in p:
            errors.append("remap planner opaque-reference policy missing suffix: " + suffix)
        if importer_needle not in i:
            errors.append("remap executor opaque-reference policy missing suffix: " + suffix)
    if "sourceElementIds.Contains(value.Trim())" in p:
        errors.append("remap planner must not hide unknown ID/ref-like properties just because their value is outside the source Element set")

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
        "Interchange Import As New BLOCKED",
        "Chưa mutate project/DWG",
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
    if 'throw new InvalidOperationException("Import As New plan is not executable.' in c:
        errors.append("blocked Import As New preview must report the plan and return instead of converting a policy block into an exception")

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
print("Import As New keeps blocked plans inspectable, re-plans and enforces safety before mutation, keeps Family/Element planner-executor opaque-reference policy aligned, scopes Family display-name collisions by category, strips incoming native ownership, preserves all existing target identities, and rolls back semantic state on failure.")
