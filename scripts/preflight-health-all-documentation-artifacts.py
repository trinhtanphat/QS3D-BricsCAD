#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    required_health = (
        "new GeneratedGridAnnotationHealthService().Inspect(project)",
        "GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)",
        "new GeneratedSemanticTagHealthService().Inspect(project)",
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
        "GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project)",
        "BbsNativeTableBuilder.Inspect(document, project)",
        "BqNativeTableBuilder.Inspect(document, project)",
        "DoorOpeningNativeTableBuilder.Inspect(document, project)",
        "MaterialUsageNativeTableBuilder.Inspect(document, project)",
        "RoomFinishNativeTableBuilder.Inspect(document, project)",
    )
    for token in required_health:
        if token not in text:
            errors.append("Health All missing documentation health source: " + token)

    required_locate = (
<<<<<<< HEAD
=======
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
>>>>>>> origin/main
        "LocateProjectArtifactHandles(currentProject, issue.Code)",
        "MetadataHandle(project, SemanticElementTableBuilder.HandleKey)",
        "MetadataHandle(project, BbsNativeTableBuilder.Definition.HandleKey)",
        "MetadataHandle(project, BqNativeTableBuilder.Definition.HandleKey)",
        "MetadataHandle(project, DoorOpeningNativeTableBuilder.Definition.HandleKey)",
        "MetadataHandle(project, MaterialUsageNativeTableBuilder.Definition.HandleKey)",
        "MetadataHandle(project, RoomFinishNativeTableBuilder.Definition.HandleKey)",
        "SplitPropertyHandles(element, GeneratedSemanticTagHealthService.HandlesKey)",
        'SplitPropertyHandles(element, "GeneratedGridAnnotationHandles")',
    )
    for token in required_locate:
        if token not in text:
            errors.append("Health All missing documentation Locate contract: " + token)

    locate_call_pos = text.find("var handles = LocateHandles(element, issue.Code).ToArray();")
    fallback_pos = text.find("SourceHandleResolver.Resolve(currentProject, new[] { element.Id })")
    if min(locate_call_pos, fallback_pos) >= 0 and not locate_call_pos < fallback_pos:
        errors.append("Generated artifact Locate must run before current-project source-handle fallback")

    read_only_pos = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    health_pos = text.find("GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project)")
    if min(read_only_pos, health_pos) >= 0 and not read_only_pos < health_pos:
        errors.append("Documentation health must run only after read-only project resolution")

    if "LocateProjectArtifactHandles(project, issue.Code)" in text:
        errors.append("Modeless project-artifact Locate must not use the project snapshot captured when Health All opened")

if errors:
    print("QS3D Health All documentation-artifact preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DHEALTHALL covers generated Grid/Tag/Table documentation metadata/live CAD health and re-resolves current project state before locating generated artifacts.")
