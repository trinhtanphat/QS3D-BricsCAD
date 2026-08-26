[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ExpectedPreviewTag = "v0.1.0-preview.10223"
$ExpectedSourceSha = "1363f9be69ebc8ca8a865ccdd41639346f55f6ee"
$ExpectedZipSha256 = "A83BC92A1F90B00ADF7DFE0B1C92DF2EF7A3286D7ED99E4307ED8E0B87F22222"
$ExpectedPluginSha256 = "3F0156A8DFD9BB31ECE43665D5D8334DA320172A6EAFB929967268218168F22F"
$MaxEvidenceBytes = 1048576
$Tolerance = 0.000001d
$StrictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)

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

function Require-False {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Context
    )

    $value = Require-Property -Object $Object -Name $Name -Context $Context
    if ($value -isnot [bool] -or $value) {
        throw "$Context.$Name must be the JSON boolean false."
    }
}

function Require-EqualString {
    param(
        [Parameter(Mandatory = $true)][string]$Actual,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not $Actual.Equals($Expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label mismatch. Expected '$Expected'; found '$Actual'."
    }
}

function Require-Pass {
    param(
        [Parameter(Mandatory = $true)]$Cell,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $status = [string](Require-Property -Object $Cell -Name "status" -Context "cells.$Name")
    if (-not $status.Equals("PASS", [StringComparison]::Ordinal)) {
        throw "cells.$Name.status must be PASS; found '$status'."
    }
}

function Require-Near {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][double]$Expected,
        [Parameter(Mandatory = $true)][string]$Context
    )

    $raw = Require-Property -Object $Object -Name $Name -Context $Context
    try {
        $actual = [Convert]::ToDouble($raw, [Globalization.CultureInfo]::InvariantCulture)
    }
    catch {
        throw "$Context.$Name must be numeric."
    }

    if ([double]::IsNaN($actual) -or [double]::IsInfinity($actual)) {
        throw "$Context.$Name must be a finite numeric value."
    }

    if ([Math]::Abs($actual - $Expected) -gt $Tolerance) {
        throw ("{0}.{1} expected {2:R} +/- {3:R}; found {4:R}." -f $Context, $Name, $Expected, $Tolerance, $actual)
    }
    return $actual
}

