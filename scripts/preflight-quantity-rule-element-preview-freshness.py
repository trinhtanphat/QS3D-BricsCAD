#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rules/QuantityRulePreviewService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing QuantityRulePreviewService.cs")
else:
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("public QuantityRuleElementPreview PreviewElement(ProjectState project, ProjectElement element)")
    end = source.find("public QuantityRuleProjectPreview PreviewProject(ProjectState project)", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QuantityRulePreviewService.PreviewElement")
    else:
        body = source[start:end]
        ownership = body.find("RequireOwnedElement(project, element);")
        version = body.find("var sourceChangeVersion = project.ChangeVersion;")
        snapshot = body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        resolve = body.find("detached.FindElement(element.Id)")
        stamp = body.find("PreviewDetached(detached, detachedElement, sourceChangeVersion)")
        if min(ownership, version, snapshot, resolve, stamp) < 0 or not (ownership < version < snapshot < resolve < stamp):
            errors.append("element preview must capture ChangeVersion before detached snapshot and stamp that immutable scalar")
        if "PreviewDetached(detached, detachedElement, project.ChangeVersion)" in body:
            errors.append("element preview must not stamp a post-snapshot live ChangeVersion")
        if body.count("project.ChangeVersion") != 1:
            errors.append("element preview must read live ChangeVersion exactly once before snapshot capture")

    project_start = source.find("public QuantityRuleProjectPreview PreviewProject(ProjectState project)")
    apply_start = source.find("public int ApplyElement", project_start)
    if project_start < 0 or apply_start <= project_start:
        errors.append("cannot isolate QuantityRulePreviewService.PreviewProject reference contract")
    else:
        project_body = source[project_start:apply_start]
        version = project_body.find("var sourceChangeVersion = project.ChangeVersion;")
        snapshot = project_body.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
        if min(version, snapshot) < 0 or version >= snapshot:
            errors.append("project preview reference contract must continue capturing ChangeVersion before detached snapshot")

print("QS3D quantity-rule element preview freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: quantity-rule element and project previews bind freshness to a live ChangeVersion captured before detached snapshot creation.")
