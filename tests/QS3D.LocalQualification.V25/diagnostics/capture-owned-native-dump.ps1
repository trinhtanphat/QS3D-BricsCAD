param(
    [Parameter(Mandatory=$true)][string]$AllocationRoot,
    [Parameter(Mandatory=$true)][string]$ExpectedAllocationSha256,
    [Parameter(Mandatory=$true)][string]$RunId,
    [Parameter(Mandatory=$true)][int]$OwnedProcessId,
    [Parameter(Mandatory=$true)][int]$OwnedParentId,
    [Parameter(Mandatory=$true)][string]$ExpectedProcessStartUtc,
    [Parameter(Mandatory=$true)][string]$ExpectedExecutable,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [switch]$ConfirmPrivateMemoryDump
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'diagnostic-guards.ps1')
if(-not $ConfirmPrivateMemoryDump) { throw 'diagnostic_dump_confirmation_required' }
Assert-Local022DiagnosticWindows
$context=Read-Local022DiagnosticAllocation $AllocationRoot $ExpectedAllocationSha256 $RunId
$binding=Read-Local022DiagnosticBinding $context
$dump=Resolve-Local022DiagnosticFile $context $OutputPath '.private.dmp' -Fresh
[void](Get-Local022DiagnosticProcess $binding $OwnedProcessId $OwnedParentId $ExpectedProcessStartUtc $ExpectedExecutable)
# Captures private memory. It can briefly pause the test host. Never upload raw
# dumps; explicit consent and exact process creation time are required each time.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class Local022BoundedNativeDump {
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetProcessTimes(IntPtr process,out long created,out long exited,out long kernel,out long user);
 [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
 [DllImport("dbghelp.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
 static extern bool MiniDumpWriteDump(IntPtr process,uint pid,IntPtr file,uint type,IntPtr exception,IntPtr user,IntPtr callback);
 static void VerifyCreationTime(long created,long expectedCreationFileTime) {
  // Win32_Process.CreationDate truncates native 100ns FILETIME to microseconds.
  // Keep that exact microsecond; no wider time tolerance or PID-only fallback.
  if(created/10!=expectedCreationFileTime/10) throw new InvalidOperationException("diagnostic_process_reused");
 }
 public static void Capture(uint pid,long expectedCreationFileTime,string path) {
  var process=OpenProcess(0x450,false,pid);
  if(process==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
  try {
   long created,exited,kernel,user;
   if(!GetProcessTimes(process,out created,out exited,out kernel,out user)) throw new Win32Exception(Marshal.GetLastWin32Error());
   VerifyCreationTime(created,expectedCreationFileTime);
   using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None)) {
    if(!MiniDumpWriteDump(process,pid,file.SafeFileHandle.DangerousGetHandle(),0x1124,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero)) throw new Win32Exception(Marshal.GetLastWin32Error());
   }
  } finally { CloseHandle(process); }
 }
}
'@
[void](Read-Local022DiagnosticBinding $context)
[void](Get-Local022DiagnosticProcess $binding $OwnedProcessId $OwnedParentId $ExpectedProcessStartUtc $ExpectedExecutable)
[void](Resolve-Local022DiagnosticFile $context $dump '.private.dmp' -Fresh)
[Local022BoundedNativeDump]::Capture([uint32]$OwnedProcessId,(ConvertTo-Local022DiagnosticUtc $ExpectedProcessStartUtc).ToFileTimeUtc(),$dump)
[pscustomobject]@{status='PRIVATE_DIAGNOSTIC_ONLY';run_id=$RunId;sha256=(Get-Local022DiagnosticHash $dump);bytes=(Get-Item -LiteralPath $dump).Length}
