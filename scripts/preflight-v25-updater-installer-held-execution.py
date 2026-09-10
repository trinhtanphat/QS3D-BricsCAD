#!/usr/bin/env python3
"""Fail closed unless the V25 updater pins the admitted installer across execution."""

from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def require(source: str, token: str, label: str, start: int = 0) -> int:
    index = source.find(token, start)
    if index < 0:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: missing {label}: {token}")
    return index


def function_body(source: str, name: str) -> str:
    marker = f"function {name}"
    start = require(source, marker, f"{name} helper")
    brace = source.find("{", start)
    if brace < 0:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: {name} has no body")
    depth = 0
    quote = None
    escaped = False
    for index in range(brace, len(source)):
        char = source[index]
        if escaped:
            escaped = False
            continue
        if char == "`":
            escaped = True
            continue
        if quote:
            if char == quote:
                quote = None
            continue
        if char in ("'", '"'):
            quote = char
            continue
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise SystemExit(f"ERROR: V25 updater installer-hold preflight: {name} body is unbalanced")


def require_regex(source: str, pattern: str, label: str, flags: int = re.IGNORECASE | re.DOTALL) -> re.Match[str]:
    match = re.search(pattern, source, flags)
    if match is None:
        raise SystemExit(f"ERROR: V25 updater installer-hold preflight: missing {label}")
    return match


def native_block(source: str) -> str:
    start = require(source, "public static class Qs3dNativeFile", "native held-file helper")
    end = source.find("'@", start)
    if end < 0:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: native held-file Add-Type block is unterminated")
    return source[start:end]


def validate_native(source: str) -> None:
    native = native_block(source)

    constants = {
        "GENERIC_READ": "0x80000000",
        "FILE_SHARE_READ": "0x00000001",
        "OPEN_EXISTING": "3",
        "FILE_FLAG_OPEN_REPARSE_POINT": "0x00200000",
        "FILE_ATTRIBUTE_DIRECTORY": "0x00000010",
        "FILE_ATTRIBUTE_REPARSE_POINT": "0x00000400",
    }
    for name, value in constants.items():
        require_regex(native, rf"private\s+const\s+uint\s+{name}\s*=\s*{re.escape(value)}\s*;", f"native {name}={value} constant")

    for declaration, label in (
        (r"private\s+static\s+extern\s+SafeFileHandle\s+CreateFileW\s*\(", "CreateFileW SafeFileHandle declaration"),
        (r"private\s+static\s+extern\s+bool\s+GetFileInformationByHandle\s*\(", "GetFileInformationByHandle declaration"),
        (r"private\s+static\s+extern\s+uint\s+GetFinalPathNameByHandleW\s*\(", "GetFinalPathNameByHandleW declaration"),
    ):
        require_regex(native, declaration, label)

    opener = require_regex(
        native,
        r"public\s+static\s+Qs3dHeldFile\s+OpenOrdinaryReadHeld\s*\(\s*string\s+path\s*\)\s*\{(?P<body>.*)\}\s*$",
        "OpenOrdinaryReadHeld implementation",
    ).group("body")
    require_regex(
        opener,
        r"SafeFileHandle\s+handle\s*=\s*CreateFileW\s*\(\s*path\s*,\s*GENERIC_READ\s*,\s*FILE_SHARE_READ\s*,\s*IntPtr\.Zero\s*,\s*OPEN_EXISTING\s*,\s*FILE_FLAG_OPEN_REPARSE_POINT\s*,\s*IntPtr\.Zero\s*\)\s*;",
        "atomic no-follow read-only CreateFileW call",
    )
    require_regex(opener, r"if\s*\(\s*handle\s*==\s*null\s*\|\|\s*handle\.IsInvalid\s*\)\s*\{(?:(?!\}).)*throw\b", "invalid native handle fail-closed check")
    require_regex(opener, r"if\s*\(\s*!GetFileInformationByHandle\s*\(\s*handle\s*,\s*out\s+information\s*\)\s*\)\s*\{(?:(?!\}).)*throw\b", "handle-bound attribute query failure check")
    require_regex(opener, r"if\s*\(\s*\(\s*information\.FileAttributes\s*&\s*FILE_ATTRIBUTE_DIRECTORY\s*\)\s*!=\s*0\s*\)\s*\{(?:(?!\}).)*throw\b", "handle-bound directory rejection")
    require_regex(opener, r"if\s*\(\s*\(\s*information\.FileAttributes\s*&\s*FILE_ATTRIBUTE_REPARSE_POINT\s*\)\s*!=\s*0\s*\)\s*\{(?:(?!\}).)*throw\b", "handle-bound reparse rejection")
    require_regex(opener, r"GetFinalPathNameByHandleW\s*\(\s*handle\s*,\s*resolved\s*,\s*\(uint\)resolved\.Capacity\s*,\s*0\s*\)", "handle-resolved final path query")
    require_regex(opener, r"if\s*\(\s*length\s*==\s*0\s*\)\s*\{(?:(?!\}).)*throw\b", "final-path API failure check")
    require_regex(opener, r"if\s*\(\s*length\s*>=\s*\(uint\)resolved\.Capacity\s*\)\s*\{(?:(?!\}).)*throw\b", "unsigned-safe final-path truncation rejection")
    require_regex(opener, r"new\s+Qs3dHeldFile\s*\(\s*handle\s*,\s*resolved\.ToString\(\)\s*\)", "held handle/final-path ownership object")
    require_regex(opener, r"handle\s*=\s*null\s*;\s*return\s+held\s*;", "single ownership transfer before return")
    require_regex(opener, r"finally\s*\{\s*if\s*\(\s*handle\s*!=\s*null\s*\)\s*handle\.Dispose\(\)\s*;\s*\}", "native handle cleanup on all pre-transfer failures")


