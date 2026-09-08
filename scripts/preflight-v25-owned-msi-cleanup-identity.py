#!/usr/bin/env python3
"""Fail closed if V25 failed-publication cleanup can delete a replacement pathname."""

from __future__ import annotations

import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
NATIVE_CREATE_DECL = "public static extern SafeFileHandle CreateFileW("
NATIVE_CREATE_CALL = "$handle = [QS3DV25NativeFileDisposition]::CreateFileW("


def validate(source: str) -> list[str]:
    failures: list[str] = []

    start_marker = "$publishedByThisAttempt = $false"
    end_marker = 'Write-Warning "BricsCAD V25 installer source failed:'
    if start_marker not in source:
        return ["V25 publisher ownership state is missing"]
    start = source.index(start_marker)
    end = source.find(end_marker, start)
    if end <= start:
        return ["could not isolate failed owned-publication cleanup window"]
    window = source[start:end]

    open_owned = "$publishedStream = Open-OwnedMsiPublication -Path $msi"
    explicit_arm = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $true"
    native_clear = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false"
    same_handle_rehash = "$publishedStream.Position = 0"
    same_handle_hash = "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)"
    pathname_delete = "[IO.File]::Delete($msi)"
    reopened_cleanup = "Get-OrdinaryFileOrNull -Path $msi -Label 'Failed owned canonical MSI publication'"
    create_time_delete = "[IO.FileOptions]::DeleteOnClose"
    delete_access = "public const uint DELETE = 0x00010000;"
    desired_delete = "[QS3DV25NativeFileDisposition]::DELETE"

    required_tokens = (
        (NATIVE_CREATE_DECL, "native CreateFileW declaration is missing"),
        (NATIVE_CREATE_CALL, "owned native CreateFileW call site is missing"),
        (delete_access, "native DELETE access constant is missing"),
        (desired_delete, "creator handle does not request DELETE access"),
        ("public const uint CREATE_NEW = 1;", "fresh-only CREATE_NEW constant is missing"),
        ("[QS3DV25NativeFileDisposition]::CREATE_NEW", "creator does not use CREATE_NEW"),
        ("SetFileInformationByHandle", "native handle disposition primitive is missing"),
        ("FileDispositionInfo", "file disposition information class is missing"),
        (open_owned, "canonical MSI is not created through the owned native handle helper"),
        (explicit_arm, "owned canonical MSI is not explicitly armed for deletion"),
        (same_handle_rehash, "owned publication handle is not rewound for same-handle verification"),
        (same_handle_hash, "owned publication bytes are not rehashed through the creator handle"),
        (native_clear, "successful publication does not explicitly clear delete disposition"),
    )
    for token, message in required_tokens:
        if token not in source:
            failures.append(message)

    native_decl = source.find(NATIVE_CREATE_DECL)
    helper = source.find("function Open-OwnedMsiPublication")
    native_call = source.find(NATIVE_CREATE_CALL, helper if helper >= 0 else 0)
    if not (0 <= native_decl < helper < native_call):
        failures.append("native CreateFileW declaration must back the exact owned-publication helper call")

    if create_time_delete in source:
        failures.append(
            "FileOptions.DeleteOnClose/FILE_FLAG_DELETE_ON_CLOSE is not cancelable with FileDispositionInfo=false"
        )
    if open_owned not in window:
        failures.append("owned native creator handle is not established in the publication window")
    if explicit_arm not in window:
        failures.append("explicit cancelable delete disposition is not armed in the publication window")
    if same_handle_rehash not in window:
        failures.append("same-handle verification is not in the owned publication window")
    if same_handle_hash not in window:
        failures.append("same-handle hash verification is not in the owned publication window")
    if native_clear not in window:
        failures.append("publication commit is not in the owned publication window")
    if pathname_delete in window:
        failures.append("failed owned-publication cleanup must not delete the canonical pathname")
    if reopened_cleanup in window:
        failures.append("failed owned-publication cleanup must not reopen the canonical pathname")

    ordered = (open_owned, explicit_arm, same_handle_rehash, same_handle_hash, native_clear)
    if any(token not in window for token in ordered):
        return failures

    create_pos = window.index(open_owned)
    arm_pos = window.index(explicit_arm, create_pos)
    rehash_pos = window.index(same_handle_rehash, arm_pos)
    hash_pos = window.index(same_handle_hash, rehash_pos)
    clear_pos = window.index(native_clear, hash_pos)
    dispose_pos = window.find("$publishedStream.Dispose()", clear_pos)
    if dispose_pos < 0:
        failures.append("owned publication stream disposal after commit is missing")
    elif not create_pos < arm_pos < rehash_pos < hash_pos < clear_pos < dispose_pos:
        failures.append(
            "explicit delete disposition must remain armed through same-handle verification and clear only at commit"
        )

    ownership_clear = "$publishedByThisAttempt = $false"
    ownership_clear_pos = window.find(ownership_clear, clear_pos)
    if ownership_clear_pos <= clear_pos:
        failures.append("publication ownership must not clear before delete disposition is successfully committed")

    catch_pos = window.find("catch {")
    if catch_pos < 0:
        failures.append("publication catch block is missing")
    elif pathname_delete in window[catch_pos:]:
        failures.append("catch cleanup must remain handle-only")

    return failures


