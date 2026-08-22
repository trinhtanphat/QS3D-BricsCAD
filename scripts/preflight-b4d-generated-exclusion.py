#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
adapter_policy = ROOT / "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs"
core_policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
semantic = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
snapshot = ROOT / "src/QS3D.Core/Model/EntitySnapshot.cs"
eligibility = ROOT / "src/QS3D.Core/Recognition/EntitySnapshotCaptureEligibility.cs"
reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs"
native_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedNativeSourceGuard.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs"

for path in (review, adapter_policy, core_policy, semantic, snapshot, eligibility, reader, native_guard, smoke):
    if not path.is_file():
        errors.append("missing B4D/generated ownership file: " + str(path.relative_to(ROOT)))

if review.is_file():
    text = review.read_text(encoding="utf-8")
    for needle in (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject)",
        "CollectGeneratedHandles(previewProject)",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "snapshots.Where(x => !generatedHandles.Contains(x.Handle))",
        "snapshots.Where(x => !x.HasQs3dGeneratedOwnershipMarker)",
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

if snapshot.is_file() and "HasQs3dGeneratedOwnershipMarker" not in snapshot.read_text(encoding="utf-8"):
    errors.append("EntitySnapshot must carry the public-native generated ownership classification.")

if eligibility.is_file():
    text = eligibility.read_text(encoding="utf-8")
    for needle in ("snapshot.HasQs3dGeneratedOwnershipMarker", "cannot be captured as a semantic source"):
        if needle not in text:
            errors.append("capture eligibility must reject native generated ownership: " + needle)

if reader.is_file() and "GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity)" not in reader.read_text(encoding="utf-8"):
    errors.append("EntitySnapshotReader must classify native generated ownership from the live CAD entity.")

if native_guard.is_file():
    text = native_guard.read_text(encoding="utf-8")
    for needle in (
        "entity.GetXDataForApplication(regAppName)",
        '"QS3D"',
        '"QS3D_REBAR"',
        '"QS3D_CURTAIN_FRAME"',
        '"QS3D_CURTAIN_PANEL"',
        '"QS3DDOC"',
        "if (marker != null) return true",
    ):
        if needle not in text:
            errors.append("native generated-source guard missing fail-closed marker contract: " + needle)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in ("HasQs3dGeneratedOwnershipMarker = true", "generatedResult.IsCaptureReady", "generated output must never be auto-accepted"):
        if needle not in text:
            errors.append("Core smoke missing generated-native capture regression: " + needle)

print("QS3D B4D generated-source exclusion preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DB4D/QS3DRECOGNIZE exclude live native QS3D generated XData without a sidecar, and Core capture eligibility rejects generated snapshots; sidecar-backed exclusion still uses canonical Core owner slots.")
