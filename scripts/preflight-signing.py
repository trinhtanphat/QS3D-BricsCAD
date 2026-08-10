#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

sign = ROOT / "scripts/sign-v25.ps1"
verify = ROOT / "scripts/verify-v25-signatures.ps1"
for path in (sign, verify):
    if not path.is_file(): errors.append("missing signing file: " + str(path.relative_to(ROOT)))

if sign.is_file():
    text = sign.read_text(encoding="utf-8")
    for needle in (
        "Cert:\\CurrentUser\\My", "HasPrivateKey", "1.3.6.1.5.5.7.3.3",
        "Set-AuthenticodeSignature", "-HashAlgorithm SHA256", "-TimestampServer $TimestampServer",
        "Get-AuthenticodeSignature", "SignatureStatus]::Valid", "SupportsShouldProcess"
    ):
        if needle not in text: errors.append("sign-v25.ps1 missing guard/token: " + needle)
    if not re.search(r"ValidatePattern\('\^https://", text):
        errors.append("sign-v25.ps1 must require an HTTPS timestamp server")
    if re.search(r"(?i)\b(pfx|pfxpassword|password|securestring)\b", text):
        errors.append("sign-v25.ps1 must not accept PFX/private-key passwords; use the Windows certificate store")
    if re.search(r"(?i)SECURELOAD\s*[=:]|setvar[^\n]*SECURELOAD", text):
        errors.append("sign-v25.ps1 must not lower BricsCAD SECURELOAD")

if verify.is_file():
    text = verify.read_text(encoding="utf-8")
    for needle in (
        "Get-AuthenticodeSignature", "SignatureStatus]::Valid", "ExpectedThumbprint",
        "TimeStamperCertificate", "Missing trusted timestamp"
    ):
        if needle not in text: errors.append("verify-v25-signatures.ps1 missing guard/token: " + needle)

for path in ROOT.rglob("*.pfx"):
    errors.append("private signing certificate must not be committed: " + str(path.relative_to(ROOT)))
for path in ROOT.rglob("*.p12"):
    errors.append("private signing certificate must not be committed: " + str(path.relative_to(ROOT)))

print("QS3D V25 signing preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Authenticode signing uses the Windows certificate store, SHA-256, HTTPS timestamping, post-sign verification and no committed PFX/P12 material.")