def windows_disposition_probe() -> None:
    if os.name != "nt":
        return

    with tempfile.TemporaryDirectory(prefix="qs3d-v25-disposition-") as directory:
        commit_path = str(Path(directory) / "commit.tmp")
        rollback_path = str(Path(directory) / "rollback.tmp")
        script = r'''
param([string]$CommitPath, [string]$RollbackPath)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class Qs3dDispositionProbe {
  [StructLayout(LayoutKind.Sequential)] public struct FILE_DISPOSITION_INFO { [MarshalAs(UnmanagedType.Bool)] public bool DeleteFile; }
  public const int FileDispositionInfo = 4;
  public const uint GENERIC_READ = 0x80000000;
  public const uint GENERIC_WRITE = 0x40000000;
  public const uint DELETE = 0x00010000;
  public const uint CREATE_NEW = 1;
  public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
  [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
  public static extern SafeFileHandle CreateFileW(string p, uint a, uint s, IntPtr sa, uint c, uint f, IntPtr t);
  [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool SetFileInformationByHandle(SafeFileHandle h, int i, ref FILE_DISPOSITION_INFO d, uint z);
}
"@
function Open-Probe([string]$Path) {
  $access = [Qs3dDispositionProbe]::GENERIC_READ -bor [Qs3dDispositionProbe]::GENERIC_WRITE -bor [Qs3dDispositionProbe]::DELETE
  $h = [Qs3dDispositionProbe]::CreateFileW($Path,[uint32]$access,0,[IntPtr]::Zero,[Qs3dDispositionProbe]::CREATE_NEW,[Qs3dDispositionProbe]::FILE_ATTRIBUTE_NORMAL,[IntPtr]::Zero)
  if ($h.IsInvalid) { throw "CreateFileW failed: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())" }
  return [IO.FileStream]::new($h,[IO.FileAccess]::ReadWrite,4096,$false)
}
function Set-Probe([IO.FileStream]$Stream,[bool]$Delete) {
  $d = New-Object 'Qs3dDispositionProbe+FILE_DISPOSITION_INFO'; $d.DeleteFile=$Delete
  $z=[Runtime.InteropServices.Marshal]::SizeOf($d)
  if (-not [Qs3dDispositionProbe]::SetFileInformationByHandle($Stream.SafeFileHandle,[Qs3dDispositionProbe]::FileDispositionInfo,[ref]$d,[uint32]$z)) {
    throw "SetFileInformationByHandle failed: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
  }
}
$c=Open-Probe $CommitPath
Set-Probe $c $true
$c.WriteByte(0x51); $c.Flush($true)
Set-Probe $c $false
$c.Dispose()
if (-not (Test-Path -LiteralPath $CommitPath -PathType Leaf)) { throw 'explicit disposition clear did not preserve committed file' }
$r=Open-Probe $RollbackPath
Set-Probe $r $true
$r.WriteByte(0x52); $r.Flush($true)
$r.Dispose()
if (Test-Path -LiteralPath $RollbackPath) { throw 'armed disposition did not remove failed generation on close' }
Remove-Item -LiteralPath $CommitPath -Force
'''
        completed = subprocess.run(
            ["powershell.exe", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, commit_path, rollback_path],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=60,
            check=False,
        )
        if completed.returncode != 0:
            raise SystemExit(
                "Windows explicit-disposition runtime probe failed:\n" + completed.stdout.strip()
            )


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)
    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    mutation_tokens = (
        (NATIVE_CREATE_DECL, "native creator declaration"),
        (NATIVE_CREATE_CALL, "native creator call site"),
        ("public const uint DELETE = 0x00010000;", "DELETE access constant"),
        ("[QS3DV25NativeFileDisposition]::DELETE", "DELETE access use"),
        ("public const uint CREATE_NEW = 1;", "fresh-only constant"),
        ("[QS3DV25NativeFileDisposition]::CREATE_NEW", "fresh-only use"),
        ("$publishedStream = Open-OwnedMsiPublication -Path $msi", "owned publication helper"),
        ("Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $true", "explicit delete arm"),
        ("$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)", "same-handle verification"),
        ("Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false", "explicit publication commit"),
        ("$publishedByThisAttempt = $false\n            $publishedStream.Dispose()", "ownership clear ordering"),
    )
    for token, label in mutation_tokens:
        if token not in source:
            print(f"FAIL: mutation fixture missing: {label}")
            return 1
        mutated = source.replace(token, "", 1)
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    for unsafe_token, label in (
        ("[IO.FileOptions]::DeleteOnClose", "uncancelable create-time delete-on-close"),
        ("[IO.File]::Delete($msi)", "pathname delete"),
        ("Get-OrdinaryFileOrNull -Path $msi -Label 'Failed owned canonical MSI publication'", "pathname reopen"),
    ):
        insertion = source.find('Write-Warning "BricsCAD V25 installer source failed:')
        if insertion < 0:
            print("FAIL: mutation insertion point is missing")
            return 1
        mutated = source[:insertion] + f"            {unsafe_token}\n" + source[insertion:]
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    windows_disposition_probe()
    print("PASS: V25 canonical MSI publication uses an explicitly armed, cancelable same-handle delete disposition")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())