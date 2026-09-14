$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = '602947c4d8b41e0127ab84dd47c17f56ce25ace1'
$v25Hash = '45ffdfd96c575c5a8a899ad7815d470d2387a092eff228602a1acc65feff9973'
$v26Hash = '71914c172370db13bf0eadfa00ad47977823f877f409dfc825975abc7d0416b4'
$version = '0.2.0-preview.19'

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
