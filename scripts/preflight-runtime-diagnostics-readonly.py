#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"

errors = []

if not PATH.is_file():
    errors.append("missing RuntimeDiagnosticsCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]' not in text:
        errors.append("QS3DRUNTIMECHECK command registration is missing.")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append("QS3DRUNTIMECHECK must resolve project state read-only when available.")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DRUNTIMECHECK must not create/cache project state merely to inspect runtime/package metadata.")
    if "runtime diagnostics remain read-only and do not create project state" not in text:
        errors.append("QS3DRUNTIMECHECK must explain its no-project read-only behavior.")
    for token in (
        "private const int ExpectedRuntimeMajor = 26;",
        "private const int ExpectedRuntimeMajor = 25;",
        "var expectedRuntime = Major(brxAssembly) == ExpectedRuntimeMajor && Major(tdAssembly) == ExpectedRuntimeMajor;",
        "var ok = expectedRuntime && x64Runtime && packageVersionMatches && diskVersionMatches && diskFingerprintMatches;",
    ):
        if token not in text:
            errors.append("Runtime host-major qualification contract is missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DRUNTIMECHECK inspects the compile-time V25/V26 host-major/package and loaded/on-disk binary identity state independently of optional semantic project presence and never creates project state.")
