param(
    [Parameter(Mandatory = $true)][string]$MsiPath,
    [Parameter(Mandatory = $true)][string]$ExtractDir,
    [Parameter(Mandatory = $true)][string]$ExpectedSha256,
    [Parameter(Mandatory = $true)][string]$MirrorUrl,
    [Parameter(Mandatory = $true)][string]$PublicUrl,
    [string]$FallbackUrl = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'BricsCAD V25 compile-reference acquisition requires Windows.'
}

$expected = $ExpectedSha256.Trim().ToUpperInvariant()
if ($expected -notmatch '^[0-9A-F]{64}$') {
    throw 'ExpectedSha256 must be one 64-hex SHA-256 digest.'
}

$msi = [IO.Path]::GetFullPath($MsiPath)
$extract = [IO.Path]::GetFullPath($ExtractDir)
$cacheDir = Split-Path -Parent $msi
New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $extract -Force | Out-Null

function Test-PinnedMsi {
    if (-not (Test-Path -LiteralPath $msi -PathType Leaf)) { return $false }
    if ((Get-Item -LiteralPath $msi).Length -le 1048576) { return $false }
    $actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash.ToUpperInvariant()
    if (-not [string]::Equals($actual, $expected, [StringComparison]::Ordinal)) {
        Write-Warning "Discarding BricsCAD V25 MSI with unexpected SHA256: $actual"
        Remove-Item -LiteralPath $msi -Force -ErrorAction SilentlyContinue
        return $false
    }
    return $true
}

$sourceName = $null
if (Test-PinnedMsi) {
    $sourceName = 'actions-cache/local-cache'
}
else {
    $candidates = @(
        [pscustomobject]@{ Name = 'pinned-user-mirror'; Url = $MirrorUrl },
        [pscustomobject]@{ Name = 'pinned-public'; Url = $PublicUrl }
    )
    if (-not [string]::IsNullOrWhiteSpace($FallbackUrl)) {
        $candidates += [pscustomobject]@{ Name = 'signed-secret-fallback'; Url = $FallbackUrl }
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace([string]$candidate.Url)) { continue }
        Remove-Item -LiteralPath $msi -Force -ErrorAction SilentlyContinue
        try {
            Write-Host "Downloading BricsCAD V25 installer from $($candidate.Name)..."
            Invoke-WebRequest -Uri $candidate.Url -OutFile $msi -MaximumRedirection 10 -TimeoutSec 1200 -UseBasicParsing
            if (-not (Test-PinnedMsi)) { continue }
            $sourceName = $candidate.Name
            break
        }
        catch {
            Write-Warning "BricsCAD V25 installer source failed: $($candidate.Name) • $($_.Exception.Message)"
        }
    }
}

if ([string]::IsNullOrWhiteSpace($sourceName) -or -not (Test-PinnedMsi)) {
    throw 'Unable to obtain the exact pinned BricsCAD V25.2.10 x64 installer.'
}

$actualHash = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash.ToUpperInvariant()
$signature = Get-AuthenticodeSignature -FilePath $msi
if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $signature.SignerCertificate) {
    throw "BricsCAD V25 MSI Authenticode signature is not valid: $($signature.Status)."
}
$signerSubject = [string]$signature.SignerCertificate.Subject
if ($signerSubject -notmatch '(^|,\s*)(CN|O)=Bricsys(,|$)') {
    throw "BricsCAD V25 MSI signer is not Bricsys: $signerSubject"
}

$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.OpenDatabase($msi, 0)
$versionView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')
$versionView.Execute()
$versionRecord = $versionView.Fetch()
$productVersion = if ($versionRecord) { [string]$versionRecord.StringData(1) } else { [string]::Empty }
$nameView = $database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')
$nameView.Execute()
$nameRecord = $nameView.Fetch()
$productName = if ($nameRecord) { [string]$nameRecord.StringData(1) } else { [string]::Empty }
if ($productVersion -notmatch '^25\.2\.10(?:\.|$)') {
    throw "Downloaded MSI is not the pinned BricsCAD V25.2.10 product. ProductVersion=$productVersion"
}
if ($productName -notmatch 'BricsCAD') {
    throw "Downloaded MSI ProductName is not BricsCAD: $productName"
}

$msiLog = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-admin-' + [Guid]::NewGuid().ToString('N') + '.log')
try {
    $arguments = @('/a', ('"' + $msi + '"'), '/qn', '/norestart', ('TARGETDIR="' + $extract + '"'), 'REBOOT=ReallySuppress', '/L*v', ('"' + $msiLog + '"'))
    Write-Host 'Starting BricsCAD V25 MSI administrative extraction (15-minute process timeout)...'
    $process = Start-Process -FilePath msiexec.exe -ArgumentList $arguments -PassThru
    $exited = $process.WaitForExit(900000)
    if (-not $exited) {
        try { $process.Kill() } catch { }
        if (Test-Path -LiteralPath $msiLog -PathType Leaf) { Get-Content -LiteralPath $msiLog -Tail 120 }
        throw 'BricsCAD V25 MSI administrative extraction timed out after 15 minutes.'
    }
    if ($process.ExitCode -notin @(0, 3010)) {
        if (Test-Path -LiteralPath $msiLog -PathType Leaf) { Get-Content -LiteralPath $msiLog -Tail 120 }
        throw "BricsCAD V25 MSI administrative extraction failed with exit code $($process.ExitCode)."
    }
}
finally {
    Remove-Item -LiteralPath $msiLog -Force -ErrorAction SilentlyContinue
}

$brx = @(Get-ChildItem -LiteralPath $extract -Recurse -File -Filter 'BrxMgd.dll')
$candidateDirs = @($brx | ForEach-Object { $_.Directory.FullName } | Sort-Object -Unique)
$bricsDir = $candidateDirs | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_ 'BrxMgd.dll') -PathType Leaf) -and
    (Test-Path -LiteralPath (Join-Path $_ 'TD_Mgd.dll') -PathType Leaf) -and
    (Test-Path -LiteralPath (Join-Path $_ 'TD_MgdBrep.dll') -PathType Leaf)
} | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($bricsDir)) {
    throw 'BrxMgd.dll, TD_Mgd.dll, and TD_MgdBrep.dll were not found together in one extracted V25 runtime directory.'
}

Write-Host "Verified BricsCAD V25.2.10 MSI SHA256: $actualHash"
Write-Host "Verified installer signer: $signerSubject"
Write-Host "Verified MSI identity: $productName $productVersion"
Write-Host "V25 compile references resolved from $sourceName."
Write-Output $bricsDir
