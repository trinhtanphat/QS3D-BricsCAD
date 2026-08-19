#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GENERATED = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedGeometryService.cs"
OPENINGS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "OpeningBooleanService.cs"
INVALIDATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedDependentGeometryInvalidator.cs"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    generated = GENERATED.read_text(encoding="utf-8")
    openings = OPENINGS.read_text(encoding="utf-8")
    invalidator = INVALIDATOR.read_text(encoding="utf-8")

    for key in (
        "PhysicalOpeningCutSolidHandle",
        "PhysicalOpeningCutFingerprint",
        "PhysicalOpeningCutCount",
    ):
        if key not in openings:
            return fail("OpeningBooleanService no longer records expected opening-cut provenance key: " + key)

    if 'RemoveByPrefix(element, "PhysicalOpeningCut");' not in invalidator:
        return fail("source reconcile no longer invalidates PhysicalOpeningCut* metadata")

    prefix_decl = 'private const string PhysicalOpeningCutPrefix = "PhysicalOpeningCut";'
    if prefix_decl not in generated:
        return fail("GeneratedGeometryService must define the shared PhysicalOpeningCut prefix")

    start = generated.find("public static void CommitReplacement(")
    end = generated.find("private static bool HasMatchingOwnership(", start)
    if start < 0 or end < 0:
        return fail("cannot locate GeneratedGeometryService.CommitReplacement")
    commit_replacement = generated[start:end]

    clear_call = "RemovePropertiesByPrefix(element, PhysicalOpeningCutPrefix);"
    if clear_call not in commit_replacement:
        return fail("generated-solid replacement must invalidate stale PhysicalOpeningCut* provenance")

    handle_write = "element.Properties[HandleKey] = generatedHandle.Trim();"
    clean_call = "element.ClearGeneratedSolidStale();"
    if handle_write not in commit_replacement or clean_call not in commit_replacement:
        return fail("cannot verify generated-solid replacement ordering")
    if not (commit_replacement.index(handle_write) < commit_replacement.index(clear_call) < commit_replacement.index(clean_call)):
        return fail("PhysicalOpeningCut* invalidation must occur after replacement handle publication and before generated-solid clean state")

    helper_start = generated.find("private static void RemovePropertiesByPrefix(")
    helper_end = generated.find("private static void RemoveFromSourceHandles(", helper_start)
    if helper_start < 0 or helper_end < 0:
        return fail("cannot locate PhysicalOpeningCut* prefix-removal helper")
    helper = generated[helper_start:helper_end]
    if "StartsWith(prefix, StringComparison.OrdinalIgnoreCase)" not in helper:
        return fail("opening-cut prefix invalidation must be case-insensitive")
    if "element.Properties.Remove(key)" not in helper:
        return fail("opening-cut prefix invalidation must remove matching properties")

    print("PASS: generated-solid rebuild invalidates stale PhysicalOpeningCut* provenance before publishing clean geometry.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
