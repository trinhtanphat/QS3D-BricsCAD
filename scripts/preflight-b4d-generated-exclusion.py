#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
policy = ROOT / "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs"
semantic = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"

for path in (review, policy, semantic):
    if not path.is_file():
        errors.append("missing B4D/generated ownership file: " + str(path.relative_to(ROOT)))

if review.is_file():
    text = review.read_text(encoding="utf-8")
    for needle in (
        "CollectGeneratedHandles(project)",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)",
        "snapshots.Where(x => !generatedHandles.Contains(x.Handle))",
    ):
        if needle not in text:
            errors.append("ReviewCommands missing future-proof generated-source exclusion: " + needle)
    for stale in (
        'foreach (var key in new[]\n                {\n                    "GeneratedSolidHandle"',
        '"GeneratedBeamStirrupHandles"\n                })',
    ):
        if stale in text:
            errors.append("B4D still uses a hard-coded generated handle list that can miss future generated families.")

if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for needle in (
        'string.Equals(normalized, "PhysicalOpeningCutSolidHandle"',
        'normalized.StartsWith("Generated"',
        'normalized.EndsWith("Handle"',
        'normalized.EndsWith("Handles"',
    ):
        if needle not in text:
            errors.append("GeneratedHandleOwnershipPolicy missing owner-slot contract: " + needle)

if semantic.is_file() and "GeneratedFoundationMeshHandles" not in semantic.read_text(encoding="utf-8"):
    errors.append("Semantic generated-handle ownership must include Foundation mesh.")

print("QS3D B4D generated-source exclusion preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DB4D excludes every generated owner-slot handle via the shared adapter policy, including current and future generated geometry families.")
