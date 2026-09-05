#!/usr/bin/env python3
"""Guard finalized V25 ZIPs against manifest/archive byte drift."""

from __future__ import annotations

import hashlib
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def _manifest(records: list[tuple[str, bytes]]) -> bytes:
    return "".join(
        f"{hashlib.sha256(payload).hexdigest().upper()}  {name}\n"
        for name, payload in records
    ).encode("ascii")


def _write_zip(path: Path, entries: list[tuple[str, bytes]]) -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, payload in entries:
            archive.writestr(name, payload)


def _powershell() -> str | None:
    return shutil.which("pwsh") or shutil.which("powershell") or shutil.which("powershell.exe")


def _run_behavioral_fixtures(verifier: str, failures: list[str]) -> None:
    shell = _powershell()
    if not shell:
        failures.append("PowerShell is required to execute finalized ZIP manifest behavioral fixtures")
        return

    with tempfile.TemporaryDirectory(prefix="qs3d-v25-zip-manifest-") as temp_dir:
        temp = Path(temp_dir)
        harness = temp / "verify.ps1"
        harness.write_text(
            "function Assert-SafeFile { param([string]$Path, [string]$Label) return [IO.Path]::GetFullPath($Path) }\n"
            + verifier
            + "\ntry { $null = Assert-ZipManifestIntegrity -ZipPath $env:QS3D_TEST_ZIP; exit 0 } "
            + "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 23 }\n",
            encoding="utf-8",
        )

        alpha = b"alpha payload\n"
        beta = b"beta payload\x00\x01"
        fixtures: list[tuple[str, bool, list[tuple[str, bytes]]]] = []

        valid_manifest = _manifest([("alpha.txt", alpha), ("nested/beta.bin", beta)])
        fixtures.append(
            (
                "valid exact coverage",
                True,
                [("alpha.txt", alpha), ("nested/beta.bin", beta), ("SHA256SUMS.txt", valid_manifest)],
            )
        )
        fixtures.append(
            (
                "mutated payload after manifest creation",
                False,
                [("alpha.txt", alpha + b"MUTATED"), ("SHA256SUMS.txt", _manifest([("alpha.txt", alpha)]))],
            )
        )
        fixtures.append(
            (
                "missing manifest record",
                False,
                [("alpha.txt", alpha), ("nested/beta.bin", beta), ("SHA256SUMS.txt", _manifest([("alpha.txt", alpha)]))],
            )
        )
        fixtures.append(
            (
                "extra manifest record",
                False,
                [
                    ("alpha.txt", alpha),
                    ("SHA256SUMS.txt", _manifest([("alpha.txt", alpha), ("ghost.txt", b"ghost")]))
                ],
            )
        )
        fixtures.append(
            (
                "malformed lowercase hash",
                False,
                [
                    ("alpha.txt", alpha),
                    (
                        "SHA256SUMS.txt",
                        f"{hashlib.sha256(alpha).hexdigest()}  alpha.txt\n".encode("ascii"),
                    ),
                ],
            )
        )
        fixtures.append(
            (
                "unsafe manifest traversal",
                False,
                [
                    ("alpha.txt", alpha),
                    ("SHA256SUMS.txt", _manifest([("../alpha.txt", alpha)])),
                ],
            )
        )
        duplicate_manifest = _manifest([("Alpha.txt", alpha), ("alpha.txt", beta)])
        fixtures.append(
            (
                "case-insensitive duplicate archive entry",
                False,
                [("Alpha.txt", alpha), ("alpha.txt", beta), ("SHA256SUMS.txt", duplicate_manifest)],
            )
        )
        fixtures.append(
            (
                "oversized checksum manifest",
                False,
                [("alpha.txt", alpha), ("SHA256SUMS.txt", b"X" * (4 * 1024 * 1024 + 1))],
            )
        )

        for index, (label, should_pass, entries) in enumerate(fixtures):
            archive_path = temp / f"fixture-{index}.zip"
            _write_zip(archive_path, entries)
            env = dict(**__import__("os").environ, QS3D_TEST_ZIP=str(archive_path))
            result = subprocess.run(
                [shell, "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(harness)],
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                timeout=30,
                check=False,
            )
            passed = result.returncode == 0
            if passed != should_pass:
                failures.append(
                    f"behavioral fixture '{label}' expected {'PASS' if should_pass else 'FAIL'}, "
                    f"got rc={result.returncode}; stderr={result.stderr.strip()[:500]}"
                )

        directory_traversal = temp / "fixture-directory-traversal.zip"
        with zipfile.ZipFile(directory_traversal, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            info = zipfile.ZipInfo("../")
            info.external_attr = 0o40775 << 16
            archive.writestr(info, b"")
            archive.writestr("alpha.txt", alpha)
            archive.writestr("SHA256SUMS.txt", _manifest([("alpha.txt", alpha)]))
        env = dict(**__import__("os").environ, QS3D_TEST_ZIP=str(directory_traversal))
        result = subprocess.run(
            [shell, "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(harness)],
            env=env,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=30,
            check=False,
        )
        if result.returncode == 0:
            failures.append("behavioral fixture 'directory traversal entry' expected FAIL, got PASS")


def main() -> int:
    source = FINALIZER.read_text(encoding="utf-8")
    failures: list[str] = []

    function_start = source.find("function Assert-ZipManifestIntegrity")
    call_site = source.find("$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip")
    zip_shape_check = source.find("Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package")
    replace_call = source.find("[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)")

    required = (
        "function Assert-ZipManifestIntegrity",
        "$fileStream = [IO.FileStream]::new(",
        "[IO.FileShare]::Read",
        "[IO.Compression.ZipArchive]::new($fileStream",
        "SHA256SUMS.txt",
        "$entry.Open()",
        "[Security.Cryptography.SHA256]::Create()",
        "^([0-9A-F]{64})  (.+)$",
        "case-insensitive duplicate",
        "checksum manifest coverage mismatch",
        "checksum mismatch",
        "$fileStream.Position = 0",
        "$outerDigest = $outerHash.ComputeHash($fileStream)",
        "$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip",
    )
    for token in required:
        if token not in source:
            failures.append(f"finalized ZIP byte-integrity contract is incomplete; missing: {token}")

    verifier = ""
    if min(function_start, call_site, zip_shape_check, replace_call) < 0:
        failures.append("could not bound finalized ZIP shape/manifest/generation validation")
    elif not (zip_shape_check < call_site < replace_call):
        failures.append("completed ZIP must pass shape then same-handle manifest-entry byte validation/digest admission before publication")

    if function_start >= 0:
        function_end = source.find("\nfunction ", function_start + 1)
        if function_end < 0:
            function_end = len(source)
        verifier = source[function_start:function_end]
        verifier_required = (
            "$manifestEntries.Count -ne 1",
            "$seenManifestPaths",
            "$archivePayloadPaths",
            "$manifestPayloadPaths",
            "$hash.ComputeHash($stream)",
            "$manifestEntry.Length -gt 4MB",
            "must not hash itself",
            "$rawName = $entry.FullName.Replace",
            "$isDirectory = [string]::IsNullOrEmpty($entry.Name)",
            "$rawName.TrimEnd('/')",
            "if ($isDirectory) { continue }",
            "$fileStream.Position = 0",
            "$outerHash.ComputeHash($fileStream)",
        )
        for token in verifier_required:
            if token not in verifier:
                failures.append(f"ZIP manifest verifier is not fail-closed enough; missing: {token}")

    if "Get-FileHash -LiteralPath $tempZip" in source:
        failures.append("outer digest must come from the same locked stream as manifest verification, not a pathname reopen")

    if not failures and verifier:
        _run_behavioral_fixtures(verifier, failures)

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: finalized V25 ZIP validates actual entry bytes and admits its outer digest from one locked generation with adversarial fixtures")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
