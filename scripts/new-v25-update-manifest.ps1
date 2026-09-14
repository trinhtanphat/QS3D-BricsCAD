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

if ($null -eq ('Qs3dV25UpdateManifestPublicationNative' -as [type])) {
    Add-Type -Path $nativeHelperPath
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

# Acquire the destination parent and exact prior destination generation before the
# long package/signature validation phase. The parent handle denies delete sharing,
# so the directory itself cannot be renamed/replaced while it is publication
# authority. The prior file handle denies delete sharing, so a byte-identical
# delete/recreate cannot silently become a new admitted generation.
$preOutputFull = [IO.Path]::GetFullPath($OutputPath)
$preOutputParentPath = [IO.Path]::GetDirectoryName($preOutputFull)
if ([string]::IsNullOrWhiteSpace($preOutputParentPath)) {
    throw "Update manifest destination has no parent directory: $preOutputFull"
}
$destinationParentOwned = $null
$priorOwned = $null
$priorIdentity = $null
$priorBytesBeforeValidation = $null
$stageOwned = $null
$publicationCommitted = $false

try {
    $destinationParentOwned = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedDirectory($preOutputParentPath)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($destinationParentOwned, $preOutputParentPath)
    $destinationParentIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($destinationParentOwned)

    try {
        $priorOwned = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedExisting($preOutputFull)
    }
    catch [ComponentModel.Win32Exception] {
        if ($_.Exception.NativeErrorCode -notin @(2, 3)) { throw }
        $priorOwned = $null
    }

    if ($priorOwned) {
        [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($priorOwned, $preOutputFull)
        $priorIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($priorOwned)
        $priorBytesBeforeValidation = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($priorOwned, 65536)
    }

    # Preserve the previously-reviewed package/signature/version admission logic.
    # -WhatIf is mandatory: the copied validation core's legacy pathname publisher
    # must never become write authority.
    . $validationCorePath `
        -PackageDirectory $PackageDirectory `
        -PackageZip $PackageZip `
        -PackageUri $PackageUri `
        -ExpectedSignerThumbprint $ExpectedSignerThumbprint `
        -OutputPath $OutputPath `
        -WhatIf 6>$null

    $validationCoreFile = Resolve-OrdinaryNonReparseFile -Path $validationCorePath -Label 'V25 update-manifest validation core'
    $nativeHelperFile = Resolve-OrdinaryNonReparseFile -Path $nativeHelperPath -Label 'V25 update-manifest owned-publication helper'

    if (-not [string]::Equals([IO.Path]::GetFullPath($outputFull), $preOutputFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Validation resolved a different update-manifest destination than the pre-acquired publication authority.'
    }
    if (-not [string]::Equals(
        $destinationParentIdentity,
        [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($destinationParentOwned),
        [StringComparison]::Ordinal)) {
        throw 'Destination-parent generation identity changed during update-manifest admission.'
    }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($destinationParentOwned, $preOutputParentPath)

    $validationSawExisting = [bool]$hadExistingOutput
    if ($validationSawExisting -ne ($null -ne $priorOwned)) {
        throw 'Update manifest destination existence changed between generation acquisition and validation.'
    }
    if ($priorOwned) {
        if (-not [string]::Equals(
            $priorIdentity,
            [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($priorOwned),
            [StringComparison]::Ordinal)) {
            throw 'Existing update-manifest generation identity changed during admission.'
        }
        $priorBytesAfterValidation = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($priorOwned, [int]$script:MaxMetadataBytes)
        if (-not (Test-ExactByteArray -Expected $priorBytesBeforeValidation -Actual $priorBytesAfterValidation)) {
            throw 'Existing update-manifest bytes changed while its exact generation was held for admission.'
        }
        $priorHash = Get-ByteArraySha256Hex -Bytes $priorBytesAfterValidation
        if ([long]$priorBytesAfterValidation.Length -ne [long]$existingOutputState.Length -or
            -not [string]::Equals($priorHash, [string]$existingOutputState.Sha256, [StringComparison]::Ordinal)) {
            throw 'Validation did not agree with the already-owned prior update-manifest generation.'
        }
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
    $outputFileName = [IO.Path]::GetFileName($outputFull)
    $stageFileName = $outputFileName + ".tmp-$nonce"
    $backupFileName = $outputFileName + ".bak-$nonce"
    $stagePath = Join-Path $preOutputParentPath $stageFileName
    $backupPath = Join-Path $preOutputParentPath $backupFileName
    if (Test-Path -LiteralPath $stagePath) { throw "Refusing to reuse update-manifest staging path: $stagePath" }
    if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse update-manifest backup path: $backupPath" }

    # Reassert parent identity/path immediately before staging creation. Any ancestor
    # path swap that redirected stage creation is also detected by the stage handle's
    # final-path assertion before publication.
    if (-not [string]::Equals(
        $destinationParentIdentity,
        [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($destinationParentOwned),
        [StringComparison]::Ordinal)) {
        throw 'Destination-parent identity changed before staging creation.'
    }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($destinationParentOwned, $preOutputParentPath)

    $stageOwned = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedStaging($stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::WriteOwnedGeneration($stageOwned, $expectedManifestBytes)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stageOwned, $stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($destinationParentOwned, $preOutputParentPath)
    $stageIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stageOwned)

    if ($priorOwned) {
        if (-not [string]::Equals(
            $priorIdentity,
            [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($priorOwned),
            [StringComparison]::Ordinal)) {
            throw 'Prior update-manifest generation identity changed before publication.'
        }
        [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($priorOwned, $outputFull)
    }
    elseif (Test-Path -LiteralPath $outputFull) {
        throw 'Update manifest destination appeared after admission; refusing to replace an unowned generation.'
    }

    [Qs3dV25UpdateManifestPublicationNative]::PublishOwnedGenerationInDirectory(
        $stageOwned,
        $destinationParentOwned,
        $outputFileName,
        $priorOwned,
        $backupFileName)

    if (-not [string]::Equals(
        $stageIdentity,
        [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stageOwned),
        [StringComparison]::Ordinal)) {
        throw 'Published update manifest generation identity changed across held-handle publication.'
    }
    if (-not [string]::Equals(
        $destinationParentIdentity,
        [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($destinationParentOwned),
        [StringComparison]::Ordinal)) {
        throw 'Destination-parent identity changed across publication.'
    }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($destinationParentOwned, $preOutputParentPath)
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
    if ($stageOwned -and $destinationParentOwned) {
        try {
            [Qs3dV25UpdateManifestPublicationNative]::RollbackOwnedGenerationInDirectory(
                $stageOwned,
                $destinationParentOwned,
                $priorOwned,
                [IO.Path]::GetFileName($preOutputFull),
                $stagePath)
            if ($priorOwned) {
                if (-not [string]::Equals(
                    $priorIdentity,
                    [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($priorOwned),
                    [StringComparison]::Ordinal)) {
                    throw 'Rollback prior generation identity differs from the pre-validation authority.'
                }
                [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($priorOwned, $preOutputFull)
                $restoredBytes = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($priorOwned, 65536)
                if (-not (Test-ExactByteArray -Expected $priorBytesBeforeValidation -Actual $restoredBytes)) {
                    throw 'Rollback did not restore the exact pre-validation prior generation bytes.'
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
    if ($destinationParentOwned) { $destinationParentOwned.Dispose() }
}

if (-not $publicationCommitted) {
    throw 'Update manifest publication did not reach a committed held-generation state.'
}

Write-Host "Update manifest: $outputFull"
Write-Host "Product version: $signedPluginProductVersion"
Write-Host "Assembly version: $($signedPluginVersion.ToString())"
Write-Host "Package SHA256: $zipHash"
Write-Host "Signer: $expectedSigner"
