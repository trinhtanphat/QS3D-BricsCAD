[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$VersionKey,
    [Parameter(Mandatory = $true)][string]$LanguageKey,
    [Parameter(Mandatory = $true)][string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)][ValidatePattern('^https://')][string]$UpgradeManifestUri,
    [Parameter(Mandatory = $true)][ValidatePattern('^https://')][string]$RollbackManifestUri,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$ExpectedSignerThumbprint,
    [string]$ArtifactDir = (Join-Path $PSScriptRoot '..\artifacts\local-v26-package-update-lifecycle'),
    [switch]$ConfirmDisposableInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageDir = Join-Path $root 'dist\QS3D-BricsCAD-V26'
$qualificationRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'QS3D\Qualification'))
$installDir = Join-Path $qualificationRoot ('V26-Update-' + [Guid]::NewGuid().ToString('N'))
$artifactFull = [IO.Path]::GetFullPath($ArtifactDir)
$sentinel = Join-Path $qualificationRoot ('unrelated-' + [Guid]::NewGuid().ToString('N') + '.txt')
$sentinelValue = [Guid]::NewGuid().ToString('N')
$originalV26Dir = $env:BRICSCAD_V26_DIR
$result = [ordered]@{ schema=2; status='FAIL'; sourceSha=''; hostMajor=0; baselineVersion=''; upgradedVersion=''; baselineInstalled=$false; upgradeSucceeded=$false; upgradedPayloadValid=$false; downgradeRejected=$false; downgradePreservedState=$false; transactionalFailureRejected=$false; transactionalPayloadRolledBack=$false; transactionalRegistryRolledBack=$false; cancelPreservedState=$false; unrelatedSentinelPreserved=$false; cleanupComplete=$false }