def validate(source: str) -> None:
    validate_native(source)
    helper = function_body(source, "Open-HeldVerifiedInstaller")
    for token, label in (
        ("[IO.Path]::GetFullPath", "canonical path/root normalization"),
        ("[StringComparison]::OrdinalIgnoreCase", "Windows path comparison"),
        ("OpenOrdinaryReadHeld", "atomic held-file opener"),
        ("FinalPath", "handle-resolved final path binding"),
        ("Assert-AuthenticodeSigner", "held Authenticode re-admission"),
        ("ExpectedSigner", "expected signer binding"),
        ("ExtractionRoot", "extraction-root input"),
    ):
        if token not in helper:
            raise SystemExit(f"ERROR: V25 updater installer-hold preflight: helper missing {label}: {token}")

    if re.search(r"Get-Item\s+-LiteralPath\s+\$full\b", helper, re.IGNORECASE):
        raise SystemExit("ERROR: V25 updater installer-hold preflight: canonical installer must not be admitted by pathname-only Get-Item before atomic no-follow open")

    separator_binding = re.search(r"(?:\$rootWithSeparator\s*=\s*\$root\.TrimEnd\([^\n]+\)\s*\+\s*['\"]\\['\"]|\$rootWithSeparator\s*=\s*\$root\s*\+\s*\[IO\.Path\]::DirectorySeparatorChar\b)", helper, re.IGNORECASE)
    boundary_check = re.search(r"\$full\.StartsWith\(\s*\$rootWithSeparator\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)", helper, re.IGNORECASE)
    if separator_binding is None or boundary_check is None:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: lexical containment must use a separator-bounded extraction root")

    atomic_open = require(helper, "OpenOrdinaryReadHeld", "atomic no-follow held open")
    final_path = require(helper, "Convert-FromExtendedWin32Path -Path $held.FinalPath", "handle-resolved final path", atomic_open)
    signer = require(helper, "Assert-AuthenticodeSigner", "held signer re-admission", final_path)
    if not (atomic_open < final_path < signer):
        raise SystemExit("ERROR: V25 updater installer-hold preflight: require atomic open < handle final-path binding < signer re-admission")
    if re.search(r"\[string\]::Equals\(\s*\$heldFinal\s*,\s*\$full\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)", helper, re.IGNORECASE) is None:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: handle-resolved final path must equal canonical installer path")
    require_regex(helper, r"if\s*\(\s*-not\s+\[string\]::Equals\(\s*\$heldFinal\s*,\s*\$full\s*,\s*\[StringComparison\]::OrdinalIgnoreCase\s*\)\s*\)\s*\{\s*throw\b", "fail-closed handle-resolved path mismatch rejection")
    if re.search(r"Assert-AuthenticodeSigner\s+-Path\s+\$full\s+-ExpectedSigner\s+\$ExpectedSigner\b", helper, re.IGNORECASE) is None:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: signer re-admission must target canonical pinned path and expected signer")
    if "catch" not in helper or ".Dispose()" not in helper:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: helper must dispose the native hold if re-admission fails")

    acquire = require(source, "$heldInstaller = Open-HeldVerifiedInstaller", "held installer acquisition")
    first_package_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package-root admission")
    package_admission = require(source, "Assert-PackageRoot -Directory $extractRoot", "package-root admission", acquire)
    if first_package_admission < acquire:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: package admission must not occur before installer hold acquisition")
    invoke = require(source, "& $installer @arguments", "installer invocation", package_admission)
    dispose = require(source, "$heldInstaller.Dispose()", "held installer disposal", invoke)
    if not (acquire < package_admission < invoke < dispose):
        raise SystemExit("ERROR: V25 updater installer-hold preflight: require acquire < final package admission < invoke < dispose")
    after_invoke = source[invoke:dispose]
    if re.search(r"(?mi)^\s*finally\s*\{", after_invoke) is None:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: held installer disposal must be inside an actual finally block after invocation")
    held_interval = source[acquire:dispose]
    if held_interval.count("& $installer @arguments") != 1:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: installer must be invoked exactly once while pinned")
    if held_interval.count("Assert-PackageRoot -Directory $extractRoot") != 1:
        raise SystemExit("ERROR: V25 updater installer-hold preflight: final package admission must occur exactly once while pinned")
    if re.search(r"(?m)^\s*\$installer\s*=", held_interval):
        raise SystemExit("ERROR: V25 updater installer-hold preflight: installer pathname must not be reassigned after the hold is acquired")

    for token, label in (
        ("Expand-VerifiedHeldArchive", "bounded ZIP/hash admission"),
        ("Assert-PackageRoot", "package signature/integrity admission"),
        ("Compare-StrictSemVer", "product-version anti-downgrade"),
        ("Enter-Qs3dUpdateMutex", "update serialization mutex"),
        ("Get-Process -Name bricscad", "running-BricsCAD guard"),
        ("Read-InstalledProductVersion", "installed-state freshness check"),
        ("RequireSigned = $true", "installer signed-payload enforcement"),
        ("Remove-Item -LiteralPath $tempRoot -Recurse -Force", "temporary-root cleanup"),
    ):
        require(source, token, label)


