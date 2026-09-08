$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$common = Join-Path $PSScriptRoot 'diagnostic-guards.ps1'
if (-not (Test-Path -LiteralPath $common)) { throw 'FAIL: diagnostic guards are missing.' }
. $common

# Compile the actual embedded C# without invoking WCT, capture or debugger APIs.
# This catches interop/compiler regressions that PowerShell parsing cannot see.
foreach($entry in @('read-owned-waitchain.ps1','capture-owned-native-dump.ps1','read-native-dump.ps1')) {
    $parseErrors=$null
    $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $entry),[ref]$null,[ref]$parseErrors)
    if($parseErrors.Count) { throw "FAIL: invalid diagnostic script $entry" }
    $sources=@($ast.FindAll({param($node)
        $node -is [Management.Automation.Language.StringConstantExpressionAst] -and
        $node.StringConstantType -eq [Management.Automation.Language.StringConstantType]::SingleQuotedHereString
    },$true))
    if($sources.Count -ne 1) { throw "FAIL: ambiguous diagnostic C# in $entry" }
    Microsoft.PowerShell.Utility\Add-Type -TypeDefinition $sources[0].Value
}
$dumpMethod=[Local022BoundedNativeDump].GetMethod('MiniDumpWriteDump',[Reflection.BindingFlags]'Static,NonPublic')
$searchPolicy=$dumpMethod.GetCustomAttributes([Runtime.InteropServices.DefaultDllImportSearchPathsAttribute],$false)
if($searchPolicy.Count -ne 1 -or $searchPolicy[0].Paths -ne [Runtime.InteropServices.DllImportSearchPath]::System32) {
    throw 'FAIL: dump import must exclude current-directory and PATH DLL lookup.'
}
$creationGuard=[Local022BoundedNativeDump].GetMethod('VerifyCreationTime',[Reflection.BindingFlags]'Static,NonPublic')
if($null -eq $creationGuard) { throw 'FAIL: missing native/CIM timestamp precision guard.' }
foreach($actual in @([long]100000000,[long]100000009)) {
    [void]$creationGuard.Invoke($null,@($actual,[long]100000000))
}
foreach($actual in @([long]99999999,[long]100000010,[long]100010000)) {
    $refused=$false
    try { [void]$creationGuard.Invoke($null,@($actual,[long]100000000)) }
    catch { if($_.Exception.ToString() -notmatch 'diagnostic_process_reused') { throw }; $refused=$true }
    if(-not $refused) { throw 'FAIL: native dump accepted a different creation microsecond.' }
}
Write-Output 'PASS: all three diagnostic C# units compile; dump lookup is System32-only and handle identity preserves CIM microsecond precision; no native APIs invoked.'

function Reject([string]$Code, [scriptblock]$Action) {
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -ne $Code) { throw }
        return
    }
    throw "FAIL: accepted $Code"
}

