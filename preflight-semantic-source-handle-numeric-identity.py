#!/usr/bin/env python3
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing semantic SourceHandle numeric-identity file: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


resolver = read("src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs")
for token in (
    "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(sourceHandle)",
    "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(rawHandle)",
    "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x)",
    "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(\n                    RequireCanonicalStoredSourceHandle",
):
    if token not in resolver:
        errors.append("SemanticHandleOwnershipResolver.cs missing numeric-identity token: " + token)

if resolver.count("GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(") < 6:
    errors.append("SemanticHandleOwnershipResolver must normalize query, capture, canonical-owner, selected, stored and owner-dictionary Handle identities")

for stale in (
    "var normalized = (sourceHandle ?? string.Empty).Trim();",
    "var normalizedHandle = (sourceHandle ?? string.Empty).Trim();",
    "selected.Add(rawHandle.Trim());",
    "var handle = (rawHandle ?? string.Empty).Trim();",
):
    if stale in resolver:
        errors.append("SemanticHandleOwnershipResolver retained raw textual Handle identity path: " + stale)

smoke = read("tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceSmoke.cs")
for token in (
    "NumericAliasDuplicateFailsAcrossOwnershipEntryPoints",
    "NumericAliasCrossOwnerAmbiguityFailsAcrossOwnershipEntryPoints",
    "NumericAliasCaptureReusesExistingOwner",
    "NumericAliasUserSelectionRemainsNormalized",
    "NumericAliasSourceGeneratedCollisionFailsClosed",
    "MalformedTextIdentityCompatibilityIsPreserved",
    'NewProject("A", "00a")',
    'second.SourceHandles.Add("0xA")',
    'new[] { "A", "00a", "0xA" }',
    'NewProject("NOT-HEX", "0")',
):
    if token not in smoke:
        errors.append("SemanticHandleOwnershipDuplicateSourceSmoke.cs missing numeric-identity regression token: " + token)

registration = read("tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceRegistration.cs")
for token in ("ModuleInitializer", "SemanticHandleOwnershipDuplicateSourceSmoke.Run();"):
    if token not in registration:
        errors.append("semantic SourceHandle duplicate smoke registration missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic source ownership uses shared numeric CAD Handle identity while preserving malformed textual compatibility.")