def expect_reject(source: str, label: str) -> None:
    try:
        validate(source)
    except SystemExit:
        return
    raise SystemExit(f"ERROR: V25 updater installer-hold preflight self-test accepted {label}")


def native_fixture() -> str:
    return r'''
public static class Qs3dNativeFile
{
private const uint GENERIC_READ = 0x80000000;
private const uint FILE_SHARE_READ = 0x00000001;
private const uint OPEN_EXISTING = 3;
private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
private static extern SafeFileHandle CreateFileW(string fileName,uint desiredAccess,uint shareMode,IntPtr sa,uint disposition,uint flags,IntPtr templateFile);
private static extern bool GetFileInformationByHandle(SafeFileHandle file,out BY_HANDLE_FILE_INFORMATION information);
private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file,StringBuilder path,uint pathLength,uint flags);
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
  handle = null;
  return held;
 }
 finally { if (handle != null) handle.Dispose(); }
}
}
'@
'''


def common_fences() -> str:
    return native_fixture() + """
function Assert-AuthenticodeSigner { }
function Expand-VerifiedHeldArchive { }
function Assert-PackageRoot { }
function Compare-StrictSemVer { }
function Enter-Qs3dUpdateMutex { }
function Read-InstalledProductVersion { }
Get-Process -Name bricscad
RequireSigned = $true
Remove-Item -LiteralPath $tempRoot -Recurse -Force
"""


def strong_helper() -> str:
    return """
function Open-HeldVerifiedInstaller {
param($Path,$ExtractionRoot,$ExpectedSigner)
$full=[IO.Path]::GetFullPath($Path)
$root=[IO.Path]::GetFullPath($ExtractionRoot)
$rootWithSeparator=$root.TrimEnd('\\') + '\\'
if (-not $full.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) { throw 'outside root' }
$held=$null
try {
  $held=[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)
  $heldFinal=[IO.Path]::GetFullPath((Convert-FromExtendedWin32Path -Path $held.FinalPath))
  if (-not [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)) { throw 'resolved path mismatch' }
  Assert-AuthenticodeSigner -Path $full -ExpectedSigner $ExpectedSigner -Label installer
  return $held
}
catch { if ($held) { $held.Dispose() }; throw }
}
"""


