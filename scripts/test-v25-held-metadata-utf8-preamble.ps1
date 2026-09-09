[CmdletBinding()]
param([string]$VerificationMetadata, [string]$VerificationSource, [string]$VerificationTag)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$validator = Join-Path $PSScriptRoot 'assert-v25-release-package-identity.ps1'
if ($VerificationMetadata) {
    & $validator -MetadataPath $VerificationMetadata -ExpectedSourceCommit $VerificationSource -ExpectedReleaseTag $VerificationTag | ConvertTo-Json -Compress
    return
}
$parseErrors = $null
$tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($validator, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Production validator has syntax errors.' }
foreach ($name in @('Read-HeldStrictUtf8Metadata', 'Assert-UniqueTopLevelJsonPropertyNames')) {
    $definitions = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true))
    if ($definitions.Count -ne 1) { throw "Expected one actual production function: $name" }
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
$script:MaxMetadataBytes = 65536
$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$bom = [byte[]]@(0xef, 0xbb, 0xbf)
$source = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$json = '{"product":"QS3D","target":"BricsCAD V25 x64","gitCommit":"' + $source + '","productVersion":"0.2.0-preview.1","version":"0.2.0.0"}'
$plainBytes = [Text.Encoding]::UTF8.GetBytes($json)
$bomBytes = [byte[]]($bom + $plainBytes)

function Assert-Rejected {
    param([scriptblock]$Action, [string]$Expected, [string]$Label)
    try { & $Action | Out-Null }
    catch {
        if ($_.Exception.Message -notmatch $Expected) { throw "$Label failed for an unexpected reason: $($_.Exception.Message)" }
        return
    }
    throw "$Label was unexpectedly admitted."
}

function Read-TestMetadata {
    param([byte[]]$Bytes)
    $stream = [IO.MemoryStream]::new($Bytes, $false)
    try {
        $decoded = Read-HeldStrictUtf8Metadata -Held ([pscustomobject]@{ Stream = $stream })
        if ($stream.Position -ne 0) { throw 'Held reader did not restore stream position.' }
        Assert-UniqueTopLevelJsonPropertyNames -Text $decoded
        $null = $decoded | ConvertFrom-Json -ErrorAction Stop
        return $decoded
    }
    finally { $stream.Dispose() }
}

function Get-FixtureHash {
    param([string]$Path)
    $stream = [IO.File]::OpenRead($Path)
    $hash = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hash.ComputeHash($stream)) }
    finally { $hash.Dispose(); $stream.Dispose() }
}

function Invoke-FullVerifier {
    param([string]$Metadata, [string]$Source, [string]$Tag = 'v0.2.0-preview.1')
    # Each public script invocation gets a fresh .NET Framework reflection-only
    # load context; do not reuse synthetic assembly identities across cases.
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $start.Arguments = '-NoLogo -NoProfile -NonInteractive -File "' + $PSCommandPath + '" -VerificationMetadata "' + $Metadata + '" -VerificationSource "' + $Source + '" -VerificationTag "' + $Tag + '"'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    try {
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); throw 'Full verifier child timed out.' }
        $output = $outputTask.GetAwaiter().GetResult()
        $errors = $errorTask.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw ($errors + $output) }
        return $output | ConvertFrom-Json -ErrorAction Stop
    }
    finally { $process.Dispose() }
}

