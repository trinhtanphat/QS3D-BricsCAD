#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"

errors = []

if not SOURCE.is_file():
    errors.append("runtime diagnostics source is missing")
else:
    text = SOURCE.read_text(encoding="utf-8")

    if "Project: not loaded/persisted" in text:
        errors.append("legacy ambiguous project-state wording is still present")

    required_display = (
        'ProjectContextCoordinator.TryGetCached(document, out _)',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'Project state: AVAILABLE',
        'Project source: ',
        'LOADED in memory',
        'PERSISTED sidecar loaded read-only',
        'Project state: UNAVAILABLE',
        'NOT LOADED in memory; no PERSISTED sidecar found',
        'Diagnostics access: READ-ONLY',
        'READ-ONLY; no project state was created',
        'Loaded DLL path: ',
        'Module MVID: ',
        'Loaded-at-start SHA256: ',
        'On-disk DLL SHA256: ',
        'Package product version: ',
        'Package assembly version: ',
    )
    for token in required_display:
        if token not in text:
            errors.append("runtime display contract missing token: " + token)

    preserved_identity_logic = (
        'ProductVersionsEqual(pluginProductVersion, diskIdentity.ProductVersion)',
        'diskFingerprintMatches',
        'ComputeSha256(_loadedBinaryPath)',
        'ReadPackageMetadata(metadataPath)',
        'ExpectedRuntimeMajor',
        'STALE PROCESS',
        'QS3DRUNTIMECHECK PASS',
        'QS3DRUNTIMECHECK FAIL',
    )
    for token in preserved_identity_logic:
        if token not in text:
            errors.append("runtime identity semantics unexpectedly changed/missing: " + token)

if not V26.is_file():
    errors.append("V26 adapter project is missing")
else:
    text = V26.read_text(encoding="utf-8")
    for token in (
        '<DefineConstants>$(DefineConstants);BRICSCAD_V26</DefineConstants>',
        '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
    ):
        if token not in text:
            errors.append("V26 shared-source contract missing token: " + token)

print("QS3D runtime diagnostics display preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: runtime diagnostics expose explicit loaded/persisted/read-only project state and clip-resistant identity labels while preserving version/fingerprint/stale-process semantics for the shared V25/V26 source.")
