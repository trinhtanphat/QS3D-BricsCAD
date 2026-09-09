$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$runner=Join-Path $PSScriptRoot 'run-quantity-observation.ps1'
$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($runner,[ref]$null,[ref]$parseErrors)
if($parseErrors.Count){throw 'Runner must parse.'}
$rejected=$false
try {
 & $runner -ExpectedPluginSha256 ('a'*64) -ExpectedCoreSha256 ('b'*64) -ProductWorktree unused -InputPrefix unused -AllocationName host-free-test
} catch {
 if($_.Exception.Message -cne 'Explicit pause authorization required.'){throw}
 $rejected=$true
}
if(-not $rejected){throw 'Missing consent reached machine setup.'}
$inputLoop=@($ast.EndBlock.Statements | Where-Object {
 $_ -is [Management.Automation.Language.ForEachStatementAst] -and $_.Variable.VariablePath.UserPath -ceq 'p'
})
if($inputLoop.Count -ne 1){throw 'Exact fixture input loop required.'}
& {
 $prior='fixture'
 $paths=@(& ([scriptblock]::Create($inputLoop[0].Condition.Extent.Text)))
 if($paths.Count -ne 2 -or $paths[0] -cne 'fixture.dwg' -or $paths[1] -cne 'fixture.qsdb'){throw 'Fixture paths must remain two separate inputs.'}
}
$hostGuard=@($ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'NoHosts'},$true))
if($hostGuard.Count -ne 1){throw 'Unique host guard required.'}
. ([scriptblock]::Create($hostGuard[0].Extent.Text))
& {
 function Get-Process { [pscustomobject]@{Id=123} }
 $refused=$false
 try { NoHosts } catch { if($_.Exception.Message -cne 'Existing CAD.'){throw}; $refused=$true }
 if(-not $refused){throw 'Existing host was admitted.'}
}
& {
 function Get-Process {}
 function Get-CimInstance { [pscustomobject]@{Name='cloudflared.exe'} }
 $refused=$false
 try { NoHosts } catch { if($_.Exception.Message -cne 'Existing tunnel.'){throw}; $refused=$true }
 if(-not $refused){throw 'Existing tunnel was admitted.'}
}
Write-Output 'PASS: runner parses; missing consent and existing CAD/tunnel refuse. No native host or machine mutation executed.'
