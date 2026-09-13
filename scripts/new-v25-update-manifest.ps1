[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$PackageUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V25.update.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$wrapperCmdlet = $PSCmdlet

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'V25 update-manifest publication requires Windows for generation-owned Win32 publication semantics.'
}

$validationCorePath = Join-Path $PSScriptRoot 'new-v25-update-manifest-validation-core.ps1'
$nativeHelperPath = Join-Path $PSScriptRoot 'Qs3dV25UpdateManifestPublicationNative.cs'
if (-not (Test-Path -LiteralPath $validationCorePath -PathType Leaf)) {
    throw "V25 update-manifest validation core is missing: $validationCorePath"
}
if (-not (Test-Path -LiteralPath $nativeHelperPath -PathType Leaf)) {
    throw "V25 update-manifest owned-publication helper is missing: $nativeHelperPath"
}

# Preserve the existing package/signature/version admission logic byte-for-byte.
# -WhatIf is mandatory here: the validation core's legacy pathname publication
# block must never become the authority that writes the caller's OutputPath.
. $validationCorePath `
    -PackageDirectory $PackageDirectory `
    -PackageZip $PackageZip `
    -PackageUri $PackageUri `
    -ExpectedSignerThumbprint $ExpectedSignerThumbprint `
    -OutputPath $OutputPath `
    -WhatIf 6>$null

# The core defines the hardened filesystem resolvers used below. Validate the two
# implementation files through the same ordinary/non-reparse policy before loading
# native code or treating the copied validation core as admitted release logic.
$validationCoreFile = Resolve-OrdinaryNonReparseFile -Path $validationCorePath -Label 'V25 update-manifest validation core'
$nativeHelperFile = Resolve-OrdinaryNonReparseFile -Path $nativeHelperPath -Label 'V25 update-manifest owned-publication helper'

if ($null -eq ('Qs3dV25UpdateManifestPublicationNative' -as [type])) {
    Add-Type -Path $nativeHelperFile.FullName
}

function Get-ByteArraySha256Hex {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '').ToUpperInvariant()
    }
    finally { $sha.Dispose() }
}

function Test-ExactByteArray {
    param([Parameter(Mandatory = $true)][byte[]]$Expected, [Parameter(Mandatory = $true)][byte[]]$Actual)
    if ($Expected.Length -ne $Actual.Length) { return $false }
    $difference = 0
    for ($index = 0; $index -lt $Expected.Length; $index++) {
        $difference = $difference -bor ($Expected[$index] -bxor $Actual[$index])
    }
    return $difference -eq 0
}

if (-not $wrapperCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')) {
    return
}

$utf8NoBom = [Text.UTF8Encoding]::new($false, $true)
$manifestJson = $manifest | ConvertTo-Json
$expectedManifestBytes = $utf8NoBom.GetBytes($manifestJson + [Environment]::NewLine)
if ($expectedManifestBytes.Length -gt $script:MaxMetadataBytes) {
    throw "Generated update manifest exceeds the $($script:MaxMetadataBytes)-byte safety limit."
}

$nonce = [Guid]::NewGuid().ToString('N')
$stagePath = Join-Path $outputParent.FullName (([IO.Path]::GetFileName($outputFull)) + ".tmp-$nonce")
$backupPath = Join-Path $outputParent.FullName (([IO.Path]::GetFileName($outputFull)) + ".bak-$nonce")
if (Test-Path -LiteralPath $stagePath) { throw "Refusing to reuse update-manifest staging path: $stagePath" }
if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse update-manifest backup path: $backupPath" }

$stageOwned = $null
$priorOwned = $null
$publicationCommitted = $false
try {
    $stageOwned = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedStaging($stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::WriteOwnedGeneration($stageOwned, $expectedManifestBytes)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stageOwned, $stagePath)
    $stageIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stageOwned)

    if ($hadExistingOutput) {
        $priorOwned = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedExisting($outputFull)
        [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($priorOwned, $outputFull)
        $priorBytes = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($priorOwned, [int]$script:MaxMetadataBytes)
        $priorHash = Get-ByteArraySha256Hex -Bytes $priorBytes
        if ([long]$priorBytes.Length -ne [long]$existingOutputState.Length -or
            -not [string]::Equals($priorHash, [string]$existingOutputState.Sha256, [StringComparison]::Ordinal)) {
            throw 'Existing update manifest changed before its exact generation could be acquired for publication.'
        }
    }
    elseif (Test-Path -LiteralPath $outputFull) {
        throw 'Update manifest destination appeared after admission; refusing to replace an unowned generation.'
    }

    [Qs3dV25UpdateManifestPublicationNative]::PublishOwnedGeneration($stageOwned, $outputFull, $priorOwned, $backupPath)

    if (-not [string]::Equals(
        $stageIdentity,
        [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stageOwned),
        [StringComparison]::Ordinal)) {
        throw 'Published update manifest generation identity changed across held-handle publication.'
    }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stageOwned, $outputFull)
    $publishedBytes = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($stageOwned, [int]$script:MaxMetadataBytes)
    if (-not (Test-ExactByteArray -Expected $expectedManifestBytes -Actual $publishedBytes)) {
        throw 'Published update manifest bytes differ from the exact generated bytes.'
    }

    try {
        $publishedText = [Text.UTF8Encoding]::new($false, $true).GetString($publishedBytes)
        $null = $publishedText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Published update manifest failed strict UTF-8/JSON verification through the owned generation: $($_.Exception.Message)"
    }

    if ($priorOwned) {
        [Qs3dV25UpdateManifestPublicationNative]::DeleteOwnedGeneration($priorOwned)
    }
    $publicationCommitted = $true
}
catch {
    $publishFailure = $_
    if ($stageOwned) {
        try {
            [Qs3dV25UpdateManifestPublicationNative]::RollbackOwnedGeneration(
                $stageOwned,
                $priorOwned,
                $outputFull,
                $stagePath)
            if ($priorOwned) {
                [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($priorOwned, $outputFull)
                $restoredBytes = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($priorOwned, [int]$script:MaxMetadataBytes)
                $restoredHash = Get-ByteArraySha256Hex -Bytes $restoredBytes
                if ([long]$restoredBytes.Length -ne [long]$existingOutputState.Length -or
                    -not [string]::Equals($restoredHash, [string]$existingOutputState.Sha256, [StringComparison]::Ordinal)) {
                    throw 'Rollback restored a generation whose exact bytes do not match the admitted prior manifest.'
                }
            }
        }
        catch {
            $priorHint = if ($priorOwned) { " Prior held generation path: $($priorOwned.CurrentPath)." } else { '' }
            throw "Update manifest publication failed and generation-owned rollback could not prove restoration: $($_.Exception.Message).$priorHint"
        }
    }
    throw $publishFailure
}
finally {
    if ($stageOwned) { $stageOwned.Dispose() }
    if ($priorOwned) { $priorOwned.Dispose() }
}

if (-not $publicationCommitted) {
    throw 'Update manifest publication did not reach a committed held-generation state.'
}

Write-Host "Update manifest: $outputFull"
Write-Host "Product version: $signedPluginProductVersion"
Write-Host "Assembly version: $($signedPluginVersion.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"
