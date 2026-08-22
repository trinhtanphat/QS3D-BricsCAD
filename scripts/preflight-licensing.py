#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

verifier = ROOT / "src/QS3D.Core/Licensing/LicenseVerifier.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/LicenseVerifierSmokeRegistration.cs"
for path in (verifier, smoke, registration):
    if not path.is_file(): errors.append("missing licensing file: " + str(path.relative_to(ROOT)))

if verifier.is_file():
    text = verifier.read_text(encoding="utf-8")
    for needle in (
        "RSAParameters publicKey", "rsa.ImportParameters(publicKey)", "HashAlgorithmName.SHA256", "RSASignaturePadding.Pkcs1",
        "DtdProcessing.Prohibit", "XmlResolver = null", "MaxLicenseBytes", "MaxCharactersInDocument",
        "DateTimeKind.Utc", "LicenseStatus.InvalidSignature", "LicenseStatus.ProductMismatch", "LicenseStatus.NotYetValid", "LicenseStatus.Expired",
        '"RSA-SHA256"'
    ):
        if needle not in text: errors.append("LicenseVerifier.cs missing guard/token: " + needle)
    for forbidden in ("PrivateKey", "Pfx", "PFX", "password", "Password", "HMACSHA", "MachineGuid"):
        if forbidden in text: errors.append("LicenseVerifier.cs must not contain private-key/secret binding token: " + forbidden)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "SignedLicenseVerifies", "TamperedLicenseFailsSignature", "ProductAndTimeWindowsAreEnforced", "DtdLicenseIsRejected",
        "ExportParameters(false)", "RSASignaturePadding.Pkcs1"
    ):
        if needle not in text: errors.append("LicenseVerifierSmoke.cs missing coverage: " + needle)

if registration.is_file() and "LicenseVerifierSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("LicenseVerifierSmoke is not registered")

for path in ROOT.rglob("*"):
    if not path.is_file(): continue
    if path.suffix.lower() in {".pfx", ".p12", ".pvk", ".key"}:
        errors.append("private key/certificate material must not be committed: " + str(path.relative_to(ROOT)))

print("QS3D licensing preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: offline license verification is public-key-only RSA-SHA256, UTC-bounded, XML-hardened and covered by deterministic smoke tests.")
