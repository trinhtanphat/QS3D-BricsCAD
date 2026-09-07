[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$ManifestPath,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$PluginPath,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$CorePath,
    [string]$ExpectedSourceCommit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-OrdinaryFile([string]$Path, [string]$Label) {
    if (-not [IO.Path]::IsPathRooted($Path)) { throw "$Label path must be absolute." }
    $full = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "$Label is missing: $full" }
    $item = Get-Item -LiteralPath $full -Force
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse file." }
    return $item
}

function Read-StrictUtf8([IO.FileInfo]$File, [string]$Label) {
    if ($File.Length -gt 65536) { throw "$Label exceeds 65536 bytes." }
    $bytes = [IO.File]::ReadAllBytes($File.FullName)
    try { return [Text.UTF8Encoding]::new($false, $true).GetString($bytes) } catch { throw "$Label is not strict UTF-8." }
}

function Assert-CanonicalSha([string]$Value, [string]$Label, [int]$Length) {
    $pattern = if ($Length -eq 40) { '^[0-9a-f]{40}$' } else { '^[0-9a-f]{64}$' }
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -ne $Value.Trim() -or $Value -cnotmatch $pattern) { throw "$Label is not canonical lowercase hexadecimal identity." }
}

function Assert-UniqueNames($Records, [string[]]$ExpectedNames, [string]$Label) {
    $items = @($Records)
    if ($items.Count -ne $ExpectedNames.Count) { throw "$Label record count is invalid." }
    foreach ($name in $ExpectedNames) {
        $matches = @($items | Where-Object { [string]::Equals([string]$_.Name, $name, [StringComparison]::Ordinal) })
        if ($matches.Count -ne 1) { throw "$Label must contain exactly one $name record." }
    }
}

$manifestFile = Get-OrdinaryFile -Path $ManifestPath -Label 'V26 qualification manifest'
$text = Read-StrictUtf8 -File $manifestFile -Label 'V26 qualification manifest'
try { $manifest = $text | ConvertFrom-Json } catch { throw "V26 qualification manifest is not valid JSON: $($_.Exception.Message)" }
if ([int]$manifest.Version -ne 1) { throw 'V26 qualification manifest Version must be 1.' }
if (-not [string]::Equals([string]$manifest.EvidenceClass, 'V26_SOURCE_BUILD_QUALIFICATION', [StringComparison]::Ordinal)) { throw 'Unexpected V26 qualification EvidenceClass.' }
$sourceCommit = [string]$manifest.SourceCommit
Assert-CanonicalSha -Value $sourceCommit -Label 'SourceCommit' -Length 40
if (-not [string]::IsNullOrWhiteSpace($ExpectedSourceCommit)) {
    Assert-CanonicalSha -Value $ExpectedSourceCommit -Label 'ExpectedSourceCommit' -Length 40
    if (-not [string]::Equals($sourceCommit, $ExpectedSourceCommit, [StringComparison]::Ordinal)) { throw 'V26 qualification SourceCommit does not match expected source.' }
}
$runtimeEvidence = [string]$manifest.RuntimeEvidence
if ($runtimeEvidence -notin @('absent', 'present')) { throw 'RuntimeEvidence must be exactly absent or present.' }

$payloadNames = @('QS3D.BricsCAD.V26.dll', 'QS3D.Core.dll')
Assert-UniqueNames -Records $manifest.Payload -ExpectedNames $payloadNames -Label 'Payload'
$payloadPaths = @{
    'QS3D.BricsCAD.V26.dll' = (Get-OrdinaryFile -Path $PluginPath -Label 'V26 plugin')
    'QS3D.Core.dll' = (Get-OrdinaryFile -Path $CorePath -Label 'QS3D.Core payload')
}
foreach ($record in @($manifest.Payload)) {
    $name = [string]$record.Name
    $file = $payloadPaths[$name]
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-CanonicalSha -Value ([string]$record.Sha256) -Label "$name Sha256" -Length 64
    if ([long]$record.Length -ne [long]$file.Length -or -not [string]::Equals([string]$record.Sha256, $hash, [StringComparison]::Ordinal)) { throw "$name does not match the manifest payload identity." }
}

$hostNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
Assert-UniqueNames -Records $manifest.HostReferences -ExpectedNames $hostNames -Label 'HostReferences'
foreach ($record in @($manifest.HostReferences)) {
    if ([long]$record.Length -le 0) { throw "Host-reference length must be positive for $($record.Name)." }
    Assert-CanonicalSha -Value ([string]$record.Sha256) -Label "$($record.Name) Sha256" -Length 64
}
Write-Host "Validated V26 qualification artifact manifest for $sourceCommit (runtime=$runtimeEvidence)."
