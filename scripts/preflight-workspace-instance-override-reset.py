#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing WorkspaceViewModel source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

signature = "private void ResetInstanceProperty(ProjectElement element, ProjectFamily family, string key, PropertyRowViewModel row)"
start = text.find(signature)
if start < 0:
    errors.append("missing ResetInstanceProperty")
    body = ""
else:
    next_method = text.find("private bool TryGetCurrentProjectForMutation", start)
    body = text[start:next_method if next_method >= 0 else len(text)]

required = [
    "TryGetCurrentProjectForMutation(\"Đặt lại Instance property\", out var project)",
    "project.FindElement(element.Id)",
    "project.FindFamily(family.Id)",
    "ReferenceEquals(ownedElement, element)",
    "ReferenceEquals(ownedFamily, family)",
    "ownedFamily.Properties.TryGetValue(key, out var liveFamilyRaw)",
    "ProjectSemanticMutationExecutor.Execute(",
    "element.Properties.Remove(key)",
    "project.Touch();",
    "row.CanReset = false;",
]
for needle in required:
    if needle not in body:
        errors.append("ResetInstanceProperty missing token: " + needle)

if "element.SetProperty(key" in body:
    errors.append("ResetInstanceProperty must remove the instance override, not copy the Family value into instance storage")

remove_index = body.find("element.Properties.Remove(key)")
display_index = body.find("row.Value =")
if remove_index >= 0 and display_index >= 0 and remove_index > display_index:
    errors.append("instance override must be removed before the row is synchronized to the Family value")

if "ProjectSemanticMutationExecutor.Execute(" in body and "element.Properties.Remove(key)" in body:
    executor_index = body.find("ProjectSemanticMutationExecutor.Execute(")
    remove_index = body.find("element.Properties.Remove(key)")
    if remove_index < executor_index:
        errors.append("override removal must execute inside ProjectSemanticMutationExecutor for rollback/touch ownership")

print("QS3D Workspace instance-override reset preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Workspace Reset Instance removes the owned semantic override atomically before synchronizing inherited Family presentation.")
