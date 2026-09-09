param(
 [switch]$ConfirmTemporaryAutostartPause,
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ExpectedPluginSha256,
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ExpectedCoreSha256,
 [Parameter(Mandatory=$true)][string]$ProductWorktree,
 [Parameter(Mandatory=$true)][string]$InputPrefix,
 [Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]{4,50}$')][string]$AllocationName
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $ConfirmTemporaryAutostartPause){throw 'Explicit pause authorization required.'}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$root=Join-Path (Join-Path $repo 'artifacts\issue4041') $AllocationName
$drawing=Join-Path $root 'quantity-raft.dwg'
$prior=[IO.Path]::GetFullPath($InputPrefix)
$installedPlugin=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'QS3D\BricsCAD-V25\QS3D.BricsCAD.V25.dll'
$installedCore=Join-Path (Split-Path $installedPlugin) 'QS3D.Core.dll'
$productWorktree=[IO.Path]::GetFullPath($ProductWorktree)
$plugin=Join-Path $productWorktree 'dist\QS3D-BricsCAD-V25\QS3D.BricsCAD.V25.dll'
if((git -C $productWorktree rev-parse HEAD).Trim() -cne 'af6c585190efb80581e286add7027540e7cc7c52' -or @(git -C $productWorktree status --porcelain).Count){throw 'Product source mismatch/dirty.'}
$core=Join-Path (Split-Path $plugin) 'QS3D.Core.dll'
$exe=Join-Path $env:ProgramFiles 'Bricsys\BricsCAD V25 en_US\bricscad.exe'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash}
function NoHosts {
 if(@(Get-Process bricscad -ErrorAction SilentlyContinue).Count){throw 'Existing CAD.'}
 if(@(Get-CimInstance Win32_Process | Where-Object Name -Match '^(cloudflared|tunnel-client)').Count){throw 'Existing tunnel.'}
}
NoHosts
$harnessSha=(git -C $repo rev-parse HEAD).Trim()
if(@(git -C $repo status --porcelain).Count){throw 'Harness must be committed and clean.'}
$branch=(git -C $repo branch --show-current).Trim()
$remote=@(git -C $repo ls-remote origin ('refs/heads/'+$branch))
if($LASTEXITCODE -ne 0 -or $remote.Count -ne 1 -or -not $remote[0].StartsWith($harnessSha+[char]9)){throw 'Exact harness must be pushed.'}
foreach($p in @($prior+'.dwg',$prior+'.qsdb')){
 $item=Get-Item -LiteralPath $p
 if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Input must be an ordinary disposable fixture file.'}
}
if((Hash ($prior+'.dwg')) -cne '8C4F38BC72927527BCD49504300BDE31C0C06DB44E7C3F5C1A73C153A2BC4EBD' -or
 (Hash ($prior+'.qsdb')) -cne 'D4E719B46BC5C63FAA00B10694FF06BD1D8E985215F38D3B2A311C50BF4FA884'){throw 'Input fixture differs from verified saved native allocation.'}
