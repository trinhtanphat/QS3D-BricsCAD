param(
    [Parameter(Mandatory=$true)][string]$AllocationRoot,
    [Parameter(Mandatory=$true)][string]$ExpectedAllocationSha256,
    [Parameter(Mandatory=$true)][string]$RunId,
    [Parameter(Mandatory=$true)][string]$DumpPath,
    [Parameter(Mandatory=$true)][string]$ExpectedDumpSha256,
    [Parameter(Mandatory=$true)][string]$LogPath,
    [Parameter(Mandatory=$true)][string]$DebuggerDirectory,
    [Parameter(Mandatory=$true)][string]$SymbolDirectory,
    [ValidateCount(1,8)][ValidateRange(0,4096)][int[]]$ThreadIndices=@(0)
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'diagnostic-guards.ps1')
Assert-Local022DiagnosticWindows
$context=Read-Local022DiagnosticAllocation $AllocationRoot $ExpectedAllocationSha256 $RunId
$dump=Resolve-Local022DiagnosticFile $context $DumpPath '.private.dmp'
Assert-Local022DiagnosticHex $ExpectedDumpSha256 64
if((Get-Local022DiagnosticHash $dump) -ine $ExpectedDumpSha256) { throw 'diagnostic_dump_hash' }
$log=Resolve-Local022DiagnosticFile $context $LogPath '.private.txt' -Fresh
$engineRoot=Assert-Local022DiagnosticPath $DebuggerDirectory
$symbols=Assert-Local022DiagnosticPath $SymbolDirectory
foreach($ansiPath in @($dump,$log,$symbols)) {
    if($ansiPath -cmatch '[^\x20-\x7e]') { throw 'diagnostic_ansi_path_required' }
}
if(-not (Test-Path -LiteralPath $engineRoot -PathType Container) -or
    -not (Test-Path -LiteralPath $symbols -PathType Container)) { throw 'diagnostic_directory_required' }
# Exact previously verified Microsoft x64 debugger bundle. No acquisition,
# symbol-server configuration or arbitrary debugger command is performed here.
$pins=[ordered]@{
    'dbgcore.dll'='de03ffb6b1181434c3db61e7ac63e3383b5732ac8a7d28579a30ff306ef8b8c4'
    'dbgeng.dll'='5a2a515f6263c66b0323d8707abf4b64511a9a34f1f1d3fb1f3c4c17f0bf9cad'
    'dbghelp.dll'='ed7323e007e863dd71e7b405e587969f37e5a146bb0fbcbf885031176bce401d'
    'dbgmodel.dll'='313c4d4b0f0fe4a23f0a8979c83ff40e095828adb4908e1c8869991aefbe6ac9'
    'msdia140.dll'='3499cf0bf1ee71ee04c197c9376fa8438964b557fc172a861572cae21abacd36'
    'symsrv.dll'='26cc026688a94533b780f0508db23a299198577c94ee4601ad157000278ebc2e'
}
foreach($name in $pins.Keys) {
    $file=Assert-Local022DiagnosticPath (Join-Path $engineRoot $name)
    $signature=Get-AuthenticodeSignature -LiteralPath $file
    Assert-Local022DiagnosticBinary (Get-Local022DiagnosticHash $file) $pins[$name] ($signature.Status.ToString()) ([string]$signature.SignerCertificate.Subject)
}
$engine=Join-Path $engineRoot 'dbgeng.dll'
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class Local022BoundedDumpReader {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string file,IntPtr reserved,uint flags);
 [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi,SetLastError=true)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int Create(ref Guid iid,out IntPtr client);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int OpenDump(IntPtr self,[MarshalAs(UnmanagedType.LPStr)] string file);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int OpenLog(IntPtr self,[MarshalAs(UnmanagedType.LPStr)] string file,int append);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int NoArgs(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int Wait(IntPtr self,uint flags,uint milliseconds);
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int Execute(IntPtr self,uint output,[MarshalAs(UnmanagedType.LPStr)] string command,uint flags);
 static T Method<T>(IntPtr self,int index) where T:Delegate { return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(self),index*IntPtr.Size)); }
 static void Check(int hr) { if(hr<0) Marshal.ThrowExceptionForHR(hr); }
 public static void Read(string engine,string dump,string log,string symbols,int[] threads) {
  // Search dependencies only beside the pinned engine and in System32.
  var module=LoadLibraryEx(engine,IntPtr.Zero,0x900);
  if(module==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
  IntPtr client=IntPtr.Zero,control=IntPtr.Zero; bool logging=false;
  try {
   var export=GetProcAddress(module,"DebugCreate");
   if(export==IntPtr.Zero) throw new InvalidOperationException("diagnostic_debug_create_missing");
   var create=Marshal.GetDelegateForFunctionPointer<Create>(export);
   Guid clientIid=new Guid("27fe5639-8407-4f47-8364-ee118fb08ac8"),controlIid=new Guid("5182e668-105e-416e-ad92-24ef800424ba");
   Check(create(ref clientIid,out client)); Check(Marshal.QueryInterface(client,in controlIid,out control));
   Check(Method<OpenLog>(control,8)(control,log,0)); logging=true;
   var execute=Method<Execute>(control,66);
   // The path validator excludes quotes, separators and debugger path syntax.
   Check(execute(control,0,".sympath \""+symbols+"\"",0));
   Check(Method<OpenDump>(client,19)(client,dump));
   Check(Method<Wait>(control,93)(control,0,30000));
   foreach(var index in threads) {
    if(index<0 || index>4096) throw new InvalidOperationException("diagnostic_thread_index");
    Check(execute(control,0,"~"+index.ToString(System.Globalization.CultureInfo.InvariantCulture)+" k 40",0));
   }
   Check(execute(control,0,"lm",0));
  } finally {
   if(logging) Method<NoArgs>(control,9)(control);
   if(control!=IntPtr.Zero) Marshal.Release(control);
   if(client!=IntPtr.Zero) Marshal.Release(client);
   FreeLibrary(module);
  }
 }
}
'@
# The inherited symbol environment must not opt into a network symbol server.
$oldSymbolPath=[Environment]::GetEnvironmentVariable('_NT_SYMBOL_PATH','Process')
$oldAltSymbolPath=[Environment]::GetEnvironmentVariable('_NT_ALT_SYMBOL_PATH','Process')
try {
    [Environment]::SetEnvironmentVariable('_NT_SYMBOL_PATH',$symbols,'Process')
    [Environment]::SetEnvironmentVariable('_NT_ALT_SYMBOL_PATH',$null,'Process')
    [void](Resolve-Local022DiagnosticFile $context $log '.private.txt' -Fresh)
    [Local022BoundedDumpReader]::Read($engine,$dump,$log,$symbols,$ThreadIndices)
} finally {
    [Environment]::SetEnvironmentVariable('_NT_SYMBOL_PATH',$oldSymbolPath,'Process')
    [Environment]::SetEnvironmentVariable('_NT_ALT_SYMBOL_PATH',$oldAltSymbolPath,'Process')
}
[pscustomobject]@{status='PRIVATE_OFFLINE_DIAGNOSTIC_ONLY';run_id=$RunId;sha256=(Get-Local022DiagnosticHash $log);bytes=(Get-Item -LiteralPath $log).Length}
