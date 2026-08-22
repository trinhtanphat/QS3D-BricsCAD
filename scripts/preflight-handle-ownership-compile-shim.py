#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

targets = ROOT / "src/QS3D.Core/Directory.Build.targets"
shim = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.Safe.cs"
safe = ROOT / "src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs"
policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
legacy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.cs"

for path in (targets, shim, safe, policy, legacy):
    if not path.is_file():
        errors.append("missing ownership compile-shim file: " + str(path.relative_to(ROOT)))

if targets.is_file():
    text = targets.read_text(encoding="utf-8")
    if '<Compile Remove="Diagnostics/GeneratedHandleOwnershipHealthService.cs" />' not in text:
        errors.append("Core Directory.Build.targets must exclude the broad transitional ownership scanner")

if shim.is_file():
    text = shim.read_text(encoding="utf-8")
    for token in (
        "public sealed class GeneratedHandleOwnershipHealthService",
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
    ):
        if token not in text:
            errors.append("ownership compile shim missing token: " + token)

if safe.is_file():
    text = safe.read_text(encoding="utf-8")
    if "GeneratedHandleOwnershipPolicy.IsOwnerSlot" not in text:
        errors.append("safe ownership scanner must use GeneratedHandleOwnershipPolicy")

if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for token in ('StartsWith("Generated"', 'PhysicalOpeningCutSolidHandle'):
        if token not in text:
            errors.append("ownership policy missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: transitional broad ownership scanner stays in history but is excluded from Core compile; legacy API delegates to provenance-safe owner-slot policy.")
