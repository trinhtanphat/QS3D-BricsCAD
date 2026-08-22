param(
    [string]$ExpectedHead = "",
    [int]$Elements = 10000,
    [int]$Targets = 256,
    [int]$Iterations = 7,
    [int]$Warmups = 2,
    [string]$Scenario = "all"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $repoRoot
try {
    $head = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
        throw "Unable to resolve repository HEAD."
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedHead) -and
        -not $head.Equals($ExpectedHead.Trim(), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Exact-SHA performance gate failed. Expected $ExpectedHead but checkout is $head."
    }

    if ($Elements -le 0 -or $Elements -gt 250000) { throw "Elements must be between 1 and 250000." }
    if ($Targets -le 0) { throw "Targets must be positive." }
    if ($Iterations -le 0) { throw "Iterations must be positive." }
    if ($Warmups -lt 0) { throw "Warmups cannot be negative." }

    $shortSha = $head.Substring(0, [Math]::Min(12, $head.Length))
    $outputDir = Join-Path $repoRoot ("artifacts\perf\" + $shortSha)
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    $jsonPath = Join-Path $outputDir "core-perf.json"

    & dotnet run --configuration Release --project "tests/QS3D.Core.PerfHarness/QS3D.Core.PerfHarness.csproj" -- `
        --elements $Elements `
        --targets $Targets `
        --iterations $Iterations `
        --warmups $Warmups `
        --scenario $Scenario `
        --revision $head `
        --json $jsonPath
    if ($LASTEXITCODE -ne 0) {
        throw "Core performance harness failed with exit code $LASTEXITCODE."
    }

    Write-Host "Core performance evidence: $jsonPath"
    Write-Host "This is a measurement artifact, not a BricsCAD V25 runtime qualification."
}
finally {
    Pop-Location
}