def valid_topology() -> str:
    return common_fences() + strong_helper() + """
$installer = Join-Path $extractRoot 'install-v25-autoload.ps1'
$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner
try {
  Assert-PackageRoot -Directory $extractRoot
  & $installer @arguments
}
finally {
  $heldInstaller.Dispose()
}
"""


def self_test() -> None:
    valid = valid_topology()
    validate(valid)
    expect_reject(valid.replace("FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000", "FILE_FLAG_OPEN_REPARSE_POINT = 0x08000000", 1), "wrong no-follow flag value")
    expect_reject(valid.replace("FILE_SHARE_READ = 0x00000001", "FILE_SHARE_READ = 0x00000003", 1), "write-share native constant")
    expect_reject(valid.replace("CreateFileW(path, GENERIC_READ, FILE_SHARE_READ", "CreateFileW(path, GENERIC_READ, FILE_SHARE_READ | 2", 1), "permissive native open call")
    expect_reject(valid.replace("GetFileInformationByHandle(handle, out information)", "GetFileInformationByHandle(other, out information)", 1), "attribute query on different handle")
    expect_reject(valid.replace("(information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0) { throw", "(information.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0) { var ignored=1; // throw", 1), "non-failing reparse inspection")
    expect_reject(valid.replace("GetFinalPathNameByHandleW(handle,", "GetFinalPathNameByHandleW(other,", 1), "final path from different handle")
    expect_reject(valid.replace("if (length >= (uint)resolved.Capacity)", "if (length >= resolved.Capacity)", 1), "signed/unsigned-incompatible buffer comparison")
    expect_reject(valid.replace("$held.FinalPath", "$full", 1), "final-path check not bound to opened handle")
    expect_reject(valid.replace("if (-not [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)) { throw 'resolved path mismatch' }", "$same = [string]::Equals($heldFinal, $full, [StringComparison]::OrdinalIgnoreCase)", 1), "resolved-path comparison without fail-closed rejection")
    expect_reject(valid.replace("$held=[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)", "$item=Get-Item -LiteralPath $full\n  $held=[Qs3dNativeFile]::OpenOrdinaryReadHeld($full)", 1), "pathname-only ordinary-file check before atomic open")
    expect_reject(valid.replace("$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot", "Assert-PackageRoot -Directory $extractRoot\n$heldInstaller = Open-HeldVerifiedInstaller -Path $installer -ExtractionRoot $extractRoot -ExpectedSigner $expectedSigner\ntry {\n  Assert-PackageRoot", 1), "package admission before hold acquisition")
    expect_reject(valid.replace("$full.StartsWith($rootWithSeparator,", "$full.StartsWith($root,", 1), "unsafe prefix containment")
    expect_reject(valid.replace("Assert-AuthenticodeSigner -Path $full", "Assert-AuthenticodeSigner -Path $Path", 1), "signer check on unbound path")
    expect_reject(valid.replace("catch { if ($held) { $held.Dispose() }; throw }", "catch { throw }", 1), "hold leak on re-admission failure")
    expect_reject(valid.replace("  & $installer @arguments\n", "  $installer = Join-Path $extractRoot 'install-v25-autoload.ps1'\n  & $installer @arguments\n", 1), "installer pathname reassignment after hold acquisition")
    expect_reject(valid.replace("  & $installer @arguments\n}\nfinally {\n  $heldInstaller.Dispose()", "  $heldInstaller.Dispose()\n  & $installer @arguments\n}\nfinally {\n  Write-Host done", 1), "hold disposed before invocation")
    expect_reject(valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  # finally {\n  $heldInstaller.Dispose()", 1), "comment-only finally")
    expect_reject(valid.replace("}\nfinally {\n  $heldInstaller.Dispose()", "  Write-Host 'finally {'\n  $heldInstaller.Dispose()", 1), "string-only finally")


if __name__ == "__main__":
    self_test()
    validate(UPDATER.read_text(encoding="utf-8"))
    print("PASS: V25 updater atomically no-follow opens the installer, validates attributes/final path from the exact handle, re-admits signer, pins it across final package admission/execution, and disposes safely")