function Assert-Leaf([string]$Path,[string]$Label) { if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label was not found." } }
function Assert-Inside([string]$Candidate,[string]$Parent,[string]$Label) {
    $candidateFull=[IO.Path]::GetFullPath($Candidate)
    $parentFull=[IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    if (-not $candidateFull.StartsWith($parentFull,[StringComparison]::OrdinalIgnoreCase)) { throw "$Label must stay inside the disposable qualification root." }
}
function Assert-CleanExactSource {
    $head=(& git -C $root rev-parse HEAD).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve exact Git HEAD.' }
    if ($head -ne $ExpectedSourceSha.Trim().ToLowerInvariant()) { throw 'ExpectedSourceSha does not match exact Git HEAD.' }
    if (@(& git -C $root status --porcelain).Count -ne 0) { throw 'Qualification requires a completely clean working tree.' }
    $result.sourceSha=$head
}
function Assert-HostIdentity {
    $exe=Join-Path $BricsCadDir 'bricscad.exe'; Assert-Leaf $exe 'BricsCAD executable'
    foreach($name in @('BrxMgd.dll','TD_Mgd.dll','TD_MgdBrep.dll')) { Assert-Leaf (Join-Path $BricsCadDir $name) "BricsCAD V26 $name" }
    $major=0; $version=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    if ([string]::IsNullOrWhiteSpace($version) -or -not [int]::TryParse($version.Split('.')[0],[ref]$major) -or $major -ne 26) { throw 'Configured BricsCAD host is not major version 26.' }
    if ($VersionKey -notmatch '^V26(?:\.|$)' -or $LanguageKey -notmatch '^[A-Za-z]{2}_[A-Za-z]{2}$') { throw 'V26 registry identity is not canonical.' }
    $result.hostMajor=$major
}
function Get-TreeDigest([string]$Directory) {
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) { return 'MISSING' }
    $base=[IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $lines=[Collections.Generic.List[string]]::new()
    foreach($file in @(Get-ChildItem -LiteralPath $Directory -Recurse -File | Sort-Object FullName)) {
        $full=[IO.Path]::GetFullPath($file.FullName); if(-not $full.StartsWith($base,[StringComparison]::OrdinalIgnoreCase)){throw 'Installed payload escaped qualification root.'}
        $relative=$full.Substring($base.Length).Replace([IO.Path]::DirectorySeparatorChar,'/').Replace([IO.Path]::AltDirectorySeparatorChar,'/')
        $lines.Add($relative+'='+(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToUpperInvariant())
    }
    $sha=[Security.Cryptography.SHA256]::Create(); try{return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))))).Replace('-','')}finally{$sha.Dispose()}
}
function Get-DemandLoadDigest {
    $appKey="HKCU:\Software\Bricsys\BricsCAD\$VersionKey\$LanguageKey\Applications\QS3D"
    if(-not (Test-Path -LiteralPath $appKey)){return 'MISSING'}
    $lines=[Collections.Generic.List[string]]::new(); $key=Get-Item -LiteralPath $appKey
    try { foreach($name in @($key.GetValueNames()|Sort-Object)){ $lines.Add("app:$name=$($key.GetValue($name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames))") } } finally { $key.Close() }
    $commandsKey=Join-Path $appKey 'Commands'
    if(Test-Path -LiteralPath $commandsKey){$key=Get-Item -LiteralPath $commandsKey; try{foreach($name in @($key.GetValueNames()|Sort-Object)){$lines.Add("cmd:$name=$($key.GetValue($name))")}}finally{$key.Close()}}
    $sha=[Security.Cryptography.SHA256]::Create(); try{return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))))).Replace('-','')}finally{$sha.Dispose()}
}
function Read-Version {
    $path=Join-Path $installDir 'PACKAGE-METADATA.json'; Assert-Leaf $path 'Installed package metadata'
    $metadata=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json; $value=[string]$metadata.productVersion
    if([string]::IsNullOrWhiteSpace($value)){throw 'Installed productVersion is missing.'}; return $value
}
function Assert-Payload {
    $manifest=Join-Path $installDir 'SHA256SUMS.txt'; Assert-Leaf $manifest 'Installed SHA256 manifest'
    $base=[IO.Path]::GetFullPath($installDir).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
    $count=0
    foreach($line in @(Get-Content -LiteralPath $manifest)) {
        if([string]::IsNullOrWhiteSpace($line)){continue}; if($line -notmatch '^([0-9A-F]{64})  ([^\\:]+)$'){throw 'Installed hash manifest is malformed.'}
        $relative=$Matches[2]; $segments=@($relative.Split('/'))
        if([IO.Path]::IsPathRooted($relative) -or @($segments|Where-Object{[string]::IsNullOrWhiteSpace($_)-or $_ -eq '.' -or $_ -eq '..'}).Count -gt 0){throw 'Installed hash manifest contains an unsafe path.'}
        $payload=[IO.Path]::GetFullPath((Join-Path $installDir $relative.Replace('/',[IO.Path]::DirectorySeparatorChar)))
        if(-not $payload.StartsWith($base,[StringComparison]::OrdinalIgnoreCase)){throw 'Installed hash path escaped install root.'}; Assert-Leaf $payload 'Installed hashed payload'
        if((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToUpperInvariant() -ne $Matches[1]){throw "Installed payload hash mismatch: $relative"}; $count++
    }
    if($count -eq 0){throw 'Installed hash manifest is empty.'}; Assert-Leaf (Join-Path $installDir 'update-v26.ps1') 'Installed V26 updater'
}

try {
    if(-not $ConfirmDisposableInstall){throw 'Pass -ConfirmDisposableInstall to acknowledge disposable qualification files/registration.'}
    if([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT){throw 'V26 package update lifecycle requires Windows.'}
    if([string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)){throw 'LOCALAPPDATA is required.'}
    if(Get-Process -Name bricscad -ErrorAction SilentlyContinue){throw 'Close all BricsCAD processes before package update qualification.'}
    Assert-Inside $installDir $qualificationRoot 'InstallDirectory'; Assert-CleanExactSource; Assert-HostIdentity
    New-Item -ItemType Directory -Path $qualificationRoot -Force | Out-Null; Set-Content -LiteralPath $sentinel -Value $sentinelValue -Encoding ASCII
    $env:BRICSCAD_V26_DIR=[IO.Path]::GetFullPath($BricsCadDir)
    & dotnet build (Join-Path $root 'src\QS3D.BricsCAD.V26\QS3D.BricsCAD.V26.csproj') -c Release '-p:Platform=x64'; if($LASTEXITCODE -ne 0){throw 'V26 Release build failed.'}
    & (Join-Path $root 'scripts\package-v26.ps1'); if($LASTEXITCODE -ne 0){throw 'V26 package creation failed.'}
    & (Join-Path $packageDir 'install-v26-autoload.ps1') -PackageDirectory $packageDir -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false
    Assert-Payload; $result.baselineVersion=Read-Version; $result.baselineInstalled=$true

    & (Join-Path $installDir 'update-v26.ps1') -ManifestUri $UpgradeManifestUri -ExpectedSignerThumbprint $ExpectedSignerThumbprint -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false
    if($LASTEXITCODE -ne 0){throw 'Signed V26 upgrade failed.'}; Assert-Payload; $result.upgradedVersion=Read-Version
    if([string]::Equals($result.baselineVersion,$result.upgradedVersion,[StringComparison]::Ordinal)){throw 'Upgrade did not change installed productVersion.'}
    $result.upgradeSucceeded=$true; $result.upgradedPayloadValid=$true; $upgradedDigest=Get-TreeDigest $installDir; $upgradedRegistryDigest=Get-DemandLoadDigest

    $downgradeFailed=$false
    try { & (Join-Path $installDir 'update-v26.ps1') -ManifestUri $RollbackManifestUri -ExpectedSignerThumbprint $ExpectedSignerThumbprint -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false; if($LASTEXITCODE -ne 0){$downgradeFailed=$true} } catch { $downgradeFailed=$true }
    $result.downgradeRejected=$downgradeFailed; if(-not $downgradeFailed){throw 'Rollback/downgrade manifest was not rejected.'}
    $result.downgradePreservedState=[string]::Equals($upgradedDigest,(Get-TreeDigest $installDir),[StringComparison]::Ordinal); if(-not $result.downgradePreservedState){throw 'Rejected downgrade changed installed payload.'}

    # Deterministically force the real installer catch/rollback path after the staged baseline
    # payload has replaced the upgraded directory. Command shadowing is scoped to this process;
    # no production installer/updater bypass or qualification switch is added to shipped code.
    $transactionFailed=$false
    function global:New-ItemProperty { throw 'QS3D qualification injected registry-write failure.' }
    try {
        try {
            & (Join-Path $packageDir 'install-v26-autoload.ps1') -PackageDirectory $packageDir -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Force -Confirm:$false
        }
        catch { $transactionFailed=$true }
    }
    finally { Remove-Item -LiteralPath Function:\global:New-ItemProperty -Force -ErrorAction SilentlyContinue }
    $result.transactionalFailureRejected=$transactionFailed; if(-not $transactionFailed){throw 'Qualification fault did not force installer failure.'}
    Assert-Payload
    $result.transactionalPayloadRolledBack=[string]::Equals($upgradedDigest,(Get-TreeDigest $installDir),[StringComparison]::Ordinal)
    $result.transactionalRegistryRolledBack=[string]::Equals($upgradedRegistryDigest,(Get-DemandLoadDigest),[StringComparison]::Ordinal)
    if(-not $result.transactionalPayloadRolledBack -or -not $result.transactionalRegistryRolledBack){throw 'Installer transaction did not restore upgraded payload and DemandLoad registration.'}
    if(-not [string]::Equals($result.upgradedVersion,(Read-Version),[StringComparison]::Ordinal)){throw 'Installer rollback did not restore upgraded productVersion.'}

    & (Join-Path $installDir 'update-v26.ps1') -ManifestUri $UpgradeManifestUri -ExpectedSignerThumbprint $ExpectedSignerThumbprint -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -AllowSameVersion -WhatIf
    $result.cancelPreservedState=[string]::Equals($upgradedDigest,(Get-TreeDigest $installDir),[StringComparison]::Ordinal); if(-not $result.cancelPreservedState){throw 'WhatIf/cancel path changed installed payload.'}
    $result.unrelatedSentinelPreserved=(Test-Path -LiteralPath $sentinel -PathType Leaf) -and ((Get-Content -LiteralPath $sentinel -Raw).Trim() -eq $sentinelValue)
    if(-not $result.unrelatedSentinelPreserved){throw 'Update lifecycle changed unrelated sentinel state.'}; $result.status='PASS'
}
finally {
    Remove-Item -LiteralPath Function:\global:New-ItemProperty -Force -ErrorAction SilentlyContinue
    $env:BRICSCAD_V26_DIR=$originalV26Dir
    if(Test-Path -LiteralPath (Join-Path $installDir 'uninstall-v26-autoload.ps1')) { & (Join-Path $installDir 'uninstall-v26-autoload.ps1') -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -Confirm:$false -ErrorAction SilentlyContinue }
    Remove-Item -LiteralPath $sentinel -Force -ErrorAction SilentlyContinue
    $result.cleanupComplete=(-not (Test-Path -LiteralPath $installDir))-and(-not (Test-Path -LiteralPath $sentinel))
    New-Item -ItemType Directory -Path $artifactFull -Force | Out-Null
    $result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifactFull 'v26-package-update-lifecycle.json') -Encoding UTF8
    Write-Host ("QS3D_V26_PACKAGE_UPDATE_LIFECYCLE status={0} source={1} baseline={2} upgraded={3} cleanup={4}" -f $result.status,$result.sourceSha,$result.baselineVersion,$result.upgradedVersion,$result.cleanupComplete)
}
if($result.status -ne 'PASS' -or -not $result.cleanupComplete){exit 1}
