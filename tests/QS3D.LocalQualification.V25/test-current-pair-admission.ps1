$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = '4112bb11f86bab3e80ae73c631f9e7b81b13bb84'
$v25Hash = 'f9395c0df502088d9622fd54441347a1e060e5e6e509e9325bee9e5ef5c906ce'
$v26Hash = '1ebc68cf66d9cf916ded572a65ee4442558de96effdd6cee71a638df17a0ee99'
$version = '0.2.0-preview.23'

function Read-Script([string]$RelativePath) {
    return [IO.File]::ReadAllText((Join-Path $repo $RelativePath))
}
function Assert-ContainsLiteral([string]$Text, [string]$Needle, [string]$Label) {
    if ($Text.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label does not admit the frozen current pair: $Needle"
    }
}

function Assert-NativeV25PredecessorGate([string]$Text) {
    foreach ($required in @(
        '[string]$PrecedingV25Receipt', 'Assert-Local022NativeV25Predecessor',
        'Assert-Local022NativeV25Phases', '$Receipt.status -cne ''LOCAL_PASS_BOUNDED''',
        '$Receipt.phases_verified -ne 3', '$Receipt.profile_cleanup.zero_bricscad_processes',
        '$Receipt.profile_cleanup.profile_inventory_restored',
        'Assert-Local022NativeV25Predecessor $v25 $allocation $restore $source $v25PackageSha256',
        'Assert-Local022NativeV25Phases (Join-Path $PSScriptRoot ''test-bricscad-v25-single-footing.ps1'') $v25Root $v25.run_id'
    )) { Assert-ContainsLiteral $Text $required 'LOCAL-022 V25 predecessor gate' }
}

$v25 = Read-Script 'scripts\test-bricscad-v25-single-footing.ps1'
$v26 = Read-Script 'scripts\test-bricscad-v26-single-footing.ps1'
$wrapper = Read-Script 'scripts\run-local022-ui-qualification.ps1'
Assert-ContainsLiteral $v25 $source 'V25 runner'
Assert-ContainsLiteral $v25 $v25Hash 'V25 runner'
Assert-ContainsLiteral $v25 $version 'V25 runner'
Assert-ContainsLiteral $v26 $source 'V26 runner'
Assert-ContainsLiteral $v26 $v26Hash 'V26 runner'
Assert-ContainsLiteral $v26 $version 'V26 runner'
Assert-ContainsLiteral $v26 "Assert-JsonPropertySet `$provenance @('product', 'target', 'releaseTag', 'productVersion', 'sourceCommit', 'packageSha256', 'installerSha256', 'hostReferences')" 'V26 provenance schema gate'
Assert-ContainsLiteral $v26 '9330806cf29e1e6b01758191aa3d9e8d301d9d3125470b6d139f78c7772f9740' 'V26 provenance installer gate'
Assert-ContainsLiteral $v26 'if (@($provenance.hostReferences).Count -ne 4)' 'V26 provenance host-reference count gate'
Assert-ContainsLiteral $v26 "Assert-JsonPropertySet `$record @('name', 'length', 'sha256')" 'V26 provenance host-reference schema gate'
Assert-ContainsLiteral $wrapper $source 'LOCAL-022 wrapper'
Assert-ContainsLiteral $wrapper $v25Hash 'LOCAL-022 wrapper'
Assert-ContainsLiteral $wrapper $v26Hash 'LOCAL-022 wrapper'
if ($wrapper.Contains('Current-source V26 package unavailable')) {
    throw 'LOCAL-022 wrapper still hard-blocks V26 despite a frozen matched package.'
}
Assert-NativeV25PredecessorGate $wrapper
$mutated = $wrapper.Replace(
    "        Assert-Local022NativeV25Phases (Join-Path `$PSScriptRoot 'test-bricscad-v25-single-footing.ps1') `$v25Root `$v25.run_id",
    '')
$negativeRejected = $false
try { Assert-NativeV25PredecessorGate $mutated } catch { $negativeRejected = $true }
if (-not $negativeRejected) { throw 'negative predecessor gate mutation was not rejected' }
Write-Host 'PASS: LOCAL-022 frozen pair and V25 predecessor cleanup/phases gate are guarded.'
