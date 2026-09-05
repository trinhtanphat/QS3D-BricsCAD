$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$wrapperPath = Join-Path $PSScriptRoot '../../scripts/run-local022-ui-qualification.ps1'
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($wrapperPath, [ref]$null, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'FAIL: wrapper parse errors.' }
$helper = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Get-Local022SourceProfile'
}, $true)
if ($null -eq $helper) { throw 'FAIL: actual source-profile selector missing.' }
. ([scriptblock]::Create($helper.Extent.Text))
if ($null -ne (Get-Local022SourceProfile '' $false)) { throw 'FAIL: omitted profile must retain native defaults.' }
foreach ($name in @('Default','QS3D-V26-TEST','User Profile')) {
    if ((Get-Local022SourceProfile $name $true) -cne $name) { throw 'FAIL: valid profile identity changed.' }
}
foreach ($name in @('', ' ', ' Default', 'Default ', 'a/b', 'a\b', 'a"b', "a`nb", ('a' * 129))) {
    $rejected = $false
    try { $null = Get-Local022SourceProfile $name $true } catch { $rejected = $true }
    if (-not $rejected) { throw 'FAIL: noncanonical or unsafe source profile accepted.' }
}
if (@($ast.ParamBlock.Parameters | Where-Object {$_.Name.VariablePath.UserPath -ceq 'SourceProfile'}).Count -ne 1) {
    throw 'FAIL: source profile is not exposed by the actual wrapper.'
}
$forward = @($ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.IfStatementAst] -and
    $node.Extent.Text -match '\$parameters\.Profile\s*=\s*\$selectedProfile'
}, $true))
if ($forward.Count -ne 1) { throw 'FAIL: actual source profile forwarding missing or ambiguous.' }
foreach ($major in @(25,26)) {
    foreach ($name in @($null, 'Default')) {
        $parameters = @{ ProductDir="frozen-v$major"; UiDriver='OBSERVED_CLICK_V2'; PauseForOperator=$true }
        $selectedProfile = $name
        . ([scriptblock]::Create($forward[0].Extent.Text))
        if ($null -eq $name) {
            if ($parameters.ContainsKey('Profile')) { throw 'FAIL: omitted source replaced the native default.' }
        } elseif ($parameters.Profile -cne $name) { throw 'FAIL: explicit source was not passed unchanged.' }
        if ($parameters.ProductDir -cne "frozen-v$major" -or $parameters.UiDriver -cne 'OBSERVED_CLICK_V2' -or -not $parameters.PauseForOperator) {
            throw 'FAIL: profile selection changed candidate or driver inputs.'
        }
    }
}
Write-Output 'PASS: actual source-profile selection validates identity, preserves omitted defaults and forwards only Profile without changing candidate/driver inputs; no host or registry mutation.'