if(Test-Path -LiteralPath $root){throw 'Consumed allocation.'}
if((Hash $installedPlugin) -cne 'CFD563182888E4149FCA5CCE6983AA7C265F68AB093C4DBC372A66E0F67BEE84' -or (Hash $installedCore) -cne 'B6592DBDD99445AF0F606A2732277A365AE81C1F1154CB722F1EDD5BEA38A32A'){throw 'Product mismatch.'}
if((Hash $plugin) -ine $ExpectedPluginSha256 -or (Hash $core) -ine $ExpectedCoreSha256){throw 'Frozen test binaries differ.'}
$reg='HKCU:\Software\Bricsys\BricsCAD\V25x64\en_US\Applications\QS3D'
$registration=Get-ItemProperty -LiteralPath $reg
if($registration.LOADER -ine $installedPlugin -or $registration.LOADCTRLS -ne 4){throw 'Registration mismatch.'}
$flag=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'QS3D\MCP\OpenAiSecureTunnel\autostart.txt'
$cloud=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'QS3D\MCP\CloudflareAccount\autostart.txt'
if([IO.File]::ReadAllText($flag) -cne '1' -or [IO.File]::ReadAllText($cloud).Trim() -cne '0'){throw 'Preference mismatch.'}
$flagHash=Hash $flag; $flagTime=(Get-Item $flag).LastWriteTimeUtc; $cloudHash=Hash $cloud
New-Item -ItemType Directory -Path $root | Out-Null
$backup=Join-Path $root 'openai-original.bin'; Copy-Item -LiteralPath $flag -Destination $backup
Copy-Item -LiteralPath ($prior+'.dwg') -Destination $drawing
Copy-Item -LiteralPath ($prior+'.qsdb') -Destination (Join-Path $root 'quantity-raft.qsdb')
$runId=[Guid]::NewGuid().ToString('N'); $runtime=Join-Path $root 'runtime.txt'; $finish=Join-Path $root 'operator-finish.json'
$record=[ordered]@{schema='QS3D_LOCAL021_QUANTITY_OBSERVED_V1'; run_id=$runId; harness_sha=$harnessSha; source_profile='Default'; source_sha='af6c585190efb80581e286add7027540e7cc7c52'; product_kind='LOCAL_POSTMERGE_DIAGNOSTIC_NOT_PUBLISHED_RELEASE'; plugin_sha256=$ExpectedPluginSha256; core_sha256=$ExpectedCoreSha256; started_utc=[DateTime]::UtcNow.ToString('o'); wrapper_sha256=(Hash $PSCommandPath); input_dwg_sha256=(Hash $drawing); input_qsdb_sha256=(Hash (Join-Path $root 'quantity-raft.qsdb')); status='RUNNING'; aggregate_local021_pass=$false; baseline_verified=$false; cleanup=$null; autostart_restored=$false; protected_install_unchanged=$false; force_close_fallback=$false}
function Record {$record | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $root 'receipt.json') -Encoding utf8}
. (Join-Path $repo 'scripts\v25-profile-sandbox.ps1')
$sandbox=$null; $proc=$null; $failure=$null; $paused=$false
$runtimeEnvBefore=[Environment]::GetEnvironmentVariable('QS3D_RUNTIME_RESULT','Process')
Record
try {
 NoHosts
 if((Hash $flag) -cne $flagHash -or (Get-Item $flag).LastWriteTimeUtc -ne $flagTime){throw 'Preference changed.'}
 [IO.File]::WriteAllText($flag,'0',[Text.UTF8Encoding]::new($false)); $paused=$true; $pauseTime=(Get-Item $flag).LastWriteTimeUtc
 $sandbox=New-Qs3dV25ProfileSandbox -SourceProfile 'Default'
 $sandbox | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $root 'profile-recovery.private.json') -Encoding utf8
 $env:QS3D_RUNTIME_RESULT=$runtime
 $scr=Join-Path $root 'start.scr'
 @('FILEDIA','0','CMDECHO','1','SAVETIME','0','NETLOAD',('"'+$plugin+'"'),'QS3DRUNTIMEPROBE','QS3DRELOAD','_.VPOINT','1,-1,1','_.ZOOM','_Extents','(progn (vl-load-com) (setq qs (ssget "_X" ''((0 . "3DSOLID")))) (if (and qs (= (sslength qs) 1) (equal (/ (vla-get-Volume (vlax-ename->vla-object (ssname qs 0))) 1e9) 19.2 1e-7)) (progn (sssetfirst nil qs) (command "QS3DQUANTITYINSIGHT")) (princ "NATIVE_PRECHECK_FAILED")))','') | Set-Content $scr -Encoding ascii
 $proc=Start-Process -FilePath $exe -ArgumentList ('"'+$drawing+'" /P "'+$sandbox.NonceProfile+'" /B "'+$scr+'"') -WorkingDirectory $root -WindowStyle Hidden -PassThru
 $record.process_id=$proc.Id; $record.process_start_utc=$proc.StartTime.ToUniversalTime().ToString('o'); Record
 $deadline=[DateTime]::UtcNow.AddSeconds(150)
 while(-not (Test-Path $runtime)){if($proc.HasExited){throw 'Host exited before baseline.'}; if([DateTime]::UtcNow -ge $deadline){throw 'Baseline timeout.'}; Start-Sleep -Milliseconds 500}
 $rt=@{}; foreach($line in Get-Content $runtime){$p=$line.Split('=',2); if($p.Count -eq 2){if($rt.ContainsKey($p[0])){throw 'Duplicate runtime field.'}; $rt[$p[0]]=$p[1]}}
 if($rt.status -cne 'PASS' -or $rt.assembly -ine $plugin -or $rt.native_runtime_major -cne '25' -or $rt.host_file_version -cne '25.2.10'){throw 'Host identity mismatch.'}
 foreach($key in @('is_64bit','native_runtime_matches','ribbon_ready','workspace_palette_visible','right_palette_visible')){if($rt[$key] -cne 'true'){throw ('Runtime flag: '+$key)}}
 $record.baseline_verified=$true; $record.status='AWAITING_OBSERVED_QUANTITY_UI'; Record; 'LOCAL021_QUANTITY_UI_READY'
 $deadline=[DateTime]::UtcNow.AddMinutes(10)
 while(-not (Test-Path $finish)){if($proc.HasExited){throw 'Host exited before operator completion.'}; if([DateTime]::UtcNow -ge $deadline){throw 'Observed UI bound expired.'}; Start-Sleep -Milliseconds 500; $proc.Refresh()}
 $ack=Get-Content $finish -Raw | ConvertFrom-Json
 if($ack.run_id -cne $runId -or $ack.action -cne 'finish_observation'){throw 'Foreign completion.'}
 $record.status='OBSERVATION_FINISHED_NOT_AGGREGATE_PASS'
} catch {$failure=$_; $record.status='FAIL_OR_NO_RESULT'; $record.error=$_.Exception.Message}
finally {
 [Environment]::SetEnvironmentVariable('QS3D_RUNTIME_RESULT',$runtimeEnvBefore,'Process')
 if($null -ne $proc){$proc.Refresh(); if(-not $proc.HasExited){[void]$proc.CloseMainWindow(); if(-not $proc.WaitForExit(10000)){$record.force_close_fallback=$true; Stop-Process -Id $proc.Id -Force; [void]$proc.WaitForExit(10000)}}; $record.graceful_exit=$proc.HasExited -and $proc.ExitCode -eq 0 -and -not $record.force_close_fallback; $proc.Dispose()}
 $settle=[DateTime]::UtcNow.AddSeconds(10)
 while(@(Get-Process bricscad -ErrorAction SilentlyContinue).Count -gt 0 -and [DateTime]::UtcNow -lt $settle){Start-Sleep -Milliseconds 250}
 try {
  NoHosts
  if($null -ne $sandbox){$record.cleanup=Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox}
  if($paused){if([IO.File]::ReadAllText($flag) -cne '0' -or (Get-Item $flag).LastWriteTimeUtc -ne $pauseTime){throw 'External preference change.'}; Copy-Item -LiteralPath $backup -Destination $flag -Force; (Get-Item $flag).LastWriteTimeUtc=$flagTime; if((Hash $flag) -cne $flagHash -or (Hash $cloud) -cne $cloudHash){throw 'Preference restoration mismatch.'}; $record.autostart_restored=$true}
  $after=Get-ItemProperty $reg
  if($after.LOADER -ine $installedPlugin -or $after.LOADCTRLS -ne 4 -or (Hash $installedPlugin) -cne 'CFD563182888E4149FCA5CCE6983AA7C265F68AB093C4DBC372A66E0F67BEE84' -or (Hash $installedCore) -cne 'B6592DBDD99445AF0F606A2732277A365AE81C1F1154CB722F1EDD5BEA38A32A'){throw 'Installed state changed.'}
  if((Hash $plugin) -ine $ExpectedPluginSha256 -or (Hash $core) -ine $ExpectedCoreSha256){throw 'Test binary changed during run.'}
  if((Hash ($prior+'.dwg')) -cne $record.input_dwg_sha256 -or (Hash ($prior+'.qsdb')) -cne $record.input_qsdb_sha256){throw 'Original fixture changed.'}
  $record.protected_install_unchanged=$true
 } catch {$record.cleanup_error=$_.Exception.Message; $record.status='CLEANUP_FAILED'; $failure=$_}
 $record.finished_utc=[DateTime]::UtcNow.ToString('o'); Record
}
if($null -ne $failure){throw $failure}
'LOCAL021_OBSERVATION_ENDED_WITH_CLEANUP'
