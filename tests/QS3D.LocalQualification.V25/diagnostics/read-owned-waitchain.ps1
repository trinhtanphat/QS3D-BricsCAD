param(
    [Parameter(Mandatory=$true)][string]$AllocationRoot,
    [Parameter(Mandatory=$true)][string]$ExpectedAllocationSha256,
    [Parameter(Mandatory=$true)][string]$RunId,
    [Parameter(Mandatory=$true)][int]$OwnedProcessId,
    [Parameter(Mandatory=$true)][int]$OwnedParentId,
    [Parameter(Mandatory=$true)][string]$ExpectedProcessStartUtc,
    [Parameter(Mandatory=$true)][string]$ExpectedExecutable
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'diagnostic-guards.ps1')
Assert-Local022DiagnosticWindows
$context=Read-Local022DiagnosticAllocation $AllocationRoot $ExpectedAllocationSha256 $RunId
$binding=Read-Local022DiagnosticBinding $context
[void](Get-Local022DiagnosticProcess $binding $OwnedProcessId $OwnedParentId $ExpectedProcessStartUtc $ExpectedExecutable)
# WCT reads only. It neither injects input nor attaches a debugger/changes privileges.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class Local022BoundedWaitChain {
 const uint WctNetworkIoFlag=0x8; // SDK wct.h: network I/O; no out-of-process COM/CS expansion.
 [StructLayout(LayoutKind.Explicit,Size=280)] public struct Node {
  [FieldOffset(0)] public int Kind; [FieldOffset(4)] public int Status;
  [FieldOffset(8)] public uint ProcessId; [FieldOffset(12)] public uint ThreadId;
 }
 [DllImport("advapi32.dll",SetLastError=true)] static extern IntPtr OpenThreadWaitChainSession(uint flags,IntPtr callback);
 [DllImport("advapi32.dll")] static extern void CloseThreadWaitChainSession(IntPtr session);
 [DllImport("advapi32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
 static extern bool GetThreadWaitChain(IntPtr session,UIntPtr context,uint flags,uint threadId,ref uint count,[Out] Node[] nodes,[MarshalAs(UnmanagedType.Bool)] out bool cycle);
 public static string Read(uint threadId,uint expectedPid) {
  var session=OpenThreadWaitChainSession(0,IntPtr.Zero);
  if(session==IntPtr.Zero) return "wct_open_error="+Marshal.GetLastWin32Error();
  try {
   uint count=16; bool cycle; var nodes=new Node[16];
   if(!GetThreadWaitChain(session,UIntPtr.Zero,WctNetworkIoFlag,threadId,ref count,nodes,out cycle)) return "wct_error="+Marshal.GetLastWin32Error();
   if(count==0 || nodes[0].Kind!=8 || nodes[0].ProcessId!=expectedPid || nodes[0].ThreadId!=threadId) throw new InvalidOperationException("diagnostic_thread_reused");
   var result=new List<string>{"thread="+threadId+" cycle="+cycle+" nodes="+count};
   for(int i=0;i<Math.Min(count,16);i++) {
    var n=nodes[i]; result.Add("kind="+n.Kind+" status="+n.Status+(n.Kind==8?" pid="+n.ProcessId+" tid="+n.ThreadId:""));
   }
   return String.Join(" | ",result);
  } finally { CloseThreadWaitChainSession(session); }
 }
}
'@
$threadIds=@((Get-Process -Id $OwnedProcessId).Threads | Sort-Object TotalProcessorTime -Descending | Select-Object -First 8 -ExpandProperty Id)
foreach($threadId in $threadIds) {
    [void](Read-Local022DiagnosticBinding $context)
    [void](Get-Local022DiagnosticProcess $binding $OwnedProcessId $OwnedParentId $ExpectedProcessStartUtc $ExpectedExecutable)
    [Local022BoundedWaitChain]::Read([uint32]$threadId,[uint32]$OwnedProcessId)
}
Write-Output 'DIAGNOSTIC_ONLY: at most eight threads sampled; no global deadlock or runtime PASS verdict.'
