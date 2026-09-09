[CmdletBinding()]
param(
    [string]$AssemblyPath,
    [string]$CoreAssemblyPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$validator = Join-Path $PSScriptRoot 'assert-v26-release-package-identity.ps1'
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($validator, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Production validator must parse before regression execution.' }
# Import the actual production functions without running its required-parameter entry point.
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) {
        Set-Item -Path ('Function:' + $statement.Name) -Value $statement.Body.GetScriptBlock()
    }
}
$script:AssemblyProbeProject = Join-Path $PSScriptRoot 'V26ReleaseIdentityProbe/V26ReleaseIdentityProbe.csproj'
$probe = Initialize-AssemblyVersionProbe
if ([string]::IsNullOrWhiteSpace($AssemblyPath)) { $AssemblyPath = $probe.Dll }
if ([string]::IsNullOrWhiteSpace($CoreAssemblyPath)) { $CoreAssemblyPath = $AssemblyPath }
$expectedVersion = [Reflection.AssemblyName]::GetAssemblyName($AssemblyPath).Version
if ([Reflection.AssemblyName]::GetAssemblyName($CoreAssemblyPath).Version -ne $expectedVersion) {
    throw 'Positive fixture assemblies must have the same version.'
}

foreach ($imagePath in @($AssemblyPath, $CoreAssemblyPath)) {
    $held = Open-LockedStableFile -Path $imagePath -Label 'real managed fixture'
    try {
        $beforeHash = $held.Sha256
        $versions = @(Get-HeldAssemblyVersion -Held $held -Probe $probe -Label 'real managed fixture')
        if ($versions.Count -ne 1 -or $versions[0] -isnot [Version] -or $versions[0] -ne $expectedVersion) {
            $types = ($versions | ForEach-Object { $_.GetType().FullName }) -join ', '
            throw "Expected exactly one System.Version $expectedVersion; got $($versions.Count) outputs: $types"
        }
        Assert-LockedPathBinding -Held $held -Label 'real managed fixture'
        if ($held.Stream.Position -ne 0 -or (Get-HeldStreamingSha256 -Stream $held.Stream -Label 'fixture') -ne $beforeHash) {
            throw 'Metadata inspection must reset and preserve the exact held image.'
        }
    }
    finally { $held.Stream.Dispose() }
}
Write-Output 'PASS: actual held-stream child returns exactly one System.Version and preserves held bytes'

$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-held-version-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot
try {
    $metadataPath = Join-Path $fixtureRoot 'PACKAGE-METADATA.json'
    $metadata = [ordered]@{
        product = 'QS3D'
        target = 'BricsCAD V26 x64'
        framework = 'net8.0-windows'
        productVersion = '0.0.0-held-version-test'
        version = $expectedVersion.ToString()
    }
    $releaseTag = 'v0.0.0-held-version-test'
    $metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8
    $receipt = @(& $validator -MetadataPath $metadataPath -PluginPath $AssemblyPath -CorePath $CoreAssemblyPath -ReleaseTag $releaseTag)
    if ($receipt.Count -ne 1 -or $receipt[0].AssemblyVersion -ne $expectedVersion.ToString()) {
        throw 'Full production validator must accept matching plugin/Core versions exactly once.'
    }
    Write-Output 'PASS: full package validator accepts matching plugin and Core versions'

    $metadata.version = '65534.65534.65534.65534'
    $metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8
    $rejected = $false
    try {
        & $validator -MetadataPath $metadataPath -PluginPath $AssemblyPath -CorePath $CoreAssemblyPath -ReleaseTag $releaseTag | Out-Null
    }
    catch {
        if ($_.Exception.Message -notlike '*managed assembly identity mismatch*') { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'Wrong package assembly version must be rejected.' }
    Write-Output 'PASS: full package validator rejects wrong assembly version'

    $metadata.version = $expectedVersion.ToString()
    $metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding utf8
    $otherAssembly = [object].Assembly.Location
    if ([Reflection.AssemblyName]::GetAssemblyName($otherAssembly).Version -eq $expectedVersion) {
        $otherAssembly = $probe.Dll
    }
    if ([Reflection.AssemblyName]::GetAssemblyName($otherAssembly).Version -eq $expectedVersion) {
        throw 'Independent mismatch fixture must have a different real assembly version.'
    }
    foreach ($slot in @('plugin', 'Core')) {
        $pluginFixture = $AssemblyPath
        $coreFixture = $CoreAssemblyPath
        if ($slot -eq 'plugin') { $pluginFixture = $otherAssembly } else { $coreFixture = $otherAssembly }
        $rejected = $false
        try {
            & $validator -MetadataPath $metadataPath -PluginPath $pluginFixture -CorePath $coreFixture -ReleaseTag $releaseTag | Out-Null
        }
        catch {
            if ($_.Exception.Message -notlike '*managed assembly identity mismatch*') { throw }
            $rejected = $true
        }
        if (-not $rejected) { throw "Independent $slot assembly version mismatch must be rejected." }
    }
    Write-Output 'PASS: full package validator rejects independent plugin and Core version mismatches'

    foreach ($case in @('malformed', 'empty')) {
        $invalidPath = Join-Path $fixtureRoot ($case + '.dll')
        $bytes = [byte[]]::new(0)
        if ($case -ne 'empty') { $bytes = [Text.Encoding]::UTF8.GetBytes('not a PE image') }
        [IO.File]::WriteAllBytes($invalidPath, $bytes)
        $held = Open-LockedStableFile -Path $invalidPath -Label $case
        try {
            $outputs = [Collections.Generic.List[object]]::new()
            $rejected = $false
            try {
                Get-HeldAssemblyVersion -Held $held -Probe $probe -Label $case | ForEach-Object { $outputs.Add($_) }
            }
            catch {
                if ($_.Exception.Message -notlike '*metadata probe failed with exit code 2*') { throw }
                $rejected = $true
            }
            if (-not $rejected -or $outputs.Count -ne 0 -or $held.Stream.Position -ne 0) {
                throw "$case image must fail without success output and reset its held stream."
            }
            Assert-LockedPathBinding -Held $held -Label $case
        }
        finally { $held.Stream.Dispose() }
    }
    Write-Output 'PASS: actual held-stream child rejects malformed and empty images without output'
}
finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixture.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedFixture) -notmatch '^qs3d-held-version-[0-9a-f]{32}$') {
        throw 'Refusing regression cleanup outside its exact owned temporary directory.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
