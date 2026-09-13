$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = 'd5e5e3851125b279bc6807074a34f8de2700cef5'
$v25Hash = 'e27d645b88af709369ac8c04b96fff43b8c688496b8908694825b7491efc2633'
$v26Hash = 'a2e358d3aa5c661249f4df00631506c3773efe7e8e26f47a8187b4b600820ec2'
$version = '0.2.0-preview.14'

function Read-Script([string]$RelativePath) {
    return [IO.File]::ReadAllText((Join-Path $repo $RelativePath))
}
function Assert-ContainsLiteral([string]$Text, [string]$Needle, [string]$Label) {
    if ($Text.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label does not admit the frozen current pair: $Needle"
    }
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
Write-Host 'PASS: LOCAL-022 wrapper and V25/V26 runners admit the frozen current package pair.'
