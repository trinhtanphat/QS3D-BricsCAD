#!/usr/bin/env python3
"""Fail closed if V25 signing PFX bytes are materialized/reopened by pathname."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "import-v25-signing-certificate.ps1"

CERTIFICATE = "[Security.Cryptography.X509Certificates.X509Certificate2]::new()"
USER_KEY_SET = "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet"
PERSIST_KEY_SET = "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet"
PERSISTENT_FLAGS = (
    "$keyStorageFlags = [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet -bor\n"
    "        [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet"
)
MEMORY_IMPORT = "$certificate.Import($bytes, $Password, $keyStorageFlags)"
STORE = "[Security.Cryptography.X509Certificates.X509Store]::new("
STORE_MY = "[Security.Cryptography.X509Certificates.StoreName]::My"
STORE_CURRENT_USER = "[Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser"
STORE_OPEN = "$store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)"
STORE_ADD = "$store.Add($certificate)"
STORE_CLOSE = "$store.Close()"
CERT_DISPOSE = "$certificate.Dispose()"
BYTE_ZERO = "[Array]::Clear($bytes, 0, $bytes.Length)"
EKU_FUNCTION = "function Test-CodeSigningEku {"

FORBIDDEN = (
    "[IO.File]::WriteAllBytes(",
    "Import-PfxCertificate",
    "Assert-SafeTempDirectory",
    "Assert-SafeTempFile",
    "$pfxPath",
    "RUNNER_TEMP",
    "GetTempPath()",
    "Remove-Item -LiteralPath $pfxPath",
    "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable",
    "X509Certificate2Collection]::new()",
    "$collection.Import(",
)


def validate(source: str) -> list[str]:
    failures: list[str] = []
    required = (
        (CERTIFICATE, "in-memory X509Certificate2 creation is missing"),
        (USER_KEY_SET, "CurrentUser key-set flag is missing"),
        (PERSIST_KEY_SET, "private-key persistence flag is missing"),
        (PERSISTENT_FLAGS, "persistent CurrentUser non-exportable key flags assignment is missing"),
        (MEMORY_IMPORT, "decoded PFX bytes are not imported directly from memory with SecureString password"),
        (STORE, "CurrentUser certificate-store handle is missing"),
        (STORE_MY, "My store identity is missing"),
        (STORE_CURRENT_USER, "CurrentUser store identity is missing"),
        (STORE_OPEN, "certificate store is not opened read/write"),
        (STORE_ADD, "imported certificate is not added through the held store object"),
        (STORE_CLOSE, "certificate store is not closed in cleanup"),
        (CERT_DISPOSE, "in-memory certificate object is not disposed"),
        (BYTE_ZERO, "decoded PFX bytes are not zeroed in finally"),
        ("$importedNewThumbprints", "newly imported certificate ownership tracking is missing"),
        ("Remove-ImportedCertificates -Thumbprints $importedNewThumbprints", "failure cleanup is not bounded to newly imported thumbprints"),
        (EKU_FUNCTION, "Code Signing EKU admission helper was removed"),
        ("$candidate.NotBefore", "certificate validity admission was removed"),
        ("$candidate.NotAfter", "certificate validity admission was removed"),
        ("$importedNewThumbprints -notcontains $expected", "expected certificate must be proven newly imported"),
    )
    for token, message in required:
        if token not in source:
            failures.append(message)

    for token in FORBIDDEN:
        if token in source:
            failures.append(f"temporary/pathname or invalid PFX primitive is forbidden: {token}")

    certificate = source.find(CERTIFICATE)
    flags = source.find(PERSISTENT_FLAGS, certificate if certificate >= 0 else 0)
    memory_import = source.find(MEMORY_IMPORT, flags if flags >= 0 else 0)
    store = source.find(STORE, memory_import if memory_import >= 0 else 0)
    store_open = source.find(STORE_OPEN, store if store >= 0 else 0)
    store_add = source.find(STORE_ADD, store_open if store_open >= 0 else 0)
    if not (0 <= certificate < flags < memory_import < store < store_open < store_add):
        failures.append(
            "PFX admission must construct an in-memory X509Certificate2, import bytes with SecureString and non-exportable CurrentUser persistence, then add through an explicitly opened CurrentUser/My store"
        )

    close_pos = source.rfind(STORE_CLOSE)
    dispose_pos = source.rfind(CERT_DISPOSE)
    clear_pos = source.rfind(BYTE_ZERO)
    if min(close_pos, dispose_pos, clear_pos) < store_add or not store_add < close_pos < clear_pos or not store_add < dispose_pos < clear_pos:
        failures.append("store close, certificate disposal and decoded-byte zeroing must remain after certificate-store consumption")

    return failures


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)
    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    required_mutations = (
        (CERTIFICATE, "in-memory certificate"),
        (PERSISTENT_FLAGS, "persistent CurrentUser non-exportable key flags"),
        (MEMORY_IMPORT, "memory import"),
        (STORE, "certificate-store constructor"),
        (STORE_MY, "My store"),
        (STORE_CURRENT_USER, "CurrentUser store"),
        (STORE_OPEN, "store open"),
        (STORE_ADD, "store add"),
        (STORE_CLOSE, "store close"),
        (CERT_DISPOSE, "certificate disposal"),
        (BYTE_ZERO, "decoded-byte zeroing"),
        ("Remove-ImportedCertificates -Thumbprints $importedNewThumbprints", "bounded failure cleanup"),
        (EKU_FUNCTION, "Code Signing EKU admission helper"),
        ("$candidate.NotBefore", "NotBefore admission"),
        ("$candidate.NotAfter", "NotAfter admission"),
        ("$importedNewThumbprints -notcontains $expected", "new-import proof"),
    )
    for token, label in required_mutations:
        if token not in source:
            print(f"FAIL: mutation fixture missing: {label}")
            return 1
        mutated = source.replace(token, "MUTATED-PFX-MEMORY-IMPORT", 1)
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    insertion = source.find(MEMORY_IMPORT)
    if insertion < 0:
        print("FAIL: memory-import mutation insertion point is missing")
        return 1
    for unsafe in FORBIDDEN:
        if unsafe == "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable":
            injected = source[:insertion] + unsafe + "\n" + source[insertion:]
        else:
            injected = source[:insertion] + "# adversarial mutation\n" + unsafe + "\n" + source[insertion:]
        if not validate(injected):
            print(f"FAIL: guard mutation escaped forbidden primitive: {unsafe}")
            return 1

    print("PASS: V25 signing PFX stays in bounded memory and never enters a temporary pathname")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
