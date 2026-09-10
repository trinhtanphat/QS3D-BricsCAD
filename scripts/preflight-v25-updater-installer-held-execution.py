#!/usr/bin/env python3
"""Fail closed unless update-v25 atomically pins the admitted installer through execution."""

from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"ERROR: V25 updater installer-hold preflight: {message}")


def require(text: str, token: str, label: str, start: int = 0) -> int:
    index = text.find(token, start)
    if index < 0:
        fail(f"missing {label}: {token}")
    return index


def require_re(text: str, pattern: str, label: str, flags: int = re.IGNORECASE | re.DOTALL) -> re.Match[str]:
    match = re.search(pattern, text, flags)
    if match is None:
        fail(f"missing {label}")
    return match


def strip_csharp_comments(text: str) -> str:
    """Remove // and /* */ comments without treating comment markers inside strings as comments."""
    out: list[str] = []
    i = 0
    state = "code"
    while i < len(text):
        ch = text[i]
        nxt = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if ch == '"':
                state = "string"
                out.append(ch)
            elif ch == "'":
                state = "char"
                out.append(ch)
            elif ch == "/" and nxt == "/":
                state = "line"
                out.extend("  ")
                i += 1
            elif ch == "/" and nxt == "*":
                state = "block"
                out.extend("  ")
                i += 1
            else:
                out.append(ch)
        elif state == "string":
            out.append(ch)
            if ch == "\\" and nxt:
                out.append(nxt)
                i += 1
            elif ch == '"':
                state = "code"
        elif state == "char":
            out.append(ch)
            if ch == "\\" and nxt:
                out.append(nxt)
                i += 1
            elif ch == "'":
                state = "code"
        elif state == "line":
            if ch in "\r\n":
                state = "code"
                out.append(ch)
            else:
                out.append(" ")
        else:
            if ch == "*" and nxt == "/":
                out.extend("  ")
                i += 1
                state = "code"
            else:
                out.append(ch if ch in "\r\n" else " ")
        i += 1
    if state == "block":
        fail("unterminated C# block comment in native helper")
    return "".join(out)


def function_body(source: str, marker: str) -> str:
    start = require(source, marker, marker)
    brace = source.find("{", start)
    if brace < 0:
        fail(f"{marker} has no body")
    depth = 0
    quote: str | None = None
    escaped = False
    for i in range(brace, len(source)):
        ch = source[i]
        if escaped:
            escaped = False
            continue
        if ch == "`":
            escaped = True
            continue
        if quote:
            if ch == quote:
                quote = None
            continue
        if ch in ("'", '"'):
            quote = ch
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[start : i + 1]
    fail(f"{marker} body is unbalanced")
    raise AssertionError


def native_code(source: str) -> str:
    start = require(source, "public static class Qs3dNativeFile", "native helper class")
    end = source.find("'@", start)
    if end < 0:
        fail("native Add-Type here-string is unterminated")
    return strip_csharp_comments(source[start:end])


def require_if_throw(text: str, condition: str, label: str) -> None:
    match = require_re(text, rf"if\s*\(\s*{condition}\s*\)\s*\{{(?P<body>[^{{}}]*)\}}", label)
    if re.search(r"\bthrow\b", match.group("body")) is None:
        fail(f"{label} does not fail closed")


def validate_native(source: str) -> None:
    native = native_code(source)
    constants = {
        "GENERIC_READ": "0x80000000",
        "FILE_SHARE_READ": "0x00000001",
        "OPEN_EXISTING": "3",
        "FILE_FLAG_OPEN_REPARSE_POINT": "0x00200000",
        "FILE_ATTRIBUTE_DIRECTORY": "0x00000010",
        "FILE_ATTRIBUTE_REPARSE_POINT": "0x00000400",
    }
    for name, value in constants.items():
        require_re(native, rf"private\s+const\s+uint\s+{name}\s*=\s*{re.escape(value)}\s*;", f"{name}={value}")

    require_re(native, r"extern\s+SafeFileHandle\s+CreateFileW\s*\(", "CreateFileW SafeFileHandle declaration")
    require_re(native, r"extern\s+bool\s+GetFileInformationByHandle\s*\(", "GetFileInformationByHandle declaration")
    require_re(native, r"extern\s+uint\s+GetFinalPathNameByHandleW\s*\(", "GetFinalPathNameByHandleW declaration")

    opener = function_body(native, "public static Qs3dHeldFile OpenOrdinaryReadHeld")
    require_re(opener, r"SafeFileHandle\s+handle\s*=\s*CreateFileW\s*\(\s*path\s*,\s*GENERIC_READ\s*,\s*FILE_SHARE_READ\s*,\s*IntPtr\.Zero\s*,\s*OPEN_EXISTING\s*,\s*FILE_FLAG_OPEN_REPARSE_POINT\s*,\s*IntPtr\.Zero\s*\)\s*;", "atomic no-follow read-only CreateFileW call")
    require_if_throw(opener, r"handle\s*==\s*null\s*\|\|\s*handle\.IsInvalid", "invalid native handle rejection")
    require_if_throw(opener, r"!GetFileInformationByHandle\s*\(\s*handle\s*,\s*out\s+information\s*\)", "handle attribute query failure rejection")
    require_if_throw(opener, r"\(\s*information\.FileAttributes\s*&\s*FILE_ATTRIBUTE_DIRECTORY\s*\)\s*!=\s*0", "directory rejection")
    require_if_throw(opener, r"\(\s*information\.FileAttributes\s*&\s*FILE_ATTRIBUTE_REPARSE_POINT\s*\)\s*!=\s*0", "reparse-point rejection")
    require_re(opener, r"GetFinalPathNameByHandleW\s*\(\s*handle\s*,\s*resolved\s*,\s*\(uint\)resolved\.Capacity\s*,\s*0\s*\)", "handle final-path query")
    require_if_throw(opener, r"length\s*==\s*0", "final-path API failure rejection")
    require_if_throw(opener, r"length\s*>=\s*\(uint\)resolved\.Capacity", "final-path truncation rejection")
    require_re(opener, r"new\s+Qs3dHeldFile\s*\(\s*handle\s*,\s*resolved\.ToString\(\)\s*\)", "held handle/final-path ownership")
    require_re(opener, r"handle\s*=\s*null\s*;\s*return\s+held\s*;", "single ownership transfer")
    require_re(opener, r"finally\s*\{\s*if\s*\(\s*handle\s*!=\s*null\s*\)\s*handle\.Dispose\(\)\s*;\s*\}", "native handle cleanup")