if ((Read-TestMetadata $plainBytes) -cne $json) { throw 'Plain strict UTF-8 changed.' }
if ((Read-TestMetadata $bomBytes) -cne $json) { throw 'One leading UTF-8 preamble was not consumed exactly.' }
$embedded = '{"note":"' + [char]0xfeff + '"}'
if ((Read-TestMetadata ([Text.Encoding]::UTF8.GetBytes($embedded))) -cne $embedded) { throw 'A JSON string U+FEFF was changed.' }
Assert-Rejected { Read-TestMetadata ([byte[]]($bom + $bomBytes)) } 'root must be one JSON object' 'Repeated preamble'
Assert-Rejected { Read-TestMetadata ([byte[]](@(0x20) + $bomBytes)) } 'root must be one JSON object' 'Misplaced preamble'
Assert-Rejected { Read-TestMetadata ([byte[]]($bom + @(0xc3, 0x28))) } 'not strict UTF-8' 'Malformed UTF-8 after preamble'
Assert-Rejected { Read-TestMetadata ([byte[]]@(0xff, 0xfe, 0x7b, 0x00)) } 'not strict UTF-8' 'UTF-16 is not UTF-8'
Assert-Rejected { Read-TestMetadata ([byte[]]@(0xef, 0xbb)) } 'not strict UTF-8' 'Truncated preamble'
Assert-Rejected { Read-TestMetadata $bom } 'root must be one JSON object' 'Preamble-only metadata'
$atLimit = [byte[]]($bomBytes + [Text.Encoding]::UTF8.GetBytes(' ' * (65536 - $bomBytes.Length)))
if ((Read-TestMetadata $atLimit).TrimEnd() -cne $json) { throw 'Exact raw byte limit was rejected.' }
Assert-Rejected { Read-TestMetadata ([byte[]]($atLimit + @(0x20))) } '65536-byte safety limit' 'BOM-inclusive byte budget'
$duplicate = $json.Replace('"product":"QS3D"', '"product":"QS3D","Product":"QS3D"')
Assert-Rejected { Read-TestMetadata ([byte[]]($bom + [Text.Encoding]::UTF8.GetBytes($duplicate))) } 'Duplicate top-level JSON property name' 'BOM case-insensitive duplicate key'
$escaped = $json.Replace('"product":"QS3D"', '"product":"QS3D","produ\u0063t":"QS3D"')
Assert-Rejected { Read-TestMetadata ([byte[]]($bom + [Text.Encoding]::UTF8.GetBytes($escaped))) } 'Duplicate top-level JSON property name' 'BOM escaped duplicate key'

# The V25 verifier's held assembly inspection uses .NET Framework ReflectionOnlyLoad.
# Execute the entire production script on its supported Windows PowerShell host.
if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Full V25 verifier regression requires Windows PowerShell 5.1.' }
$directory = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-metadata-preamble-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $directory
try {
    $plugin = Join-Path $directory 'QS3D.BricsCAD.V25.dll'
    $core = Join-Path $directory 'QS3D.Core.dll'
    $metadata = Join-Path $directory 'PACKAGE-METADATA.json'
    Add-Type -TypeDefinition @'
using System.Reflection;
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyInformationalVersion("0.2.0-preview.1")]
public sealed class SyntheticMetadataPreambleIdentity { }
'@ -OutputAssembly $plugin -ErrorAction Stop
    Add-Type -TypeDefinition @'
using System.Reflection;
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyInformationalVersion("0.2.0-preview.1")]
public sealed class SyntheticCoreMetadataPreambleIdentity { }
'@ -OutputAssembly $core -ErrorAction Stop
    foreach ($bytes in @($plainBytes, $bomBytes)) {
        [IO.File]::WriteAllBytes($metadata, $bytes)
        $hashBefore = Get-FixtureHash $metadata
        $result = Invoke-FullVerifier $metadata $source
        if ($result.SourceCommit -cne $source -or $result.MetadataBytes -ne $bytes.Length -or $result.ProductVersion -cne '0.2.0-preview.1') {
            throw 'Full held identity verifier returned wrong source/version/raw byte count.'
        }
        if ((Get-FixtureHash $metadata) -cne $hashBefore) { throw 'Verifier modified metadata bytes.' }
        $exclusive = [IO.File]::Open($metadata, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        $exclusive.Dispose()
    }
    Assert-Rejected { Invoke-FullVerifier $metadata ('b' * 40) } 'does not match expected source commit' 'Full wrong source'
    Assert-Rejected { Invoke-FullVerifier $metadata $source 'v0.2.0-preview.2' } 'does not exactly match source product version' 'Full wrong tag'
    [IO.File]::WriteAllBytes($metadata, [byte[]]($bom + [Text.Encoding]::UTF8.GetBytes($duplicate)))
    Assert-Rejected { Invoke-FullVerifier $metadata $source } 'Duplicate top-level JSON property name' 'Full duplicate identity'
}
finally {
    $fullDirectory = [IO.Path]::GetFullPath($directory)
    $temporaryParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $fullDirectory.StartsWith($temporaryParent, [StringComparison]::OrdinalIgnoreCase) -or
        -not ([IO.Path]::GetFileName($fullDirectory)).StartsWith('qs3d-v25-metadata-preamble-', [StringComparison]::Ordinal)) { throw 'Unsafe synthetic fixture cleanup path.' }
    Remove-Item -LiteralPath $fullDirectory -Recurse -Force
}
Write-Output 'PASS V25 held strict UTF-8 preamble and full identity verifier'
