#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulkEditCanonicalizationSmoke.cs"
errors = []

for path in (SERVICE, SMOKE):
    if not path.is_file():
        errors.append("missing bulk-edit null-target contract file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    start = text.find("private static IReadOnlyList<ProjectElement> OwnedDistinct(ProjectState project, IEnumerable<ProjectElement> elements)")
    end = text.find("private static ElementDirtyFlags DirtyFlags", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate BulkEditService.OwnedDistinct")
    else:
        body = text[start:end]
        for token in (
            "foreach (var element in elements)",
            "if (element == null)",
            'throw new InvalidOperationException("Bulk edit target collection contains a null semantic element entry.")',
            "if (elementId.Length == 0)",
            "!projectElements.TryGetValue(elementId, out var owned) || !ReferenceEquals(owned, element)",
        ):
            if token not in body:
                errors.append("OwnedDistinct missing fail-closed target validation: " + token)
        if "if (element == null) continue;" in body:
            errors.append("OwnedDistinct must not silently drop null caller-supplied targets")

    for signature in (
        "public IReadOnlyList<string> SetProperty(ProjectState project, IEnumerable<ProjectElement> elements",
        "public IReadOnlyList<string> MultiplyNumericProperty(ProjectState project, IEnumerable<ProjectElement> elements",
    ):
        start = text.find(signature)
        if start < 0:
            errors.append("missing object-based bulk edit overload: " + signature)
            continue
        next_public = text.find("\n        public ", start + len(signature))
        body = text[start: next_public if next_public >= 0 else len(text)]
        if "OwnedDistinct(project, elements)" not in body:
            errors.append("object-based bulk edit must validate all targets through OwnedDistinct before mutation: " + signature)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ObjectBasedBulkEditsRejectNullTargets();",
        "private static void ObjectBasedBulkEditsRejectNullTargets()",
        "new ProjectElement[] { wall, null! }",
        "service.SetProperty(project, targets",
        "service.MultiplyNumericProperty(project, targets",
        "wall.Dirty != dirty",
        "project.ChangeVersion != version",
    ):
        if token not in smoke:
            errors.append("bulk-edit smoke missing null-target atomicity assertion: " + token)

print("QS3D bulk-edit null target preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: object-based bulk edits reject null caller targets before property/dirty/version mutation while retaining project-ownership validation.")