def validate(source: str) -> None:
    validate_native(source)
    helper = function_body(source, "function Open-HeldVerifiedInstaller")
    if re.search(r"Get-Item\s+-LiteralPath\s+\$full\b", helper, re.IGNORECASE):
        fail("pathname-only Get-Item must not be authoritative before atomic open")
    require_re(helper, r"\$rootWithSeparator\s*=\s*\$root\s*\+\s*\[IO\.Path\]::DirectorySeparatorChar", "separator-bounded root")
    require_re(helper, r"\$full\.StartsWith\(\s*\$rootWithSeparator\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)", "root containment check")

    opened = require(helper, "[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)", "atomic held open")
    resolved = require(helper, "Convert-FromExtendedWin32Path -Path $held.FinalPath", "handle-resolved final path", opened)
    compared = require_re(helper, r"if\s*\(\s*-not\s+\[string\]::Equals\(\s*\$heldFinal\s*,\s*\$full\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)\s*\)\s*\{\s*throw\b", "fail-closed final-path identity check").start()
    signer = require(helper, "Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner", "held signer re-admission", resolved)
    if not (opened < resolved < compared < signer):
        fail("require atomic open < handle final-path identity < signer re-admission")
    require_re(helper, r"catch\s*\{[^{}]*if\s*\(\s*\$held\s*\)\s*\{\s*\$held\.Dispose\(\)\s*\}[^{}]*throw", "helper failure cleanup")

    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition")
    first_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package admission")
    if first_admission < acquire:
        fail("package admission occurs before installer hold")
    admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "held package admission", acquire)
    invoke = require(source, "& $installerScript @arguments", "in-memory installer invocation", admission)
    if re.search(r"\$heldInstaller\.Dispose\(\)", source[acquire:invoke], re.IGNORECASE):
        fail("held installer is disposed before in-memory invocation")
    dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal", invoke)
    if not (acquire < admission < invoke < dispose):
        fail("require acquire < final package admission < in-memory invoke < dispose")
    if source[acquire:dispose].count("& $installerScript @arguments") != 1:
        fail("held installer ScriptBlock must be invoked exactly once while held")
    if source[acquire:dispose].count("Assert-PackageRoot -Directory $extractRoot") != 1:
        fail("package admission must occur exactly once while held")
    if re.search(r"(?m)^\s*\$installer\s*=", source[acquire:dispose]):
        fail("installer path is reassigned after hold acquisition")
    if re.search(r"(?mi)^\s*finally\s*\{", source[invoke:dispose]) is None:
        fail("held installer disposal is not in a real finally block")

    for token, label in (
        ("Expand-VerifiedHeldArchive", "bounded ZIP/hash admission"),
        ("Compare-StrictSemVer", "product-version anti-downgrade"),
        ("Enter-Qs3dUpdateMutex", "update serialization mutex"),
        ("Get-Process -Name bricscad", "running-BricsCAD guard"),
        ("Read-InstalledProductVersion", "installed-state freshness check"),
        ("RequireSigned = $true", "signed child enforcement"),
        ("Remove-Item -LiteralPath $tempRoot -Recurse -Force", "temporary-root cleanup"),
    ):
        require(source, token, label)


def expect_reject(source: str, label: str) -> None:
    try:
        validate(source)
    except SystemExit:
        return
    fail(f"self-test accepted unsafe mutant: {label}")


