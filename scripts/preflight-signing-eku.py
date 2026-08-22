#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SIGNER = ROOT / "scripts" / "sign-v25.ps1"
CODE_SIGNING_OID = "1.3.6.1.5.5.7.3.3"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def eku_allows_code_signing(eku_oids):
    if eku_oids is None:
        return False
    return CODE_SIGNING_OID in eku_oids


def main() -> int:
    if not SIGNER.is_file():
        raise AssertionError("missing scripts/sign-v25.ps1")
    text = SIGNER.read_text(encoding="utf-8")

    required_tokens = (
        "$codeSigningOid = '1.3.6.1.5.5.7.3.3'",
        "$_.Oid.Value -eq '2.5.29.37'",
        "does not expose an Enhanced Key Usage extension",
        "New-Object Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension",
        "$enhancedEku.CopyFrom($eku)",
        "$enhancedEku.EnhancedKeyUsages | Where-Object { $_.Value -eq $codeSigningOid }",
        "if (-not @(",
        "is not valid for Code Signing ($codeSigningOid)",
        "Set-AuthenticodeSignature",
    )
    for token in required_tokens:
        require(token in text, "signing EKU guard missing token: " + token)

    require("$eku.Format(" not in text, "signing EKU authorization must not depend on localized/rendered extension text")
    require("-notmatch 'Code Signing'" not in text and "-match 'Code Signing'" not in text, "signing EKU authorization must not depend on the English friendly name")

    cases = (
        (None, False, "missing EKU extension"),
        ([CODE_SIGNING_OID], True, "Code Signing EKU"),
        (["1.3.6.1.5.5.7.3.2"], False, "Client Authentication only"),
        (["1.3.6.1.5.5.7.3.1"], False, "Server Authentication only"),
        (["1.3.6.1.5.5.7.3.2", CODE_SIGNING_OID], True, "mixed EKUs including Code Signing"),
    )
    for eku_oids, expected, label in cases:
        actual = eku_allows_code_signing(eku_oids)
        require(actual is expected, f"EKU OID model mismatch for {label}: expected {expected}, got {actual}")

    eku_lookup = text.find("$eku = $certificate.Extensions")
    eku_copy = text.find("$enhancedEku.CopyFrom($eku)")
    eku_filter = text.find("$enhancedEku.EnhancedKeyUsages | Where-Object { $_.Value -eq $codeSigningOid }")
    eku_refusal = text.find("is not valid for Code Signing ($codeSigningOid)")
    cert_resolve = text.find("$certificate = Get-CodeSigningCertificate -Thumbprint $CertificateThumbprint")
    sign_call = text.find("$signature = Set-AuthenticodeSignature")
    positions = (eku_lookup, eku_copy, eku_filter, eku_refusal, cert_resolve, sign_call)
    require(min(positions) >= 0, "signing EKU/signing ordering token is missing")
    require(
        eku_lookup < eku_copy < eku_filter < eku_refusal < cert_resolve < sign_call,
        "certificate EKU OID validation must complete before any Set-AuthenticodeSignature call",
    )

    print("PASS: V25 signing requires a structured EKU extension containing the exact Code Signing OID before Authenticode signing and does not depend on localized extension display text.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
