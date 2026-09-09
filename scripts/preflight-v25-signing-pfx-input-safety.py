#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SCRIPT = REPO_ROOT / "scripts" / "import-v25-signing-certificate.ps1"
MAX_SOURCE_BYTES = 128 * 1024


def fail(message: str) -> None:
    raise SystemExit(f"::error::{message}")


def load_source() -> str:
    try:
        stat = SCRIPT.stat()
    except OSError as exc:
        fail(f"cannot stat signing PFX importer: {exc}")
    if not SCRIPT.is_file() or SCRIPT.is_symlink():
        fail("signing PFX importer must be an ordinary non-symlink file")
    if stat.st_size > MAX_SOURCE_BYTES:
        fail(f"signing PFX importer unexpectedly exceeds {MAX_SOURCE_BYTES} bytes")
    try:
        return SCRIPT.read_bytes().decode("utf-8", errors="strict")
    except (OSError, UnicodeDecodeError) as exc:
        fail(f"cannot read signing PFX importer as strict UTF-8: {exc}")


def require(source: str, needle: str, label: str) -> int:
    index = source.find(needle)
    if index < 0:
        fail(f"missing {label}: {needle}")
    return index


def require_after(source: str, needle: str, after_index: int, label: str) -> int:
    index = source.find(needle, after_index)
    if index < 0:
        fail(f"missing {label} after offset {after_index}: {needle}")
    return index


def require_before(source: str, first: str, second: str, label: str) -> None:
    first_index = require(source, first, label + " first token")
    second_index = require(source, second, label + " second token")
    if first_index >= second_index:
        fail(f"{label}: expected {first!r} before {second!r}")


