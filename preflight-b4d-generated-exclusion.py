#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
adapter_policy = ROOT / "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs"
core_policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
semantic = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"

for path in (review, adapter_policy, core_policy, semantic):
    if not path.is_file():
        errors.append("missing B4D/generated ownership file: " + str(path.relative_to(ROOT)))

if review.is_file():
    text = review.read_text(encoding="utf-8")
    for needle in (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject)",
        "CollectGeneratedHandles(previewProject)",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "snapshots.Where(x => !generatedHandles.Contains(x.Handle))",
    ):
        if needle not in text:
            errors.append("ReviewCommands missing canonical generated-source exclusion: " + needle)
    if "property.Value.Split" in text[text.find("private static HashSet<string> CollectGeneratedHandles"):]:
        errors.append("B4D must not duplicate owner-handle parsing after the Core policy exposes CollectOwnerHandles.")

if adapter_policy.is_file():
    text = adapter_policy.read_text(encoding="utf-8")
    for needle in (
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
    ):
        if needle not in text:
            errors.append("adapter ownership facade must delegate to Core policy: " + needle)
    if 'StartsWith("Generated"' in text or 'PhysicalOpeningCutSolidHandle' in text:
        errors.append("adapter ownership facade must not duplicate owner-slot classification logic")

if core_policy.is_file():
    text = core_policy.read_text(encoding="utf-8")
    for needle in (
        'string.Equals(normalized, OpeningCutOwnerKey',
        'normalized.StartsWith("Generated"',
        'normalized.EndsWith("Handle"',
        'normalized.EndsWith("Handles"',
        "EnumerateOwnerHandles",
        "CollectOwnerHandles",
        "TryFindOwner",
    ):
        if needle not in text:
            errors.append("Core GeneratedHandleOwnershipPolicy missing owner-slot contract: " + needle)

if semantic.is_file() and "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)" not in semantic.read_text(encoding="utf-8"):
    errors.append("Semantic generated-handle ownership must consume the shared Core policy dynamically.")

print("QS3D B4D generated-source exclusion preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DB4D excludes generated owner-slot handles using Core CollectOwnerHandles; selection and adapter code consume the same canonical classification/parsing contract.")