def fixture() -> str:
    return r'''
public sealed class Qs3dHeldFile { public void Dispose() {} }
public static class Qs3dNativeFile {
private const uint GENERIC_READ = 0x80000000;
private const uint FILE_SHARE_READ = 0x00000001;
private const uint OPEN_EXISTING = 3;
private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
private static extern SafeFileHandle CreateFileW(string p,uint a,uint s,IntPtr x,uint d,uint f,IntPtr t);
private static extern bool GetFileInformationByHandle(SafeFileHandle h,out BY_HANDLE_FILE_INFORMATION information);
private static extern uint GetFinalPathNameByHandleW(SafeFileHandle h,StringBuilder b,uint n,uint f);
public static Qs3dHeldFile OpenOrdinaryReadHeld(string path) {
 SafeFileHandle handle = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
 if (handle == null || handle.IsInvalid) { throw new Exception(); }
 try {
  BY_HANDLE_FILE_INFORMATION information;
  if (!GetFileInformationByHandle(handle, out information)) { throw new Exception(); }
  if ((information.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0) { throw new Exception(); }
  if ((information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0) { throw new Exception(); }
  StringBuilder resolved = new StringBuilder(100);
  uint length = GetFinalPathNameByHandleW(handle, resolved, (uint)resolved.Capacity, 0);
  if (length == 0) { throw new Exception(); }
  if (length >= (uint)resolved.Capacity) { throw new Exception(); }
  Qs3dHeldFile held = new Qs3dHeldFile(handle, resolved.ToString());
  handle = null; return held;
 }
 finally { if (handle != null) handle.Dispose(); }
}
}
'@
function Assert-AuthenticodeSigner { }
function Expand-VerifiedHeldArchive { }
function Assert-PackageRoot { }
function Compare-StrictSemVer { }
function Enter-Qs3dUpdateMutex { }
function Read-InstalledProductVersion { }
function Open-HeldVerifiedInstaller {
 param($Path,$ExtractionRoot,$ExpectedSigner)
 $full=[IO.Path]::GetFullPath($Path)
 $root=[IO.Path]::GetFullPath($ExtractionRoot)
 $rootWithSeparator=$root + [IO.Path]::DirectorySeparatorChar
 if (-not $full.StartsWith($rootWithSeparator,[StringComparison]::OrdinalIgnoreCase)) { throw 'outside root' }
 $held=$null
 try {
  $held=[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)
  $heldFinal=[IO.Path]::GetFullPath((Convert-FromExtendedWin32Path -Path $held.FinalPath))
  if (-not [string]::Equals($heldFinal,$full,[StringComparison]::OrdinalIgnoreCase)) { throw 'mismatch' }
  Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
  return $held
 }
 catch { if ($held) { $held.Dispose() }; throw }
}
Get-Process -Name bricscad
RequireSigned = $true
Remove-Item -LiteralPath $tempRoot -Recurse -Force
$installer=Join-Path $extractRoot 'install-v25-autoload.ps1'
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
try {
 Assert-PackageRoot -Directory $extractRoot
 & $installerScript @arguments
}
finally { $heldInstaller.Dispose() }
'''


def self_test() -> None:
    good = fixture()
    validate(good)
    expect_reject(good.replace("FILE_SHARE_READ = 0x00000001", "FILE_SHARE_READ = 0x00000003", 1), "write sharing")
    expect_reject(good.replace("FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000", "FILE_FLAG_OPEN_REPARSE_POINT = 0x08000000", 1), "wrong no-follow flag")
    expect_reject(good.replace("GetFileInformationByHandle(handle, out information)", "GetFileInformationByHandle(other, out information)", 1), "attributes from another handle")
    expect_reject(good.replace("if ((information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0) { throw new Exception(); }", "if ((information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0) { /* throw new Exception(); */ return null; }", 1), "comment-spoofed reparse rejection")
    expect_reject(good.replace("GetFinalPathNameByHandleW(handle,", "GetFinalPathNameByHandleW(other,", 1), "final path from another handle")
    expect_reject(good.replace("if (length >= (uint)resolved.Capacity)", "if (length >= resolved.Capacity)", 1), "signed/unsigned truncation comparison")
    expect_reject(good.replace("$held.FinalPath", "$full", 1), "path identity not derived from held handle")
    expect_reject(good.replace("Assert-AuthenticodeSigner -Path $full", "Assert-AuthenticodeSigner -Path $Path", 1), "signer on unbound path")
    expect_reject(good.replace("catch { if ($held) { $held.Dispose() }; throw }", "catch { throw }", 1), "leaked hold on signer failure")
    expect_reject(good.replace(" Assert-PackageRoot -Directory $extractRoot\n & $installerScript", " $heldInstaller.Dispose()\n Assert-PackageRoot -Directory $extractRoot\n & $installerScript", 1), "early hold disposal")


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print("PASS: V25 updater atomically no-follow opens the installer, binds attributes/final path to that handle, re-admits signer, holds through final admission/in-memory execution, and disposes safely")
