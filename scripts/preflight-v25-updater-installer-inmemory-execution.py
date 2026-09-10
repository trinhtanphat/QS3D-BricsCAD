#!/usr/bin/env python3
"""Fail closed unless update-v25 executes source read from the held installer object."""

from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"ERROR: V25 updater in-memory installer preflight: {message}")


def require(text: str, token: str, label: str, start: int = 0) -> int:
    index = text.find(token, start)
    if index < 0:
        fail(f"missing {label}: {token}")
    return index


def validate(source: str) -> None:
    temp_create = require(source, "New-Item -ItemType Directory -Path $tempRoot", "temporary-root creation")
    temp_hold = require(source, "$heldTempRoot = Open-HeldVerifiedDirectory -Path $tempRoot", "temporary-root directory hold", temp_create)
    expand = require(source, "Expand-VerifiedHeldArchive", "bounded archive extraction", temp_hold)
    extract_hold = require(source, "$heldExtractRoot = Open-HeldVerifiedDirectory -Path $extractRoot", "extraction-root directory hold", expand)
    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition", extract_hold)
    admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "final package admission", acquire)

    stream = require(source, "$installerStream", "held installer stream", acquire)
    if stream > admission:
        fail("held installer stream must be opened before final package admission")

    stream_ctor = re.search(
        r"\$installerStream\s*=\s*\[IO\.FileStream\]::new\(\s*\$heldInstaller\.Handle\s*,\s*\[IO\.FileAccess\]::Read\s*\)",
        source[acquire:admission],
        re.IGNORECASE,
    )
    if stream_ctor is None:
        fail("installer source must be read through a FileStream bound to $heldInstaller.Handle")

    strict_utf8 = require(source, "[Text.UTF8Encoding]::new($false, $true)", "strict UTF-8 decoder", stream)
    reader = require(source, "$installerReader", "held installer reader", strict_utf8)
    reader_ctor = re.search(
        r"\$installerReader\s*=\s*\[IO\.StreamReader\]::new\(\s*\$installerStream\s*,\s*\$strictUtf8\s*,\s*\$false\s*,\s*4096\s*,\s*\$true\s*\)",
        source[strict_utf8:admission],
        re.IGNORECASE,
    )
    if reader_ctor is None:
        fail("installer reader must disable BOM auto-detection and leave the held stream open")

    installer_text = require(source, "$installerText", "held installer text", reader)
    read = require(source, "$installerReader.ReadToEnd()", "held installer read", installer_text)
    script = require(source, "[ScriptBlock]::Create($installerText)", "in-memory installer ScriptBlock", read)
    invoke = require(source, "& $installerScript @arguments", "in-memory installer invocation", admission)
    if re.search(r"\$heldInstaller\.Dispose\(\)", source[acquire:invoke], re.IGNORECASE):
        fail("held installer is disposed before in-memory invocation")
    installer_dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal", invoke)
    extract_dispose = require(source, "$heldExtractRoot.Dispose()", "extraction-root hold disposal", installer_dispose)
    temp_dispose = require(source, "$heldTempRoot.Dispose()", "temporary-root hold disposal", extract_dispose)
    cleanup = require(source, "Remove-Item -LiteralPath $tempRoot -Recurse -Force", "temporary-root cleanup", temp_dispose)

    if not (
        temp_create < temp_hold < expand < extract_hold < acquire < stream < strict_utf8 < reader
        < installer_text < read < script < admission < invoke < installer_dispose < extract_dispose
        < temp_dispose < cleanup
    ):
        fail("require ancestor holds < held-byte read/script < final admission < invoke < ordered hold disposal < cleanup")

    unsafe_patterns = (
        (r"(?m)^\s*&\s*\$installer\s+@arguments\b", "pathname installer invocation"),
        (r"Get-Content\s+-LiteralPath\s+\$installer\b", "pathname installer source read"),
        (r"\[IO\.File\]::ReadAllText\(\s*\$installer", "pathname ReadAllText installer source read"),
    )
    for pattern, label in unsafe_patterns:
        if re.search(pattern, source[acquire:installer_dispose], re.IGNORECASE):
            fail(f"unsafe {label} remains while the installer is held")

    if source[acquire:installer_dispose].count("& $installerScript @arguments") != 1:
        fail("held installer ScriptBlock must be invoked exactly once")
    if source[acquire:installer_dispose].count("Assert-PackageRoot -Directory $extractRoot") != 1:
        fail("final package admission must occur exactly once while installer object is held")

    finally_block = source[invoke:cleanup]
    if re.search(r"(?mi)^\s*finally\s*\{", finally_block) is None:
        fail("installer and ancestor holds must be released from a real finally block")

    for token, label in (
        ("OpenOrdinaryDirectoryHeld", "native directory hold"),
        ("FILE_FLAG_BACKUP_SEMANTICS", "directory-open semantics"),
        ("FILE_SHARE_WRITE", "directory child-mutation sharing"),
        ("RequireSigned = $true", "signed child enforcement"),
        ("ExpectedSignerThumbprint = $expectedSigner", "expected signer forwarding"),
        ("Enter-Qs3dUpdateMutex", "update serialization mutex"),
        ("Get-Process -Name bricscad", "running-BricsCAD guard"),
    ):
        require(source, token, label)

    if "FILE_SHARE_DELETE" in source:
        fail("directory holds must not grant delete sharing; rename/delete would reopen the path identity race")


