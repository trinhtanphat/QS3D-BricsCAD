[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'QS3D\McpDashboard'),
    [switch]$NoStart
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tools\QS3D.McpDashboard\QS3D.McpDashboard.csproj'
$taskName = 'QS3D MCP Local Dashboard'
$exeName = 'QS3D.McpDashboard.exe'
$publishTemp = Join-Path $env:TEMP ('qs3d-mcp-dashboard-' + [Guid]::NewGuid().ToString('N'))

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Dashboard project not found: $project"
}

try {
    New-Item -ItemType Directory -Force -Path $publishTemp | Out-Null
    $runtimeOk = (& dotnet --list-runtimes) -match '^Microsoft.AspNetCore.App 8\.'
    if (-not $runtimeOk) { throw 'Microsoft.AspNetCore.App 8.x runtime is required for the local MCP dashboard.' }
    & dotnet publish $project -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=false -p:PublishTrimmed=false -o $publishTemp --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

    $publishedExe = Join-Path $publishTemp $exeName
    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw "Published dashboard executable not found: $publishedExe"
    }

    $installedExe = Join-Path $InstallRoot $exeName
    Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($exeName)) -ErrorAction SilentlyContinue |
        ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
    $unlockDeadline = [DateTime]::UtcNow.AddSeconds(5)
    while ((Test-Path -LiteralPath $installedExe) -and [DateTime]::UtcNow -lt $unlockDeadline) {
        try {
            $probe = [IO.File]::Open($installedExe, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $probe.Dispose()
            break
        } catch { Start-Sleep -Milliseconds 100 }
    }

    New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null
    Get-ChildItem -LiteralPath $InstallRoot -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
    Copy-Item -Path (Join-Path $publishTemp '*') -Destination $InstallRoot -Recurse -Force

    $action = New-ScheduledTaskAction -Execute $installedExe
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
    $settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -RestartCount 3 `
        -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit (New-TimeSpan -Days 1) `
        -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings `
        -Description 'Loopback-only QS3D MCP/tunnel health dashboard on 127.0.0.1:3220.' -Force | Out-Null

    if (-not $NoStart) {
        Start-ScheduledTask -TaskName $taskName
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        $ready = $false
        while ([DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 250
            try {
                $r = Invoke-WebRequest 'http://127.0.0.1:3220/' -UseBasicParsing -TimeoutSec 2
                if ($r.StatusCode -eq 200 -and $r.Content -match 'QS3D MCP Local Dashboard') { $ready = $true; break }
            } catch { }
        }
        if (-not $ready) { throw 'Dashboard did not become ready on http://127.0.0.1:3220/.' }
    }

    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction Stop
    if ($task.State -eq 'Disabled') { throw 'Dashboard scheduled task is disabled after registration.' }
    Write-Host "MCP_DASHBOARD_INSTALL=PASS root=$InstallRoot task=$taskName url=http://127.0.0.1:3220/"
}
finally {
    Remove-Item -LiteralPath $publishTemp -Recurse -Force -ErrorAction SilentlyContinue
}
