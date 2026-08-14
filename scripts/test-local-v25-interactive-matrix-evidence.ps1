[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSha,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{64}$')]
    [string]$ExpectedPluginSha256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredScenarios = @(
    "pluginShellUi",
    "demandLoad",
    "directDraw",
    "build3dGeneratedOwnershipHealth",
    "doorOpening",
    "roomHtPhong",
    "curtain",
    "rebar",
    "projectSaveReopenMultiDwg",
    "modelessMultiDwgLifecycle",
    "modelessEditorRollbackPostCommit",
    "reportingBqBbsExcel",
    "unicodeHiDpi",
    "cleanInstallUpgradeUninstall",
    "privateDwgRegression"
)

function Require-Property {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Context
    )
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        throw "$Context is missing required property '$Name'."
    }
    return $property.Value
}

function Require-True {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Context
    )
    $value = Require-Property -Object $Object -Name $Name -Context $Context
    if ($value -isnot [bool] -or -not $value) {
        throw "$Context.$Name must be the JSON boolean true."
    }
}

$resolved = [IO.Path]::GetFullPath($EvidencePath)
if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
    throw "Interactive matrix evidence file does not exist: $resolved"
}

$file = Get-Item -LiteralPath $resolved
if ($file.Length -le 0 -or $file.Length -gt 1048576) {
    throw "Interactive matrix evidence must be a non-empty JSON file no larger than 1 MiB."
}

$raw = Get-Content -LiteralPath $resolved -Raw -Encoding UTF8
if ($raw -match '(?im)([A-Z]:\\|\\\\[^\\\s]+\\|private[/\\]|customer[/\\])') {
    throw "Interactive matrix evidence appears to contain a raw machine/private path. Keep only sanitized evidence."
}

try {
    $evidence = $raw | ConvertFrom-Json
}
catch {
    throw "Interactive matrix evidence is not valid JSON: $($_.Exception.Message)"
}

$schema = Require-Property -Object $evidence -Name "schema" -Context "evidence"
if ([int]$schema -ne 1) {
    throw "Unsupported interactive matrix evidence schema '$schema'; expected schema 1."
}

$exactSha = [string](Require-Property -Object $evidence -Name "exactSha" -Context "evidence")
if ($exactSha -notmatch '^[0-9A-Fa-f]{40}$' -or -not $exactSha.Equals($ExpectedSha, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Evidence exactSha does not match the candidate Git SHA."
}

$pluginSha = [string](Require-Property -Object $evidence -Name "pluginSha256" -Context "evidence")
if ($pluginSha -notmatch '^[0-9A-Fa-f]{64}$' -or -not $pluginSha.Equals($ExpectedPluginSha256, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Evidence pluginSha256 does not match the exact built V25 plugin DLL."
}

$environment = Require-Property -Object $evidence -Name "environment" -Context "evidence"
Require-True -Object $environment -Name "windowsX64" -Context "environment"
Require-True -Object $environment -Name "interactive" -Context "environment"
Require-True -Object $environment -Name "licensedBricsCadV25" -Context "environment"
$productVersion = [string](Require-Property -Object $environment -Name "bricsCadProductVersion" -Context "environment")
if ($productVersion -notmatch '^25(?:\.|$)') {
    throw "environment.bricsCadProductVersion must identify BricsCAD V25."
}

$attestation = Require-Property -Object $evidence -Name "attestation" -Context "evidence"
Require-True -Object $attestation -Name "executedOnLicensedV25" -Context "attestation"
Require-True -Object $attestation -Name "sameExactShaAndPlugin" -Context "attestation"
Require-True -Object $attestation -Name "sanitized" -Context "attestation"

$scenarios = Require-Property -Object $evidence -Name "scenarios" -Context "evidence"
foreach ($name in $requiredScenarios) {
    $status = [string](Require-Property -Object $scenarios -Name $name -Context "scenarios")
    if (-not $status.Equals("PASS", [StringComparison]::Ordinal)) {
        throw "Required scenario '$name' must be PASS; found '$status'."
    }
}

$blockersProperty = $evidence.PSObject.Properties["knownBlockers"]
if ($null -eq $blockersProperty -or $null -eq $blockersProperty.Value) {
    throw "knownBlockers must be an empty JSON array for full qualification."
}
if ($blockersProperty.Value -isnot [System.Array]) {
    throw "knownBlockers must be a JSON array."
}
$blockerCount = $blockersProperty.Value.Count
if ($blockerCount -ne 0) {
    throw "Full licensed V25 qualification cannot pass while knownBlockers contains $blockerCount item(s)."
}

Write-Host ("LICENSED V25 INTERACTIVE MATRIX EVIDENCE PASS: exactSha={0}, pluginSha256={1}, BricsCAD={2}, scenarios={3}" -f $exactSha.ToLowerInvariant(), $pluginSha.ToLowerInvariant(), $productVersion, $requiredScenarios.Count)
