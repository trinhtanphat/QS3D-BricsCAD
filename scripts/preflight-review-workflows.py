#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
apply_service = ROOT / "src/QS3D.BricsCAD.V25/Services/RecognitionApplyBatchService.cs"
build3d = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
errors = []

if not review.is_file():
    errors.append("missing ReviewCommands.cs")
else:
    text = review.read_text(encoding="utf-8")
    required = (
        "var autoPlan = RecognitionApplyBatchService.PrepareBestEffort(doc, reviewProjectId, batch.AutoAccepted);",
        "RecognitionApplyBatchService.Commit(doc, reviewProjectId, autoPlan);",
        "QS3D Recognition skip",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
    )
    for token in required:
        if token not in text:
            errors.append("Review workflow safety contract missing: " + token)
    if "catch { skipped++; }" in text:
        errors.append("auto recognition must not silently swallow failed semantic captures")
    if 'AuditTrail.ForProject(ProjectContextCoordinator.GetOrCreate(doc)).Record("recognition.skip"' in text:
        errors.append("recognition.skip audit must not create/cache replacement project state")

if not apply_service.is_file():
    errors.append("missing RecognitionApplyBatchService.cs")
else:
    text = apply_service.read_text(encoding="utf-8")
    required = (
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        "if (project.ChangeVersion != plan.ProjectChangeVersion)",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        'audit.Record("recognition.skip", skip.Handle, skip.Reason);',
        "rollback.Restore(project)",
    )
    for token in required:
        if token not in text:
            errors.append("Recognition batch safety contract missing: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("Recognition batch service must not create/cache replacement project state")

if not build3d.is_file():
    errors.append("missing Build3DCommands.cs")
else:
    text = build3d.read_text(encoding="utf-8")
    required = (
        "categories.Count > 1",
        "SemanticReferenceHandles.GetSelectionAliases",
        "var untrackedHandles = handles",
        "if (untrackedHandles.Count > 0)",
        "Đã dừng trước khi rebuild",
    )
    for token in required:
        if token not in text:
            errors.append("Build3D selection safety contract missing: " + token)

    guard_index = text.find("if (untrackedHandles.Count > 0)")
    resolve_index = text.find("CadHandleService.Resolve(document, sourceHandles)")
    build_index = text.find("built = BuildCategory(document, project, category, sourceType);")
    if min(guard_index, resolve_index, build_index) >= 0 and not guard_index < resolve_index < build_index:
        errors.append("Build3D must reject untracked selection handles before source resolution/native build")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Build3D fails closed on partial/mixed selections and auto-recognition validation skips are committed/audited only through the current existing-project/version-guarded atomic batch service.")