function Read-SafeEvidenceText {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "Beam matrix evidence file does not exist: $resolved"
    }

    $file = Get-Item -LiteralPath $resolved -Force
    if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Beam matrix evidence must be an ordinary non-reparse file: $resolved"
    }
    if ($file.Length -le 0 -or $file.Length -gt $MaxEvidenceBytes) {
        throw "Beam matrix evidence must be non-empty and no larger than 1 MiB."
    }

    $stream = $null
    try {
        $stream = New-Object IO.FileStream(
            $resolved,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        if ($stream.Length -le 0 -or $stream.Length -gt $MaxEvidenceBytes) {
            throw "Beam matrix evidence changed size while opening."
        }

        $buffer = New-Object byte[] ([int]$stream.Length)
        $offset = 0
        while ($offset -lt $buffer.Length) {
            $read = $stream.Read($buffer, $offset, $buffer.Length - $offset)
            if ($read -le 0) {
                throw "Beam matrix evidence ended before its validated length was read."
            }
            $offset += $read
        }
        if ($stream.ReadByte() -ne -1) {
            throw "Beam matrix evidence grew beyond the validated maximum while reading."
        }

        try {
            return $StrictUtf8.GetString($buffer)
        }
        catch {
            throw "Beam matrix evidence is not strict UTF-8: $($_.Exception.Message)"
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
    }
}

$raw = Read-SafeEvidenceText -Path $EvidencePath
if ($raw -match '(?im)([A-Z]:\\|\\\\[^\\\s]+\\|private[/\\]|customer[/\\])') {
    throw "Beam matrix evidence appears to contain a raw machine/private path. Keep only sanitized evidence."
}

try {
    $evidence = $raw | ConvertFrom-Json
}
catch {
    throw "Beam matrix evidence is not valid JSON: $($_.Exception.Message)"
}

$schema = [int](Require-Property -Object $evidence -Name "schema" -Context "evidence")
if ($schema -ne 1) {
    throw "Unsupported Beam matrix evidence schema '$schema'; expected schema 1."
}

Require-EqualString -Actual ([string](Require-Property -Object $evidence -Name "previewTag" -Context "evidence")) -Expected $ExpectedPreviewTag -Label "previewTag"
Require-EqualString -Actual ([string](Require-Property -Object $evidence -Name "sourceSha" -Context "evidence")) -Expected $ExpectedSourceSha -Label "sourceSha"
Require-EqualString -Actual ([string](Require-Property -Object $evidence -Name "zipSha256" -Context "evidence")) -Expected $ExpectedZipSha256 -Label "zipSha256"
Require-EqualString -Actual ([string](Require-Property -Object $evidence -Name "pluginSha256" -Context "evidence")) -Expected $ExpectedPluginSha256 -Label "pluginSha256"

$environment = Require-Property -Object $evidence -Name "environment" -Context "evidence"
Require-True -Object $environment -Name "windowsX64" -Context "environment"
Require-True -Object $environment -Name "interactive" -Context "environment"
Require-True -Object $environment -Name "licensedBricsCadV25" -Context "environment"
Require-EqualString -Actual ([string](Require-Property -Object $environment -Name "loadMode" -Context "environment")) -Expected "NETLOAD" -Label "environment.loadMode"
$productVersion = [string](Require-Property -Object $environment -Name "bricsCadProductVersion" -Context "environment")
if ($productVersion -notmatch '^25\.2\.10(?:\D|$)') {
    throw "environment.bricsCadProductVersion must identify the qualified BricsCAD V25.2.10 host."
}
Require-EqualString -Actual ([string](Require-Property -Object $environment -Name "loadedPluginSha256" -Context "environment")) -Expected $ExpectedPluginSha256 -Label "environment.loadedPluginSha256"

$attestation = Require-Property -Object $evidence -Name "attestation" -Context "evidence"
Require-True -Object $attestation -Name "executedOnLicensedV25" -Context "attestation"
Require-True -Object $attestation -Name "sameExactReleasedPlugin" -Context "attestation"
Require-True -Object $attestation -Name "matrixActuallyExercised" -Context "attestation"
Require-True -Object $attestation -Name "sanitized" -Context "attestation"

$beam = Require-Property -Object $evidence -Name "beam" -Context "evidence"
$null = Require-Near -Object $beam -Name "widthM" -Expected 0.30d -Context "beam"
$null = Require-Near -Object $beam -Name "heightM" -Expected 0.50d -Context "beam"
$null = Require-Near -Object $beam -Name "deltaXM" -Expected 5.0d -Context "beam"
$null = Require-Near -Object $beam -Name "deltaYM" -Expected 5.0d -Context "beam"
$null = Require-Near -Object $beam -Name "lengthM" -Expected 7.0710678d -Context "beam"

$cells = Require-Property -Object $evidence -Name "cells" -Context "evidence"

$m1 = Require-Property -Object $cells -Name "M1" -Context "cells"
Require-Pass -Cell $m1 -Name "M1"
Require-True -Object $m1 -Name "sideEnabled" -Context "cells.M1"
Require-False -Object $m1 -Name "bottomEnabled" -Context "cells.M1"
$null = Require-Near -Object $m1 -Name "grossM2" -Expected 7.0710678d -Context "cells.M1"

$m2 = Require-Property -Object $cells -Name "M2" -Context "cells"
Require-Pass -Cell $m2 -Name "M2"
Require-True -Object $m2 -Name "sideEnabled" -Context "cells.M2"
Require-True -Object $m2 -Name "bottomEnabled" -Context "cells.M2"
$null = Require-Near -Object $m2 -Name "grossM2" -Expected 9.1923881d -Context "cells.M2"

$m3 = Require-Property -Object $cells -Name "M3" -Context "cells"
Require-Pass -Cell $m3 -Name "M3"
$null = Require-Near -Object $m3 -Name "topContributionM2" -Expected 0.0d -Context "cells.M3"

$m4 = Require-Property -Object $cells -Name "M4" -Context "cells"
Require-Pass -Cell $m4 -Name "M4"
$null = Require-Near -Object $m4 -Name "endContributionM2" -Expected 0.0d -Context "cells.M4"
$null = Require-Near -Object $m4 -Name "otherContributionM2" -Expected 0.0d -Context "cells.M4"

$m5 = Require-Property -Object $cells -Name "M5" -Context "cells"
Require-Pass -Cell $m5 -Name "M5"
$null = Require-Near -Object $m5 -Name "sideDeductionM2" -Expected 0.30d -Context "cells.M5"
$null = Require-Near -Object $m5 -Name "bottomDeductionM2" -Expected 0.0d -Context "cells.M5"
$null = Require-Near -Object $m5 -Name "netM2" -Expected 6.7710678d -Context "cells.M5"

$m6 = Require-Property -Object $cells -Name "M6" -Context "cells"
Require-Pass -Cell $m6 -Name "M6"
$null = Require-Near -Object $m6 -Name "sideDeductionM2" -Expected 0.30d -Context "cells.M6"
$null = Require-Near -Object $m6 -Name "bottomDeductionM2" -Expected 0.09d -Context "cells.M6"
$null = Require-Near -Object $m6 -Name "netM2" -Expected 8.8023881d -Context "cells.M6"

$m7 = Require-Property -Object $cells -Name "M7" -Context "cells"
Require-Pass -Cell $m7 -Name "M7"
$aggregate = Require-Near -Object $m7 -Name "aggregateFormworkM2" -Expected 8.8023881d -Context "cells.M7"
$detail = Require-Near -Object $m7 -Name "detailNetFormworkM2" -Expected 8.8023881d -Context "cells.M7"
if ([Math]::Abs($aggregate - $detail) -gt $Tolerance) {
    throw "cells.M7 aggregate/detail parity mismatch."
}

$m8 = Require-Property -Object $cells -Name "M8" -Context "cells"
Require-Pass -Cell $m8 -Name "M8"
Require-True -Object $m8 -Name "diagonalAxisResolved" -Context "cells.M8"
Require-False -Object $m8 -Name "endCapsClassifiedAsSide" -Context "cells.M8"
Require-True -Object $m8 -Name "horizontalClassificationUsesLiveZBounds" -Context "cells.M8"

$cleanup = Require-Property -Object $evidence -Name "cleanup" -Context "evidence"
Require-True -Object $cleanup -Name "profileRestored" -Context "cleanup"
Require-True -Object $cleanup -Name "loaderRestored" -Context "cleanup"
Require-True -Object $cleanup -Name "demandLoadRestored" -Context "cleanup"
Require-True -Object $cleanup -Name "zeroTestOwnedProcesses" -Context "cleanup"
Require-True -Object $cleanup -Name "noScopedResidue" -Context "cleanup"

$blockers = Require-Property -Object $evidence -Name "knownBlockers" -Context "evidence"
if ($blockers -isnot [System.Array]) {
    throw "knownBlockers must be a JSON array."
}
if ($blockers.Count -ne 0) {
    throw "Beam behavior matrix cannot PASS while knownBlockers contains $($blockers.Count) item(s)."
}

Write-Host ("LOCAL_PASS / BEAM_BEHAVIOR_MATRIX: preview={0}, source={1}, pluginSha256={2}, BricsCAD={3}, cells=8" -f `
    $ExpectedPreviewTag,
    $ExpectedSourceSha,
    $ExpectedPluginSha256,
    $productVersion)