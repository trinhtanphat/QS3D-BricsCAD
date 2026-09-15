$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = '99c6dd4a91fcbb0bb911b2593ca2f84246df451e'
$v25Hash = '3ef6d526f60815b123b35fe239e404c1a9c47ff2e8053f9985c8edc7069a5474'
$v26Hash = 'c738c1ae1da5569a61bd58f2776d8715e3850d1bf0e27d3f219cbcf31cff2bd4'
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


function Assert-ScriptStageWitnessContract([string]$Text) {
    foreach ($required in @(
        'function New-Local022ScriptStageWitnessLine',
        'function Get-Local022HighestScriptStage',
        'script_start', 'before_product_netload', 'after_product_netload',
        'before_probe_netload', 'after_probe_netload',
        'before_phase_command', 'after_phase_command',
        'highest_script_stage='
    )) { Assert-ContainsLiteral $Text $required 'V25 script-stage witness contract' }
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
Assert-ScriptStageWitnessContract $v25
$witnessMutated = $v25.Replace('after_phase_command', 'after_phase_commanX')
$witnessNegativeRejected = $false
try { Assert-ScriptStageWitnessContract $witnessMutated } catch { $witnessNegativeRejected = $true }
if (-not $witnessNegativeRejected) { throw 'negative script-stage witness mutation was not rejected' }
$mutated = $wrapper.Replace(
    "        Assert-Local022NativeV25Phases (Join-Path `$PSScriptRoot 'test-bricscad-v25-single-footing.ps1') `$v25Root `$v25.run_id",
    '')
$negativeRejected = $false
try { Assert-NativeV25PredecessorGate $mutated } catch { $negativeRejected = $true }
if (-not $negativeRejected) { throw 'negative predecessor gate mutation was not rejected' }
Write-Host 'PASS: LOCAL-022 frozen pair and V25 predecessor cleanup/phases gate are guarded.'
