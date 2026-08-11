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
        if "return new ProjectInterchangeRemapAppendPlan(remap, ownershipProperties)" not in plan_body:
            errors.append("remap append Plan must return blocked/ready plan metadata without mutation")

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

    # Re-plan and fail-closed validation must happen before snapshot capture/mutation.
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
        "ProjectInterchangeRemapAppendImporter.Import(project, json)",
        "if (!plan.CanImport)",
        "Interchange Import As New BLOCKED",
        "plan.Remap.OpaqueReferenceWarnings.Count",
        "QS3DINTERCHANGEREMAPPLAN",
        "chưa mutate project/DWG",
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
    if "Import As New plan is not executable. Run QS3DINTERCHANGEREMAPPLAN" in c:
        errors.append("blocked remap plans should surface as normal BLOCKED status, not generic command exceptions")

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
print("Import As New keeps blocked plans inspectable, fails closed before mutation, aligns Family/Element opaque-reference policy, scopes Family names by category, strips incoming native ownership, and preserves existing target identities.")