$root = Join-Path (Get-Local022DiagnosticBase) ('diagnostic-test-' + [Guid]::NewGuid().ToString('N'))
$outside = Join-Path $root 'outside'
New-Item -ItemType Directory -Path $root, (Join-Path $root 'private'), $outside | Out-Null
try {
    $run = '0123456789abcdef0123456789abcdef'
    $nonce = 'QS3D-AUTO-fedcba9876543210fedcba9876543210'
    $recovery = [ordered]@{schema='QS3D_V26_PROFILE_RECOVERY_V1'; state='ALLOCATED'; run_id=$run; nonce_prefix='QS3D-AUTO-'; nonce_profile=$nonce; profile_names_before=@('Default')}
    $recoveryPath = Join-Path $root 'profile-recovery.private.json'
    [IO.File]::WriteAllText($recoveryPath, ($recovery | ConvertTo-Json))
    $allocation = [ordered]@{schema='QS3D_LOCAL022_V26_ALLOCATION_V1'; run_id=$run; harness_git_sha=('a'*40); host_version='26.2.07'; host_sha256=('b'*64); interactive_ui=$true; profile_recovery_sha256=(Get-Local022DiagnosticHash $recoveryPath)}
    $allocationPath = Join-Path $root 'allocation.json'
    [IO.File]::WriteAllText($allocationPath, ($allocation | ConvertTo-Json))
    $allocationHash = Get-Local022DiagnosticHash $allocationPath
    $args49 = @{AllocationRoot=$root; ExpectedAllocationSha256=$allocationHash; RunId=$run}
    $context = Read-Local022DiagnosticAllocation @args49
    $binding = Read-Local022DiagnosticBinding $context
    if ($binding.Profile -cne $nonce) { throw 'FAIL: nonce changed.' }
    $exe = 'C:\Program Files\Bricsys\BricsCAD V26 en_US\bricscad.exe'
    $start = '2026-09-07T01:02:03.0000000Z'
    $record = [pscustomobject]@{ProcessId=123;ParentProcessId=456;CreationDate=[datetime]::Parse($start).ToUniversalTime();ExecutablePath=$exe;CommandLine=('"'+$exe+'" "'+$binding.Drawing+'" /L /P "'+$nonce+'" /B "'+$binding.Script+'"')}
    $identity = @{Record=$record; Binding=$binding; OwnedProcessId=123; OwnedParentId=456; ExpectedProcessStartUtc=$start; ExpectedExecutable=$exe}
    Assert-Local022DiagnosticProcess @identity
    Reject 'diagnostic_pid_mismatch' { $copy=$record.PSObject.Copy(); $copy.ProcessId=124; Assert-Local022DiagnosticProcess -Record $copy -Binding $binding -OwnedProcessId 123 -OwnedParentId 456 -ExpectedProcessStartUtc $start -ExpectedExecutable $exe }
    Reject 'diagnostic_pid_mismatch' { $copy=$record.PSObject.Copy(); $copy.ParentProcessId=457; Assert-Local022DiagnosticProcess -Record $copy -Binding $binding -OwnedProcessId 123 -OwnedParentId 456 -ExpectedProcessStartUtc $start -ExpectedExecutable $exe }
    Reject 'diagnostic_process_reused' { $copy=$record.PSObject.Copy(); $copy.CreationDate=$copy.CreationDate.AddSeconds(1); Assert-Local022DiagnosticProcess -Record $copy -Binding $binding -OwnedProcessId 123 -OwnedParentId 456 -ExpectedProcessStartUtc $start -ExpectedExecutable $exe }
    Reject 'diagnostic_executable_mismatch' { $copy=$record.PSObject.Copy(); $copy.ExecutablePath=$exe.Replace('V26','V25'); Assert-Local022DiagnosticProcess -Record $copy -Binding $binding -OwnedProcessId 123 -OwnedParentId 456 -ExpectedProcessStartUtc $start -ExpectedExecutable $exe }
    foreach ($changed in @($record.CommandLine.Replace($nonce,'Default'), ($record.CommandLine+' /P "Default"'), $record.CommandLine.Replace('single-footing-copy.dwg','other.dwg'), $record.CommandLine.Replace('ui.scr','run.scr'))) {
        $copy=$record.PSObject.Copy(); $copy.CommandLine=$changed
        Reject 'diagnostic_commandline_mismatch' { Assert-Local022DiagnosticProcess -Record $copy -Binding $binding -OwnedProcessId 123 -OwnedParentId 456 -ExpectedProcessStartUtc $start -ExpectedExecutable $exe }
    }
    Reject 'diagnostic_timestamp_invalid' { ConvertTo-Local022DiagnosticUtc '2026-09-07T01:02:03+07:00' }
    Reject 'diagnostic_allocation_hash' { Read-Local022DiagnosticAllocation -AllocationRoot $root -ExpectedAllocationSha256 ('0'*64) -RunId $run }
    Reject 'diagnostic_allocation_identity' { Read-Local022DiagnosticAllocation -AllocationRoot $root -ExpectedAllocationSha256 $allocationHash -RunId ('0'*32) }
    Reject 'diagnostic_allocation_root' { Read-Local022DiagnosticAllocation -AllocationRoot $outside -ExpectedAllocationSha256 $allocationHash -RunId $run }
    Reject 'diagnostic_private_path' { Resolve-Local022DiagnosticFile $context (Join-Path $root '../escape.private.dmp') '.private.dmp' -Fresh }
    Reject 'diagnostic_private_path' { Resolve-Local022DiagnosticFile $context (Join-Path $root 'private/nested.private.dmp') '.private.dmp' -Fresh }
    Reject 'diagnostic_private_path' { Resolve-Local022DiagnosticFile $context (Join-Path $root 'dump.txt') '.private.dmp' -Fresh }
    $dump = Join-Path $root 'snapshot.private.dmp'
    [IO.File]::WriteAllText($dump, 'not a native dump; guard fixture only')
    Reject 'diagnostic_output_exists' { Resolve-Local022DiagnosticFile $context $dump '.private.dmp' -Fresh }
    Reject 'diagnostic_hash_invalid' { Assert-Local022DiagnosticHex ('a'*64+"`n") 64 }
    Reject 'diagnostic_debugger_binary' { Assert-Local022DiagnosticBinary ('a'*64) ('b'*64) 'Valid' 'CN=Microsoft Corporation, O=Microsoft Corporation' }
    Reject 'diagnostic_debugger_binary' { Assert-Local022DiagnosticBinary ('a'*64) ('a'*64) 'NotSigned' 'CN=Microsoft Corporation, O=Microsoft Corporation' }
    Reject 'diagnostic_debugger_binary' { Assert-Local022DiagnosticBinary ('a'*64) ('a'*64) 'Valid' 'CN=Other Corporation' }
    Assert-Local022DiagnosticBinary ('a'*64) ('a'*64) 'Valid' 'CN=Microsoft Corporation, O=Microsoft Corporation, C=US'
    $junction=Join-Path $root 'redirect'
    New-Item -ItemType Junction -Path $junction -Target $outside | Out-Null
    Reject 'diagnostic_reparse_path' { Assert-Local022DiagnosticPath (Join-Path $junction 'missing.private.dmp') -AllowMissingLeaf }
    Remove-Item -LiteralPath $junction -Force
    [IO.File]::WriteAllText((Join-Path $root 'phase-ui.json'), '{}')
    Reject 'diagnostic_terminal_allocation' { Read-Local022DiagnosticBinding $context }
    Remove-Item -LiteralPath (Join-Path $root 'phase-ui.json')

    # Execute actual entry points with malformed pins, trapping native compilation.
    # No WCT/dump/debugger API, process query, UI, or BricsCAD assembly is called.
    $script:nativeCalls=0
    function Add-Type { $script:nativeCalls++; throw 'FAIL: native compilation reached' }
    function Get-CimInstance { throw 'FAIL: process query reached before rejection' }
    $entryArgs=@{AllocationRoot=$root;ExpectedAllocationSha256=('0'*64);RunId=$run;OwnedProcessId=123;OwnedParentId=456;ExpectedProcessStartUtc=$start;ExpectedExecutable=$exe}
    Reject 'diagnostic_allocation_hash' { & (Join-Path $PSScriptRoot 'read-owned-waitchain.ps1') @entryArgs }
    Reject 'diagnostic_dump_confirmation_required' { & (Join-Path $PSScriptRoot 'capture-owned-native-dump.ps1') @entryArgs -OutputPath (Join-Path $root 'fresh.private.dmp') }
    Reject 'diagnostic_allocation_hash' { & (Join-Path $PSScriptRoot 'capture-owned-native-dump.ps1') @entryArgs -OutputPath (Join-Path $root 'fresh.private.dmp') -ConfirmPrivateMemoryDump }
    Reject 'diagnostic_allocation_hash' { & (Join-Path $PSScriptRoot 'read-native-dump.ps1') -AllocationRoot $root -ExpectedAllocationSha256 ('0'*64) -RunId $run -DumpPath $dump -ExpectedDumpSha256 ('a'*64) -LogPath (Join-Path $root 'fresh.private.txt') -DebuggerDirectory $outside -SymbolDirectory $outside }
    if ($script:nativeCalls -ne 0) { throw 'FAIL: native API boundary crossed.' }
    Write-Output 'PASS: actual diagnostic guards and entry points reject wrong allocation/hash/PID/parent/creation/exe/arguments, terminal state, path escape/reparse/overwrite, untrusted binaries and missing memory-dump confirmation; no native execution.'
} finally {
    # Delete only this test-created direct allocation; reject links before cleanup.
    if ((Split-Path $root) -ine (Get-Local022DiagnosticBase)) { throw 'Unsafe test cleanup root.' }
    $items=@(Get-ChildItem -LiteralPath $root -Recurse -Force)
    foreach($item in $items) { if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Test cleanup found retained reparse point.' } }
    foreach($item in @($items | Where-Object { -not $_.PSIsContainer })) { Remove-Item -LiteralPath $item.FullName -Force }
    foreach($item in @($items | Where-Object PSIsContainer | Sort-Object { $_.FullName.Length } -Descending)) { Remove-Item -LiteralPath $item.FullName }
    Remove-Item -LiteralPath $root
}
