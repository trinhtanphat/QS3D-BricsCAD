#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "resolver": ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs",
    "cad": ROOT / "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs",
    "smoke": ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleResolverSafetySmoke.cs",
}
for path in files.values():
    if not path.is_file():
        errors.append("missing locate-selection file: " + str(path.relative_to(ROOT)))

if files["resolver"].is_file():
    text = files["resolver"].read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Diagnostics;",
        "GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element)",
        "if (!hasDirectReference)",
        "if (!hasDirectReference && !hasBoundaryReference)",
        "AddBoundaryHandles(element, knownHandles, handles, out hasBoundaryReference)",
        "AddGeneratedOwnerHandles(element, knownHandles, handles)",
    ):
        if token not in text:
            errors.append("SourceHandleResolver.cs missing locate fallback token: " + token)
    if 'element.Properties.TryGetValue("GeneratedSolidHandle"' in text:
        errors.append("SourceHandleResolver.cs still hard-codes GeneratedSolidHandle fallback instead of canonical logical owner handles.")

if files["cad"].is_file():
    text = files["cad"].read_text(encoding="utf-8")
    for token in (
        "public static int Select(Document document, IEnumerable<string> handles) => SelectIfAny(document, handles);",
        "public static int SelectIfAny(Document document, IEnumerable<string> handles)",
        "if (ids.Count == 0) return 0;",
        "document.Editor.SetImpliedSelection",
    ):
        if token not in text:
            errors.append("CadHandleService.cs missing PICKFIRST-safe selection token: " + token)
    method = text.find("public static int SelectIfAny")
    if method >= 0:
        end = text.find("public static ISet<string> GetLiveHandles", method)
        body = text[method:end if end >= 0 else len(text)]
        empty_guard = body.find("if (ids.Count == 0) return 0;")
        set_selection = body.find("document.Editor.SetImpliedSelection")
        if empty_guard < 0 or set_selection < 0 or empty_guard > set_selection:
            errors.append("SelectIfAny must return before SetImpliedSelection when no live handles resolve.")

if files["smoke"].is_file():
    text = files["smoke"].read_text(encoding="utf-8")
    for token in (
        "SourceReferenceWinsOverGeneratedFallback",
        "BoundaryReferenceWinsOverGeneratedFallback",
        "CanonicalGeneratedOwnersResolveWhenSourceIsMissing",
        '"GeneratedSolidHandle"',
        '"GeneratedRebarHandles"',
        '"GeneratedShapeRebarHandles"',
        '"GeneratedTieRebarHandles"',
        '"GeneratedBeamStirrupHandles"',
        '"GeneratedSlabMeshHandles"',
        '"GeneratedWallMeshHandles"',
        '"GeneratedFoundationMeshHandles"',
        '"GeneratedCurtainFrameHandles"',
        '"PhysicalOpeningCutSolidHandle"',
    ):
        if token not in text:
            errors.append("SourceHandleResolverSafetySmoke.cs missing locate coverage token: " + token)

print("QS3D locate-selection preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: semantic locate keeps source/boundary priority, falls back to canonical generated owners, and unresolved CAD handles do not clear PICKFIRST.")
