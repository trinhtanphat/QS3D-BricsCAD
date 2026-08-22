#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

adapter = ROOT / "src/QS3D.BricsCAD.V25"
wall_snap = adapter / "WallJunctionSnapCommands.cs"
if not adapter.is_dir():
    errors.append("missing BricsCAD V25 adapter directory")
else:
    sources = []
    combined = []
    for path in adapter.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        sources.append((path, text))
        combined.append(text)
    all_text = "\n".join(combined)

    for command in ("QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY"):
        if not re.search(r'CommandMethod\("' + re.escape(command) + r'"', all_text):
            errors.append("missing wall snap command: " + command)

    safety_tokens = (
        "WallJunctionAdjustmentPlanner",
        "PlanHash",
        "SourceFingerprint",
        "preview",
        "fingerprint",
    )
    for token in safety_tokens:
        if token.lower() not in all_text.lower():
            errors.append("wall snap review/apply safety token missing: " + token)

    apply_sources = [text for _, text in sources if 'CommandMethod("QS3DWALLSNAPAPPLY"' in text]
    if apply_sources:
        apply_text = "\n".join(apply_sources)
        if "Transaction" not in apply_text:
            errors.append("wall snap apply must use a CAD transaction")
        if "Erase" in apply_text and "SourceFingerprint" not in apply_text:
            errors.append("wall snap apply contains destructive erase without source fingerprint guard")

if wall_snap.is_file():
    text = wall_snap.read_text(encoding="utf-8")
    preview_start = text.find('[CommandMethod("QS3DWALLSNAPPREVIEW"')
    apply_start = text.find('[CommandMethod("QS3DWALLSNAPAPPLY"')
    build_start = text.find("private static SnapPlan BuildPlan", apply_start)
    if min(preview_start, apply_start, build_start) < 0:
        errors.append("cannot isolate Wall Snap Preview/Apply command bodies")
    else:
        preview = text[preview_start:apply_start]
        apply = text[apply_start:build_start]
        for label, block in (("Preview", preview), ("Apply", apply)):
            readonly = block.find("RequireReadOnlyProject(document")
            expected_id = block.find("var expectedProjectId = observedProject.ProjectId;")
            expected_version = block.find("var expectedChangeVersion = observedProject.ChangeVersion;")
            plan = block.find("BuildPlan(document, observedProject, true)")
            empty = block.find("if (plan.Segments.Count == 0)")
            bind = block.find("RequireFreshMutationProject(document")
            if min(readonly, expected_id, expected_version, plan, empty, bind) < 0 or not (readonly < expected_id < expected_version < plan < empty < bind):
                errors.append("Wall Snap %s must read-only probe/capture project identity, prompt/plan, allow cancel, then bind fresh mutation project" % label)

        for token in (
            'private const string PreviewProjectIdKey = "WallJunctionSnapPreviewProjectId";',
            'private const string PreviewChangeVersionKey = "WallJunctionSnapPreviewChangeVersion";',
            "project.Metadata[PreviewProjectIdKey] = project.ProjectId;",
            "var approvedVersion = NextChangeVersion(project.ChangeVersion);",
            "project.Metadata[PreviewChangeVersionKey] = approvedVersion.ToString(CultureInfo.InvariantCulture);",
            "previewVersion != project.ChangeVersion",
            "if (ClearPreview(project)) project.Touch();",
            "private static ProjectState RequireReadOnlyProject",
            "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
            "private static ProjectState RequireFreshMutationProject",
            "project.ChangeVersion != expectedChangeVersion",
            "private static long NextChangeVersion(long current)",
            "private static bool ClearPreview(ProjectState project)",
            "changed |= project.Metadata.Remove(PreviewProjectIdKey);",
            "changed |= project.Metadata.Remove(PreviewChangeVersionKey);",
        ):
            if token not in text:
                errors.append("Wall Snap lifecycle/freshness contract missing: " + token)

        audit = preview.find('AuditTrail.ForProject(project).Record("wall.junction.snap.preview"')
        approved = preview.find("var approvedVersion = NextChangeVersion(project.ChangeVersion);")
        stored = preview.find("project.Metadata[PreviewChangeVersionKey] = approvedVersion.ToString(CultureInfo.InvariantCulture);")
        touch = preview.find("project.Touch();", stored)
        if min(audit, approved, stored, touch) < 0 or not (audit < approved < stored < touch):
            errors.append("Wall Snap Preview must stamp its exact final ChangeVersion after preview audit mutation")

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    for tag in ('Tag="QS3DWALLSNAPPREVIEW"', 'Tag="QS3DWALLSNAPAPPLY"'):
        if tag not in text:
            errors.append("Domain Hub missing wall snap workflow tag: " + tag)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.is_file():
    text = registration.read_text(encoding="utf-8")
    if "WallJunctionAdjustmentSmoke.Run();" not in text:
        errors.append("WallJunctionAdjustmentSmoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: wall snap Preview/Apply preserve cancel-safe read-only selection, exact project/version preview freshness, source/plan fingerprints, metadata cleanup versioning, CAD transaction safety and UI wiring.")
