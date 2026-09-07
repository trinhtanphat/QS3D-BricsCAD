[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$SourceCommit,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$PluginPath,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$CorePath,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$HostReferenceStatePath,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$OutputPath,
    [string]$RuntimeArtifactDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-CanonicalCommit([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -ne $Value.Trim() -or $Value -cnotmatch '^[0-9a-f]{40}$') {
        throw 'SourceCommit must be exactly 40 lowercase hexadecimal characters with no surrounding whitespace.'
    }
}

function Get-OrdinaryFile([string]$Path, [string]$Label) {
    if (-not [IO.Path]::IsPathRooted($Path)) { throw "$Label path must be absolute." }
    $full = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "$Label is missing: $full" }
    $item = Get-Item -LiteralPath $full -Force
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file."
    }
    return $item
}

function Get-FileRecord([IO.FileInfo]$File, [string]$Name) {
    $hash = (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    return [ordered]@{ Name = $Name; Length = [long]$File.Length; Sha256 = $hash }
}

function Read-StrictUtf8Json([string]$Path, [string]$Label) {
    $file = Get-OrdinaryFile -Path $Path -Label $Label
    if ($file.Length -gt 65536) { throw "$Label exceeds 65536 bytes." }
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    $encoding = [Text.UTF8Encoding]::new($false, $true)
    try { $text = $encoding.GetString($bytes) } catch { throw "$Label is not strict UTF-8." }
    try { return $text | ConvertFrom-Json } catch { throw "$Label is not valid JSON: $($_.Exception.Message)" }
}

Assert-CanonicalCommit -Value $SourceCommit
$plugin = Get-OrdinaryFile -Path $PluginPath -Label 'V26 plugin'
$core = Get-OrdinaryFile -Path $CorePath -Label 'QS3D.Core payload'
$state = Read-StrictUtf8Json -Path $HostReferenceStatePath -Label 'V26 host-reference state'
if ([int]$state.Version -ne 1) { throw 'V26 host-reference state version must be 1.' }
$hostFiles = @($state.Files)
$requiredHostNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
if ($hostFiles.Count -ne $requiredHostNames.Count) { throw 'V26 host-reference state must contain exactly four required files.' }
$hostRecords = foreach ($name in $requiredHostNames) {
    $matches = @($hostFiles | Where-Object { [string]::Equals([string]$_.Name, $name, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "V26 host-reference state must contain exactly one $name record." }
    $record = $matches[0]
    $sha = [string]$record.Sha256
    if ($sha -cnotmatch '^[0-9a-f]{64}$' -or [long]$record.Length -le 0) { throw "Invalid V26 host-reference identity for $name." }
    [ordered]@{ Name = $name; Length = [long]$record.Length; Sha256 = $sha }
}

$runtimeState = 'absent'
if (-not [string]::IsNullOrWhiteSpace($RuntimeArtifactDir)) {
    $runtimeFull = [IO.Path]::GetFullPath($RuntimeArtifactDir)
    if (Test-Path -LiteralPath $runtimeFull -PathType Container) {
        $runtimeFiles = @(Get-ChildItem -LiteralPath $runtimeFull -File -Recurse -Force)
        if ($runtimeFiles.Count -gt 0) { $runtimeState = 'present' }
    }
}

$manifest = [ordered]@{
    Version = 1
    SourceCommit = $SourceCommit
    EvidenceClass = 'V26_SOURCE_BUILD_QUALIFICATION'
    RuntimeEvidence = $runtimeState
    Payload = @(
        (Get-FileRecord -File $plugin -Name 'QS3D.BricsCAD.V26.dll'),
        (Get-FileRecord -File $core -Name 'QS3D.Core.dll')
    )
    HostReferences = @($hostRecords)
}

$outputFull = [IO.Path]::GetFullPath($OutputPath)
$parent = Split-Path -Parent $outputFull
if ([string]::IsNullOrWhiteSpace($parent) -or -not (Test-Path -LiteralPath $parent -PathType Container)) {
    throw "Manifest parent directory does not exist: $parent"
}
if (Test-Path -LiteralPath $outputFull) {
    $existing = Get-Item -LiteralPath $outputFull -Force
    if ($existing.PSIsContainer -or ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Manifest output must be an ordinary file path.' }
}
$json = $manifest | ConvertTo-Json -Depth 6 -Compress
[IO.File]::WriteAllText($outputFull, $json + "`n", [Text.UTF8Encoding]::new($false, $true))
Write-Output $outputFull
