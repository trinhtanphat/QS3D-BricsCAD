$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = '22b2ca9ce2ca5a1fc9522f7b5a298a6ac2f83869'
$v25Hash = '8eeb3823d81eb37fbd84b652e3c7b446bc645f9edac6052afd561123aae55e82'
$v26Hash = '37c477d3884bab5122eb3baad9c472570213d83e7009b5572eeaabda5d6dd14e'
$version = '0.2.0-preview.25'

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

function Assert-V26ProvenanceContract([string]$Text) {
    foreach ($required in @(
        "Assert-JsonPropertySet `$provenance @('product', 'target', 'releaseTag', 'productVersion', 'sourceCommit', 'packageSha256', 'installerSha256', 'hostReferences')",
        '9330806cf29e1e6b01758191aa3d9e8d301d9d3125470b6d139f78c7772f9740',
        'if (@($provenance.hostReferences).Count -ne 4)',
        "Assert-JsonPropertySet `$record @('name', 'length', 'sha256')",
        'Candidate provenance host-reference bytes differ from the installed V26 host.'
    )) { Assert-ContainsLiteral $Text $required 'V26 provenance admission' }
}
function Assert-CurrentMainProductIdentityContract([string]$Text) {
    foreach ($required in @(
        'function Assert-Local022CurrentMainProductIdentity', 'rev-parse --verify origin/main',
        'merge-base --is-ancestor', 'diff --quiet --no-ext-diff',
        'src/QS3D.BricsCAD.V25/', 'src/QS3D.BricsCAD.V26/', 'src/QS3D.Core/',
        'external/QS3D-Platform', 'scripts/package-v25.ps1', 'scripts/package-v26.ps1',
        'scripts/new-v26-candidate-provenance.ps1', 'scripts/assert-v26-candidate-identity.ps1',
        'Protected main product/package identity advanced beyond the frozen LOCAL-022 pair.'
    )) { Assert-ContainsLiteral $Text $required 'current-main product identity gate' }
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
Assert-V26ProvenanceContract $v26
$provenanceMutated = $v26.Replace("'installerSha256', 'hostReferences'", "'installerSha256'")
$provenanceNegativeRejected = $false
try { Assert-V26ProvenanceContract $provenanceMutated } catch { $provenanceNegativeRejected = $true }
if (-not $provenanceNegativeRejected) { throw 'negative V26 provenance schema mutation was not rejected' }
Assert-CurrentMainProductIdentityContract $wrapper
$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'scripts\run-local022-ui-qualification.ps1'),[ref]$null,[ref]$parseErrors)
if($parseErrors.Count){throw 'wrapper AST parse failed'}
$fn=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Assert-Local022CurrentMainProductIdentity'},$true))
if($fn.Count -ne 1){throw 'current-main product identity helper is unavailable'}
. ([scriptblock]::Create($fn[0].Extent.Text))
$tmp=Join-Path ([IO.Path]::GetTempPath()) ('local022-main-identity-'+[Guid]::NewGuid().ToString('N'))
try {
    git init -q $tmp; git -C $tmp config user.email 'local022@test.invalid'; git -C $tmp config user.name 'LOCAL022 Test'
    New-Item -ItemType Directory -Force -Path (Join-Path $tmp 'docs'),(Join-Path $tmp 'src\QS3D.Core') | Out-Null
    Set-Content (Join-Path $tmp 'docs\note.txt') 'source'; Set-Content (Join-Path $tmp 'src\QS3D.Core\core.txt') 'source'
    git -C $tmp add .; git -C $tmp commit -q -m source; $frozen=(git -C $tmp rev-parse HEAD).Trim()
    Set-Content (Join-Path $tmp 'docs\note.txt') 'harmless'; git -C $tmp add .; git -C $tmp commit -q -m harmless
    git -C $tmp update-ref refs/remotes/origin/main HEAD
    [void](Assert-Local022CurrentMainProductIdentity $tmp $frozen)
    Set-Content (Join-Path $tmp 'src\QS3D.Core\core.txt') 'drift'; git -C $tmp add .; git -C $tmp commit -q -m product-drift
    git -C $tmp update-ref refs/remotes/origin/main HEAD
    $rejected=$false; try { Assert-Local022CurrentMainProductIdentity $tmp $frozen } catch { $rejected=$true }
    if(-not $rejected){throw 'product drift was not rejected'}
} finally { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }
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
