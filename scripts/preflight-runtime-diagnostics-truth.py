#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs"
errors = []

if not source.is_file():
    errors.append("missing RuntimeDiagnosticsCommands.cs")
else:
    text = source.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DRUNTIMECHECK"',
        "signatureMetadataRecorded",
        "Package signature metadata:",
        "Recorded signer thumbprint:",
        "Authenticode: metadata only here; cryptographic publisher/timestamp verification belongs to the signed installer/release gate.",
    )
    for needle in required:
        if needle not in text:
            errors.append("runtime diagnostics missing truthfulness token: " + needle)

    misleading = (
        '" • signature=" + (packageSigned ? "signed"',
        'QS3DRUNTIMECHECK PASS: signed package',
        'Authenticode verified',
    )
    for needle in misleading:
        if needle in text:
            errors.append("runtime diagnostics must not claim cryptographic signature verification from package metadata: " + needle)

print("QS3D runtime diagnostics truthfulness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DRUNTIMECHECK reports recorded package signing metadata without claiming Authenticode publisher/timestamp verification; cryptographic trust remains in the signed installer/release gate.")
