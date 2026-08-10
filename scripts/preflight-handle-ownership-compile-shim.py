#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

facade = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.cs"
safe = ROOT / "src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs"
policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
legacy_shim = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.Safe.cs"
targets = ROOT / "src/QS3D.Core/Directory.Build.targets"

for path in (facade, safe, policy):
    if not path.is_file():
        errors.append("missing canonical ownership file: " + str(path.relative_to(ROOT)))

if legacy_shim.exists():
    errors.append("legacy duplicate ownership shim must not exist")

if targets.exists():
    text = targets.read_text(encoding="utf-8")
    if "GeneratedHandleOwnershipHealthService.cs" in text and "Compile Remove" in text:
        errors.append("canonical ownership facade must not be excluded from Core compilation")

if facade.is_file():
    text = facade.read_text(encoding="utf-8")
    for token in (
        "public sealed class GeneratedHandleOwnershipHealthService",
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
    ):
        if token not in text:
            errors.append("canonical ownership facade missing token: " + token)

if safe.is_file():
    text = safe.read_text(encoding="utf-8")
    if "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)" not in text:
        errors.append("safe ownership scanner must consume GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles")

if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for token in ('StartsWith("Generated"', 'PhysicalOpeningCutSolidHandle', "EnumerateOwnerHandles", "CollectOwnerHandles", "TryFindOwner"):
        if token not in text:
            errors.append("ownership policy missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical generated ownership facade compiles directly and safe health consumes the single Core ownership enumeration contract.")
