#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeFieldMergeImporter.cs"
COORDINATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeImportCoordinator.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeFieldMergeImporterSmoke.cs"
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeImportService.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeFieldMergeCommands.cs"
PROJECT_TOOLS = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ProjectToolsWindow.xaml"

errors = []
for path in (IMPORTER, COORDINATOR, SMOKE, ADAPTER, COMMAND, PROJECT_TOOLS):
    if not path.is_file():
        errors.append("missing field-merge execution contract file: " + str(path.relative_to(ROOT)))

if not errors:
    importer = IMPORTER.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    adapter = ADAPTER.read_text(encoding="utf-8")
    command = COMMAND.read_text(encoding="utf-8")
    project_tools = PROJECT_TOOLS.read_text(encoding="utf-8")

    required = (
        "ProjectInterchangeFieldMergeAuthorization",
        "TargetChangeVersion",
        "SourceSnapshotHash",
        "DecisionStamp",
        "authorization.MatchesExactly(plan)",
        "Field merge handles same-ID collisions only",
        "ProjectStateSnapshot.Capture(target)",
        "ProjectZoneService.Update",
        "ProjectFloorService.Update",
        "ProjectFamilyService.Rename",
        "ProjectFamilyService.SetProperty",
        "ProjectFamilyService.RemoveProperty",
        "ProjectFamilyService.Assign",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle",
        "Field merge native cleanup ownership is ambiguous",
        "requires a non-empty target drawing fingerprint",
        "ClearGeneratedOwnershipMetadata",
        "ValidateCombinedTarget",
        "snapshot.Restore(target)",
    )
    for token in required:
        if token not in importer:
            errors.append("field-merge importer missing guarded execution token: " + token)

    forbidden = (
        "ProjectContextCoordinator.GetOrCreate",
        "TransactionManager",
    )
    for token in forbidden:
        if token in importer:
            errors.append("Core field-merge importer crossed native/project-bootstrap boundary: " + token)

    adapter_required = (
        "ExistingProjectMutationContext.Require",
        "ProjectStateSnapshot.Capture(lockedProject)",
        "ReferenceEquals(lockedProject, project)",
        "reviewedPlan.CorePlan.AffectedTargetElementIds",
        "GeneratedDependentGeometryInvalidator.Prepare",
        "ProjectInterchangeFieldMergeImporter.Import",
        "reviewedPlan.Authorization",
        "invalidation.CommitMetadata()",
        "transaction.Commit()",
        "rollback.Restore(project)",
        "MdiActiveDocument",
    )
    for token in adapter_required:
        if token not in adapter:
            errors.append("BricsCAD field-merge adapter missing atomic native/semantic token: " + token)

    prepare_at = adapter.find("GeneratedDependentGeometryInvalidator.Prepare")
    import_at = adapter.find("ProjectInterchangeFieldMergeImporter.Import")
    metadata_at = adapter.find("invalidation.CommitMetadata()")
    commit_at = adapter.find("transaction.Commit()")
    if min(prepare_at, import_at, metadata_at, commit_at) < 0 or not (prepare_at < import_at < metadata_at < commit_at):
        errors.append("BricsCAD field-merge adapter must preserve Prepare-native -> authorized Core Import -> metadata parity sweep -> CAD commit ordering")

    if "ProjectContextCoordinator.GetOrCreate(document)" in adapter:
        errors.append("field-merge native mutation must require an existing canonical project instead of bootstrapping one")

    command_required = (
        'CommandMethod("QS3DINTERCHANGEFIELDMERGE"',
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var reviewedProject)",
        "ExistingProjectMutationContext.Require",
        "InterchangeFieldMergeImportService.Plan",
        "InterchangeFieldMergeImportService.Import",
        "currentProject.ProjectId",
        "plan.TargetProjectId",
        "currentProject.DrawingFingerprint",
        "plan.TargetDrawingFingerprint",
        "currentProject.ChangeVersion",
        "plan.TargetChangeVersion",
        "policy.ZoneName",
        "policy.FloorName",
        "policy.FloorElevation",
        "policy.FamilyName",
        "policy.FamilyProperties",
        "policy.ElementFamily",
        "policy.ElementFloor",
        "policy.ElementZone",
        "policy.ElementDependencies",
        "policy.ElementProperties",
        "policy.ElementQuantities",
        "FieldMerge chỉ xử lý same-ID collisions",
        "Incoming source CAD ownership không được nhận vào target",
    )
    for token in command_required:
        if token not in command:
            errors.append("field-merge command missing reviewed policy/freshness token: " + token)

    probe_at = command.find("ProjectContextCoordinator.TryGetReadOnly(document, out var reviewedProject)")
    policy_at = command.find("TryChoosePolicy(out var policy)")
    plan_at = command.find("InterchangeFieldMergeImportService.Plan(reviewedProject, json, policy)")
    confirm_at = command.find("System.Windows.MessageBox.Show(", plan_at)
    bind_at = command.find("ExistingProjectMutationContext.Require", confirm_at)
    command_import_at = command.find("InterchangeFieldMergeImportService.Import", bind_at)
    if min(probe_at, policy_at, plan_at, confirm_at, bind_at, command_import_at) < 0 or not (
        probe_at < policy_at < plan_at < confirm_at < bind_at < command_import_at
    ):
        errors.append("field-merge command must preserve read-only preview -> policy -> plan -> confirmation -> canonical bind -> authorized import ordering")

    if "InterchangeConfirmationGuard.RequireFresh" in command:
        errors.append("field-merge command must not use reference-identity freshness before a cold-cache canonical bind; exact target identity/version is checked after confirmation instead")

    if project_tools.count('Tag="QS3DINTERCHANGEFIELDMERGE"') != 1:
        errors.append("Project Tools must expose the dedicated reviewed field-merge command exactly once")

    if "FieldMerge" in coordinator or "FieldPrecedence" in coordinator:
        errors.append("field merge must stay a dedicated reviewed BricsCAD command until exact-V25 qualification closes the generic coordinator exposure gate")

    smoke_required = (
        "MixedReviewedMergeAppliesOnlySelectedSourceGroups",
        "TargetRevisionChangeRejectsReviewedAuthorization",
        "SourceSnapshotChangeRejectsReviewedAuthorization",
        "GeneratedHandleChangeRejectsReviewedAuthorization",
        "AmbiguousGeneratedOwnershipBlocksAuthorization",
        "DestructiveCleanupRequiresTargetDrawingFingerprint",
        "SourceOnlyIdentityBlocksExecution",
        "FamilyReassignmentPreservesTargetPropertiesWhenRequested",
    )
    for token in smoke_required:
        if token not in smoke:
            errors.append("field-merge importer smoke missing execution/freshness regression: " + token)

if errors:
    print("QS3D interchange field-merge execution preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: reviewed Core field merge is target/source/decision fresh and exact-handle cleanup-bound; BricsCAD previews through a non-creating read-only project, defers canonical binding until explicit confirmation, rechecks target identity/version before the atomic native transaction, exposes the dedicated workflow through Project Tools, and keeps generic coordinator exposure gated on exact-V25 qualification.")
