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


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

load_body = method_body(
    "private void LoadInstanceProperties(ProjectElement element, ProjectFamily family)",
    "private PropertyRowViewModel CreatePropertyRow",
)
apply_body = method_body(
    "private string ApplyInstanceProperty(ProjectElement element, ProjectFamily family, string key, string unit, PropertyRowViewModel row, string value)",
    "private void ResetInstanceProperty",
)
reset_body = method_body(
    "private void ResetInstanceProperty(ProjectElement element, ProjectFamily family, string key, PropertyRowViewModel row)",
    "private bool TryGetCurrentProjectForMutation",
)

if "row.CanReset = hasInstance;" not in load_body:
    errors.append("editable instance rows must expose every persisted instance value as an override, including legacy values equal to Family")
if "row.CanReset = hasInstance && !string.Equals(current, familyValue, StringComparison.Ordinal);" in load_body:
    errors.append("legacy equal-to-Family stored overrides must not be hidden from Reset")

for needle in [
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
]:
    if needle not in reset_body:
        errors.append("ResetInstanceProperty missing token: " + needle)

if "element.SetProperty(key" in reset_body:
    errors.append("ResetInstanceProperty must remove the instance override, not copy the Family value into instance storage")

remove_index = reset_body.find("element.Properties.Remove(key)")
display_index = reset_body.find("row.Value =")
if remove_index >= 0 and display_index >= 0 and remove_index > display_index:
    errors.append("instance override must be removed before the row is synchronized to the Family value")

for needle in [
    "var hasInstanceOverride = element.Properties.TryGetValue(key, out var stored);",
    "var resetToFamily = string.Equals(next, familyValue, StringComparison.Ordinal);",
    "if (resetToFamily)",
    "element.Properties.Remove(key);",
    "else",
    "element.SetProperty(key, next);",
    "row.CanReset = !resetToFamily;",
]:
    if needle not in apply_body:
        errors.append("ApplyInstanceProperty missing inheritance-collapse token: " + needle)

if "if (string.Equals(current, next, StringComparison.Ordinal) && (!resetToFamily || !hasInstanceOverride))" not in apply_body:
    errors.append("same-value fast path must not bypass removal of a hidden override equal to the Family value")

print("QS3D Workspace instance-override reset preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Workspace exposes persisted semantic overrides truthfully, removes them on Reset, and collapses edits back to live Family inheritance atomically with stale-affinity guards.")