def main() -> None:
    source = load_source()

    # Preserve bounded secret admission before any certificate/key-store work.
    require(source, "[Security.SecureString] $Password", "SecureString password parameter")
    require(source, "$maxPfxDecodedBytes = 1048576", "decoded PFX limit")
    require(source, "$maxPfxBase64Chars = 1398104", "encoded PFX limit")
    require(source, "if ($encodedPfx.Length -gt $maxPfxBase64Chars)", "pre-decode encoded-size guard")
    require_before(
        source,
        "if ($encodedPfx.Length -gt $maxPfxBase64Chars)",
        "[Convert]::FromBase64String($encodedPfx)",
        "encoded-size rejection must precede base64 allocation",
    )
    require(
        source,
        "if ($bytes.Length -lt 256 -or $bytes.Length -gt $maxPfxDecodedBytes)",
        "decoded-size guard",
    )
    require_before(
        source,
        "if ($bytes.Length -lt 256 -or $bytes.Length -gt $maxPfxDecodedBytes)",
        "$certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new()",
        "decoded-size admission must precede persistent certificate import",
    )

    # The expected certificate must be absent before this attempt and the bounded
    # bytes must be validated before a persisted key is created. Validate the
    # probe call by ordered semantic tokens instead of indentation/newline shape.
    require(source, "if ($existing -contains $expected)", "pre-existing expected-certificate rejection")
    probe_start = require(source, "$probeCertificate.Import(", "non-persistent probe import")
    probe_bytes = require_after(source, "$bytes,", probe_start, "probe byte input")
    probe_password = require_after(source, "$Password,", probe_bytes, "probe SecureString password input")
    probe_flag = require_after(
        source,
        "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet",
        probe_password,
        "probe non-persistent UserKeySet flag",
    )
    probe_end = require_after(source, ")", probe_flag, "probe import close")
    require(source, "$probeCertificate.HasPrivateKey", "probe private-key admission")
    require(source, "Test-CodeSigningEku $probeCertificate", "probe Code Signing EKU admission")
    require(source, "$probeCertificate.NotBefore", "probe NotBefore admission")
    require(source, "$probeCertificate.NotAfter", "probe NotAfter admission")
    require(source, "$probeCertificate.Dispose()", "probe certificate disposal")

    # Persist only after the probe succeeds. Check the flag composition by token
    # order so harmless PowerShell formatting cannot weaken or break the guard.
    certificate_new = require(
        source,
        "$certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new()",
        "persistent in-memory certificate",
    )
    key_flags_start = require_after(source, "$keyStorageFlags =", certificate_new, "persistent key-storage flags")
    persistent_user_flag = require_after(
        source,
        "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet",
        key_flags_start,
        "persistent CurrentUser key flag",
    )
    persistent_persist_flag = require_after(
        source,
        "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet",
        persistent_user_flag,
        "persistent key flag",
    )
    memory_import = "$certificate.Import($bytes, $Password, $keyStorageFlags)"
    memory_import_index = require_after(source, memory_import, persistent_persist_flag, "direct in-memory PFX import")
    if probe_end >= memory_import_index:
        fail("probe validation must precede persisted-key import")

    store_ctor = "[Security.Cryptography.X509Certificates.X509Store]::new("
    store_open = "$store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)"
    rollback_arm = "$importedNewThumbprints = @($expected)"
    store_add = "$store.Add($certificate)"
    require(source, store_ctor, "X509Store constructor")
    require(source, "[Security.Cryptography.X509Certificates.StoreName]::My", "My store identity")
    require(source, "[Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser", "CurrentUser store identity")
    require(source, store_open, "read/write certificate-store open")
    require(source, rollback_arm, "bounded rollback ownership arm")
    require(source, store_add, "held-store certificate add")
    require_before(source, memory_import, store_open, "memory import must precede store open")
    require_before(source, store_open, rollback_arm, "store must be open before rollback ownership is armed")
    require_before(source, rollback_arm, store_add, "rollback ownership must be armed before store add")

    # Preserve exact expected-certificate admission and bounded rollback.
    require(source, "if ($candidates.Count -ne 1)", "exactly-one expected certificate admission")
    require(source, "$candidate.NotBefore", "imported NotBefore admission")
    require(source, "$candidate.NotAfter", "imported NotAfter admission")
    require(source, "$importedNewThumbprints -notcontains $expected", "new-import proof")
    rollback = "Remove-ImportedCertificates -Thumbprints $importedNewThumbprints"
    require(source, rollback, "bounded certificate rollback")
    require(source, "$operationError = $_.Exception", "operation failure preservation")
    require(source, "Signing operation failed and imported certificate cleanup also failed.", "aggregate operation/rollback failure")

    store_close = "$store.Close()"
    certificate_dispose = "$certificate.Dispose()"
    byte_zero = "[Array]::Clear($bytes, 0, $bytes.Length)"
    require(source, store_close, "store close")
    require(source, certificate_dispose, "persistent certificate disposal")
    require(source, byte_zero, "decoded secret zeroing")
    require_before(source, store_add, store_close, "store consumption must precede store close")
    require_before(source, store_add, certificate_dispose, "store consumption must precede certificate disposal")
    require_before(source, store_close, byte_zero, "store close must precede decoded secret zeroing")
    require_before(source, certificate_dispose, byte_zero, "certificate disposal must precede decoded secret zeroing")

    # The secret must never return to a filesystem pathname and the password must
    # never be converted to plaintext for import.
    forbidden = (
        "[IO.File]::WriteAllBytes(",
        "Import-PfxCertificate",
        "Assert-SafeTempDirectory",
        "Assert-SafeTempFile",
        "$pfxPath",
        "RUNNER_TEMP",
        "GetTempPath()",
        "Remove-Item -LiteralPath $pfxPath",
        "[Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable",
        "SecureStringToBSTR",
        "PtrToString",
        "GetNetworkCredential().Password",
        "Get-ChildItem -Path Cert:\\CurrentUser\\My | Remove-Item",
        "Remove-Item -Path Cert:\\CurrentUser\\My",
        "taskkill",
    )
    for token in forbidden:
        if token in source:
            fail(f"signing PFX importer contains forbidden pathname/plaintext/broad-cleanup primitive: {token}")

    print("V25 signing PFX bounded in-memory input/rollback safety preflight: PASS")


if __name__ == "__main__":
    main()