def expect_reject(source: str, label: str) -> None:
    try:
        validate(source)
    except SystemExit:
        return
    fail(f"self-test accepted unsafe mutant: {label}")


def fixture() -> str:
    return r'''
FILE_SHARE_WRITE
FILE_FLAG_BACKUP_SEMANTICS
function OpenOrdinaryDirectoryHeld { }
function Open-HeldVerifiedDirectory { }
function Open-HeldVerifiedInstaller { }
function Expand-VerifiedHeldArchive { }
function Enter-Qs3dUpdateMutex { }
Get-Process -Name bricscad
RequireSigned = $true
ExpectedSignerThumbprint = $expectedSigner
$heldTempRoot = $null
$heldExtractRoot = $null
$heldInstaller = $null
try {
 New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
 $heldTempRoot = Open-HeldVerifiedDirectory -Path $tempRoot
 Expand-VerifiedHeldArchive
 $heldExtractRoot = Open-HeldVerifiedDirectory -Path $extractRoot
 $installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
 $heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
 $installerStream = [IO.FileStream]::new($heldInstaller.Handle, [IO.FileAccess]::Read)
 $strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
 $installerReader = [IO.StreamReader]::new($installerStream, $strictUtf8, $false, 4096, $true)
 $installerText = $installerReader.ReadToEnd()
 $installerScript = [ScriptBlock]::Create($installerText)
 Assert-PackageRoot -Directory $extractRoot
 & $installerScript @arguments
}
finally {
 if ($installerReader) { $installerReader.Dispose() }
 if ($installerStream) { $installerStream.Dispose() }
 if ($heldInstaller) { $heldInstaller.Dispose() }
 if ($heldExtractRoot) { $heldExtractRoot.Dispose() }
 if ($heldTempRoot) { $heldTempRoot.Dispose() }
 Remove-Item -LiteralPath $tempRoot -Recurse -Force
}
'''


def self_test() -> None:
    good = fixture()
    validate(good)
    expect_reject(good.replace("& $installerScript @arguments", "& $installer @arguments", 1), "pathname execution")
    expect_reject(good.replace("$heldInstaller.Handle", "$other.Handle", 1), "source read from another handle")
    expect_reject(good.replace("[Text.UTF8Encoding]::new($false, $true)", "[Text.UTF8Encoding]::new($false, $false)", 1), "permissive UTF-8")
    expect_reject(good.replace("$strictUtf8, $false, 4096, $true", "$strictUtf8, $true, 4096, $true", 1), "BOM auto-detection")
    expect_reject(good.replace(" $heldExtractRoot = Open-HeldVerifiedDirectory -Path $extractRoot\n", "", 1), "missing extraction-root hold")
    expect_reject(good.replace(" $heldTempRoot = Open-HeldVerifiedDirectory -Path $tempRoot\n", "", 1), "missing temporary-root hold")
    expect_reject(good.replace(" if ($heldExtractRoot) { $heldExtractRoot.Dispose() }", " if ($heldExtractRoot) { $heldExtractRoot.Dispose() }\n FILE_SHARE_DELETE", 1), "delete sharing")
    expect_reject(good.replace(" Assert-PackageRoot -Directory $extractRoot", " $heldInstaller.Dispose()\n Assert-PackageRoot -Directory $extractRoot", 1), "early installer hold disposal")
    expect_reject(good.replace("$installerReader.ReadToEnd()", "Get-Content -LiteralPath $installer -Raw", 1), "pathname source read")


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print("PASS: V25 updater pins temp/extraction ancestors, reads strict UTF-8 installer source from the held object without BOM decoder switching, executes only that ScriptBlock, and releases all holds before cleanup")
