# Source-only guards. Dot-sourcing this file performs no process or native calls.
Set-StrictMode -Version Latest
function Assert-Local022DiagnosticWindows {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or -not [Environment]::Is64BitProcess) {
        throw 'diagnostic_windows_x64_required'
    }
}
function Get-Local022DiagnosticBase {
    [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../artifacts/issue-5718-local022'))
}
function Assert-Local022DiagnosticHex([string]$Value, [int]$Length) {
    if ($Value -cnotmatch ('\A[0-9a-fA-F]{'+$Length+'}\z')) { throw 'diagnostic_hash_invalid' }
}
function Get-Local022DiagnosticHash([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Assert-Local022DiagnosticPath([string]$Path, [switch]$AllowMissingLeaf) {
    # Reject UNC/device paths, alternate streams, debugger metacharacters and links.
    if ($Path -notmatch '\A[A-Za-z]:[\\/]' -or $Path.Substring(2) -match '[:*?"<>|;\r\n]' -or
        $Path -match '[\\/]\.\.?([\\/]|\z)') { throw 'diagnostic_path_invalid' }
    $full=[IO.Path]::GetFullPath($Path).TrimEnd('\','/')
    $probe=$full
    if (-not (Test-Path -LiteralPath $probe)) {
        if (-not $AllowMissingLeaf) { throw 'diagnostic_path_missing' }
        $probe=Split-Path $probe
        if (-not (Test-Path -LiteralPath $probe -PathType Container)) { throw 'diagnostic_path_missing' }
    }
    while (-not [string]::IsNullOrEmpty($probe)) {
        $item=Get-Item -LiteralPath $probe -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'diagnostic_reparse_path' }
        $parent=Split-Path $probe
        if ($parent -eq $probe) { break }
        $probe=$parent
    }
    return $full
}
function Read-Local022DiagnosticAllocation([string]$AllocationRoot, [string]$ExpectedAllocationSha256, [string]$RunId) {
    Assert-Local022DiagnosticHex $ExpectedAllocationSha256 64
    Assert-Local022DiagnosticHex $RunId 32
    $root=[IO.Path]::GetFullPath($AllocationRoot).TrimEnd('\','/')
    if ((Split-Path $root) -ine (Get-Local022DiagnosticBase) -or
        (Split-Path $root -Leaf) -cnotmatch '\A[a-z0-9-]{4,80}\z') { throw 'diagnostic_allocation_root' }
    $root=Assert-Local022DiagnosticPath $root
    $path=Assert-Local022DiagnosticPath (Join-Path $root 'allocation.json')
    if ((Get-Item -LiteralPath $path).Length -gt 1048576) { throw 'diagnostic_allocation_size' }
    if ((Get-Local022DiagnosticHash $path) -ine $ExpectedAllocationSha256) { throw 'diagnostic_allocation_hash' }
    $allocation=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -ErrorAction Stop
    if ($allocation.schema -cnotin @('QS3D_LOCAL022_ALLOCATION_V1','QS3D_LOCAL022_V26_ALLOCATION_V1') -or
        $allocation.run_id -cne $RunId -or $allocation.host_version -notmatch '\A(25|26)\.') { throw 'diagnostic_allocation_identity' }
    $major=[int]($allocation.host_version.Split('.')[0])
    if (($major -eq 26) -ne ($allocation.schema -ceq 'QS3D_LOCAL022_V26_ALLOCATION_V1')) { throw 'diagnostic_allocation_identity' }
    Assert-Local022DiagnosticHex $allocation.harness_git_sha 40
    Assert-Local022DiagnosticHex $allocation.host_sha256 64
    return [pscustomobject]@{Root=$root;Allocation=$allocation;AllocationHash=$ExpectedAllocationSha256.ToLowerInvariant();RunId=$RunId;HostMajor=$major}
}
function Read-Local022DiagnosticBinding($Context) {
    [void](Read-Local022DiagnosticAllocation $Context.Root $Context.AllocationHash $Context.RunId)
    if($Context.Allocation.interactive_ui -isnot [bool] -or -not $Context.Allocation.interactive_ui) { throw 'diagnostic_initial_ui_required' }
    # Live diagnostics cannot attach to a consumed/failed allocation.
    foreach($name in @('receipt.json','phase-ui.json','phase-uisaved.json','phase-run.json','phase-saved.json','phase-reopen.json','phase-uireopen.json')) {
        if(Test-Path -LiteralPath (Join-Path $Context.Root $name)) { throw 'diagnostic_terminal_allocation' }
    }
    $path=Assert-Local022DiagnosticPath (Join-Path $Context.Root 'profile-recovery.private.json')
    Assert-Local022DiagnosticHex $Context.Allocation.profile_recovery_sha256 64
    if ((Get-Local022DiagnosticHash $path) -ine $Context.Allocation.profile_recovery_sha256) { throw 'diagnostic_recovery_hash' }
    $recovery=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -ErrorAction Stop
    if ($recovery.schema -cne ('QS3D_V'+$Context.HostMajor+'_PROFILE_RECOVERY_V1') -or $recovery.state -cne 'ALLOCATED' -or
        $recovery.run_id -cne $Context.RunId -or $recovery.nonce_prefix -cne 'QS3D-AUTO-' -or
        $recovery.nonce_profile -cnotmatch '\AQS3D-AUTO-[0-9a-f]{32}\z' -or
        @($recovery.profile_names_before) -contains $recovery.nonce_profile) { throw 'diagnostic_recovery_identity' }
    # Only initial UI startup is supported; cold reopen/native API need separate bindings.
    $drawing=Join-Path $Context.Root 'private/single-footing-copy.dwg'
    $scriptPath=Join-Path $Context.Root 'private/ui.scr'
    [void](Assert-Local022DiagnosticPath (Split-Path $drawing))
    return [pscustomobject]@{Context=$Context;Profile=$recovery.nonce_profile;Drawing=$drawing;Script=$scriptPath}
}
function ConvertTo-Local022DiagnosticUtc([string]$Value) {
    if ($Value -cnotmatch '\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z\z') { throw 'diagnostic_timestamp_invalid' }
    [datetime]::ParseExact($Value,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind)
}
function Assert-Local022DiagnosticProcess($Record, $Binding, [int]$OwnedProcessId, [int]$OwnedParentId, [string]$ExpectedProcessStartUtc, [string]$ExpectedExecutable) {
    $started=ConvertTo-Local022DiagnosticUtc $ExpectedProcessStartUtc
    if ($null -eq $Record -or $OwnedProcessId -le 0 -or $OwnedParentId -le 0 -or
        $Record.ProcessId -ne $OwnedProcessId -or $Record.ParentProcessId -ne $OwnedParentId) { throw 'diagnostic_pid_mismatch' }
    if ($Record.CreationDate -isnot [datetime] -or $Record.CreationDate.ToUniversalTime().Ticks -ne $started.Ticks) { throw 'diagnostic_process_reused' }
    if ($ExpectedExecutable -notmatch ('(?i)\\BricsCAD V'+$Binding.Context.HostMajor+' en_US\\bricscad\.exe\z') -or
        -not [string]::Equals($Record.ExecutablePath,$ExpectedExecutable,[StringComparison]::OrdinalIgnoreCase)) { throw 'diagnostic_executable_mismatch' }
    $args='"'+$Binding.Drawing+'"'+$(if($Binding.Context.HostMajor -eq 26){' /L'}else{''})+' /P "'+$Binding.Profile+'" /B "'+$Binding.Script+'"'
    $allowed=@(('"'+$ExpectedExecutable+'" '+$args),($ExpectedExecutable+' '+$args))
    if ($allowed -cnotcontains $Record.CommandLine) { throw 'diagnostic_commandline_mismatch' }
}
function Get-Local022DiagnosticProcess($Binding, [int]$OwnedProcessId, [int]$OwnedParentId, [string]$ExpectedProcessStartUtc, [string]$ExpectedExecutable) {
    [void](ConvertTo-Local022DiagnosticUtc $ExpectedProcessStartUtc)
    if($OwnedProcessId -le 0 -or $OwnedParentId -le 0) { throw 'diagnostic_pid_mismatch' }
    $exe=Assert-Local022DiagnosticPath $ExpectedExecutable
    if ((Get-Local022DiagnosticHash $exe) -ine $Binding.Context.Allocation.host_sha256) { throw 'diagnostic_host_hash' }
    [void](Assert-Local022DiagnosticPath $Binding.Drawing)
    [void](Assert-Local022DiagnosticPath $Binding.Script)
    $record=Get-CimInstance Win32_Process -Filter "ProcessId=$OwnedProcessId"
    Assert-Local022DiagnosticProcess $record $Binding $OwnedProcessId $OwnedParentId $ExpectedProcessStartUtc $exe
    return $record
}
function Resolve-Local022DiagnosticFile($Context,[string]$Path,[string]$Suffix,[switch]$Fresh) {
    $full=[IO.Path]::GetFullPath($Path)
    if ((Split-Path $full) -ine $Context.Root -or -not $full.EndsWith($Suffix,[StringComparison]::Ordinal) -or
        (Split-Path $full -Leaf) -cnotmatch '\A[a-z0-9][a-z0-9.-]{1,100}\z') { throw 'diagnostic_private_path' }
    if($Fresh -and (Test-Path -LiteralPath $full)) { throw 'diagnostic_output_exists' }
    Assert-Local022DiagnosticPath $full -AllowMissingLeaf:$Fresh
}
function Assert-Local022DiagnosticBinary([string]$ActualHash,[string]$ExpectedHash,[string]$Status,[string]$Subject) {
    if ($ActualHash -ine $ExpectedHash -or $Status -cne 'Valid' -or
        $Subject -notmatch '(^|, )O=Microsoft Corporation(,|$)') { throw 'diagnostic_debugger_binary' }
}
