#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "WallJunctionSnapCommands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []

if not COMMAND.is_file():
    errors.append("missing WallJunctionSnapCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("Wall Snap Preview/Apply must not create/cache project state")

    preview_region_start = text.find('[CommandMethod("QS3DWALLSNAPPREVIEW"')
    apply_region_start = text.find('[CommandMethod("QS3DWALLSNAPAPPLY"')
    build_plan_start = text.find("private static SnapPlan BuildPlan", apply_region_start)
    read_only_helper_start = text.find("private static ProjectState RequireReadOnlyProject")
    fresh_helper_start = text.find("private static ProjectState RequireFreshMutationProject")
    next_version_start = text.find("private static long NextChangeVersion", fresh_helper_start)
    source_fingerprint_start = text.find("private static void RequireSourceFingerprint")
    source_fingerprint_end = text.find("private static void EnsureElevation", source_fingerprint_start)
    if min(
        preview_region_start,
        apply_region_start,
        build_plan_start,
        read_only_helper_start,
        fresh_helper_start,
        next_version_start,
        source_fingerprint_start,
        source_fingerprint_end,
    ) < 0:
        errors.append("cannot isolate Wall Snap command regions")
    else:
        preview_region = text[preview_region_start:apply_region_start]
        apply_region = text[apply_region_start:build_plan_start]
        read_only_helper = text[read_only_helper_start:fresh_helper_start]
        fresh_helper = text[fresh_helper_start:next_version_start]
        source_fingerprint_helper = text[source_fingerprint_start:source_fingerprint_end]

        for label, operation, region in (
            ("Preview", "Wall Snap Preview", preview_region),
            ("Apply", "Wall Snap Apply", apply_region),
        ):
            positions = (
                region.find('RequireReadOnlyProject(document, "' + operation + '")'),
                region.find("var expectedProjectId = observedProject.ProjectId;"),
                region.find("var expectedChangeVersion = observedProject.ChangeVersion;"),
                region.find("BuildPlan(document, observedProject, true)"),
                region.find("if (plan.Segments.Count == 0)"),
                region.find('RequireFreshMutationProject(document, "' + operation + '", expectedProjectId, expectedChangeVersion)'),
            )
            if min(positions) < 0 or tuple(sorted(positions)) != positions:
                errors.append(
                    "Wall Snap %s must capture read-only project identity/version, allow empty selection/cancel, then bind fresh mutation state"
                    % label
                )
            elif "return;" not in region[positions[4]:positions[5]]:
                errors.append("Wall Snap %s empty selection/cancel must return before mutation bind" % label)

        for token in (
            "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
            "selection/cancel must not create or recover one",
        ):
            if token not in read_only_helper:
                errors.append("Wall Snap read-only selection phase drift; missing token: " + token)
        for token in (
            "ExistingProjectMutationContext.Require(document, operation)",
            "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
            "project.ChangeVersion != expectedChangeVersion",
        ):
            if token not in fresh_helper:
                errors.append("Wall Snap fresh canonical mutation bind drift; missing token: " + token)

        preview_fresh_bind = preview_region.find('RequireFreshMutationProject(document, "Wall Snap Preview"')
        preview_plan_hash = preview_region.find("project.Metadata[PreviewPlanHashKey] = plan.PlanHash;")
        preview_source = preview_region.find("project.Metadata[PreviewSourceFingerprintKey] = plan.SourceFingerprint;")
        preview_project = preview_region.find("project.Metadata[PreviewProjectIdKey] = project.ProjectId;")
        preview_next_version = preview_region.find("var approvedVersion = NextChangeVersion(project.ChangeVersion);")
        preview_version = preview_region.find("project.Metadata[PreviewChangeVersionKey] = approvedVersion.ToString(CultureInfo.InvariantCulture);")
        preview_touch = preview_region.find("project.Touch();", preview_version)
        preview_positions = (
            preview_fresh_bind,
            preview_plan_hash,
            preview_source,
            preview_project,
            preview_next_version,
            preview_version,
            preview_touch,
        )
        if min(preview_positions) < 0 or tuple(sorted(preview_positions)) != preview_positions:
            errors.append("Wall Snap Preview must stamp plan/source/project and the exact post-preview ChangeVersion before Touch")

        apply_fresh_bind = apply_region.find('RequireFreshMutationProject(document, "Wall Snap Apply"')
        apply_project_check = apply_region.find("PreviewProjectIdKey", apply_fresh_bind)
        apply_version_check = apply_region.find("PreviewChangeVersionKey", apply_project_check)
        apply_source_check = apply_region.find("PreviewSourceFingerprintKey", apply_version_check)
        apply_plan_check = apply_region.find("PreviewPlanHashKey", apply_source_check)
        rollback_capture = apply_region.find("ProjectStateSnapshot.Capture(project)", apply_plan_check)
        live_source_check = apply_region.find("RequireSourceFingerprint(transaction, units, plan)", rollback_capture)
        invalidation = apply_region.find("GeneratedDependentGeometryInvalidator.Prepare", live_source_check)
        native_commit = apply_region.find("transaction.Commit()", invalidation)
        apply_positions = (
            apply_fresh_bind,
            apply_project_check,
            apply_version_check,
            apply_source_check,
            apply_plan_check,
            rollback_capture,
            live_source_check,
            invalidation,
            native_commit,
        )
        if min(apply_positions) < 0 or tuple(sorted(apply_positions)) != apply_positions:
            errors.append("Wall Snap Apply must validate project/version/source/plan freshness before rollback-protected native mutation")

        for token in (
            "rollback.Restore(project)",
        ):
            if token not in apply_region:
                errors.append("Wall Snap Apply rollback/native boundary drift; missing token: " + token)
        for token in (
            "transaction.GetObject(segment.ObjectId, OpenMode.ForRead, false)",
            "BuildSourceFingerprint(current, plan.ToleranceM, plan.MovementEpsilonM)",
            "string.Equals(liveFingerprint, plan.SourceFingerprint, StringComparison.Ordinal)",
        ):
            if token not in source_fingerprint_helper:
                errors.append("Wall Snap live source freshness drift; missing token: " + token)

if not INBOX.is_file():
    errors.append("missing LOCAL-AGENT-INBOX.md")
else:
    inbox = INBOX.read_text(encoding="utf-8")
    for token in (
        "LOCAL-007 — physical L/T/X wall junction output",
        "QS3DWALLSNAPPREVIEW",
        "QS3DWALLSNAPAPPLY",
        "must not create/cache a replacement project",
    ):
        if token not in inbox:
            errors.append("LOCAL-007 Wall Snap runtime handoff missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Wall Snap Preview/Apply preserve cancel-before-bind, canonical project/version/source/plan freshness, apply rollback boundaries, and the explicit LOCAL-007 V25 lifecycle scenario.")
