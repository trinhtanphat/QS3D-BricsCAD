$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
foreach ($major in @(25,26)) {
    $runner = Get-Content (Join-Path $PSScriptRoot "../../scripts/test-bricscad-v$major-single-footing.ps1") -Raw
    foreach ($function in @('Get-Local022OperatorWaitPolicy','New-Local022PhaseClock','Update-Local022PhaseClock')) {
        $actual = [regex]::Match($runner, "(?ms)^function $function\([^\r\n]*\) \{.*?^\}").Value
        if (-not $actual) { throw "FAIL: V$major actual $function missing." }
        . ([scriptblock]::Create($actual))
    }
    if ((Get-Local022OperatorWaitPolicy $false $false 'NATIVE_V1') -cne 'WALL_CLOCK_V1' -or
        (Get-Local022OperatorWaitPolicy $true $true 'OBSERVED_CLICK_V2') -cne 'PAUSE_FOR_OPERATOR_V1') { throw 'FAIL: policy identity.' }
    foreach ($case in @(@($true,$false,'OBSERVED_CLICK_V2'),@($true,$true,'NATIVE_V1'))) {
        $rejected = $false
        try { Get-Local022OperatorWaitPolicy @case } catch { $rejected = $true }
        if (-not $rejected) { throw 'FAIL: pause without observed interactive mode accepted.' }
    }
    $start = [DateTime]::SpecifyKind([DateTime]'2026-09-05', [DateTimeKind]::Utc)
    foreach ($phase in @('ui','uireopen','uisaved','create')) {
        foreach ($pause in @($true,$false)) {
            $clock = New-Local022PhaseClock $start $phase 3600 $pause
            $expected = if ($pause -and $phase -ceq 'ui') { 14400 } else { 3600 }
            if ($clock.Deadline -ne $start.AddSeconds($expected)) { throw 'FAIL: outer phase deadline changed outside operator UI allowance.' }
            Update-Local022PhaseClock $clock $start.AddSeconds(4000) $phase 3600 $pause $false
            if ($clock.Deadline -ne $start.AddSeconds($expected)) { throw 'FAIL: unverified marker changed deadline.' }
            Update-Local022PhaseClock $clock $start.AddSeconds(5000) $phase 3600 $pause $true
            $after = if ($pause -and $phase -ceq 'ui') { 8600 } else { $expected }
            if ($clock.Deadline -ne $start.AddSeconds($after)) { throw 'FAIL: verified UI did not start bounded save deadline.' }
            Update-Local022PhaseClock $clock $start.AddSeconds(7000) $phase 3600 $pause $true
            if ($clock.Deadline -ne $start.AddSeconds($after)) { throw 'FAIL: repeated marker extended save deadline.' }
        }
    }
    $clock = New-Local022PhaseClock $start 'ui' 3600 $true
    Update-Local022PhaseClock $clock $start.AddSeconds(14000) 'ui' 3600 $true $true
    if ($clock.Deadline -ne $start.AddSeconds(14400)) { throw 'FAIL: operator hard wall limit was extended.' }
}
Write-Output 'PASS: both actual native runner policies reject implicit pause; only UI gets finite four-hour allowance, verified UI starts one-shot bounded save deadline, cold/legacy timing unchanged.'
