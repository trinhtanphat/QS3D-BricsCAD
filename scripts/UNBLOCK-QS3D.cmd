@echo off
setlocal
set "QS3D_PACKAGE_ROOT=%~dp0"
set "QS3D_UNBLOCK_HELPER=%~dp0unblock-v25-netload.ps1"
echo QS3D for BricsCAD V25 - manual NETLOAD recovery
echo Verifies the packaged helper and complete SHA256SUMS manifest before unblocking DLLs.
echo.
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -Command "$ErrorActionPreference='Stop'; $root=[IO.Path]::GetFullPath($env:QS3D_PACKAGE_ROOT); $p=$env:QS3D_UNBLOCK_HELPER; $m=Join-Path $root 'SHA256SUMS.txt'; if(-not (Test-Path -LiteralPath $p -PathType Leaf)){ throw 'unblock-v25-netload.ps1 is missing from this package.' }; if(-not (Test-Path -LiteralPath $m -PathType Leaf)){ throw 'SHA256SUMS.txt is missing from this package.' }; $matches=@(Get-Content -LiteralPath $m | Where-Object { $_ -match '^([0-9A-Fa-f]{64})\s{2}unblock-v25-netload\.ps1$' }); if($matches.Count -ne 1){ throw 'SHA256SUMS.txt must contain exactly one unblock-v25-netload.ps1 entry.' }; if($matches[0] -notmatch '^([0-9A-Fa-f]{64})\s{2}unblock-v25-netload\.ps1$'){ throw 'Invalid unblock helper hash entry.' }; $expected=$Matches[1].ToUpperInvariant(); $actual=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToUpperInvariant(); if($actual -ne $expected){ throw 'SHA-256 mismatch for unblock-v25-netload.ps1.' }; $s=Get-AuthenticodeSignature -LiteralPath $p; if($s.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and $s.SignerCertificate){ Write-Host ('Verified QS3D recovery helper signer: ' + $s.SignerCertificate.Subject) } elseif($s.Status -eq [System.Management.Automation.SignatureStatus]::NotSigned){ Write-Warning 'This QS3D preview recovery helper is unsigned. Its package hash was verified before bootstrap.' } else { throw ('QS3D recovery helper signature is invalid or untrusted: ' + $s.Status) }; Unblock-File -LiteralPath $p -ErrorAction Stop; $item=Get-Item -LiteralPath $p -Force -ErrorAction Stop; if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){ throw 'unblock-v25-netload.ps1 must be an ordinary non-reparse file.' }; $stream=[IO.File]::Open($item.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read); try { $sha=[Security.Cryptography.SHA256]::Create(); try { $heldHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToUpperInvariant() } finally { $sha.Dispose() }; if($heldHash -ne $expected){ throw 'SHA-256 mismatch for held unblock-v25-netload.ps1 generation.' }; $current=Get-Item -LiteralPath $item.FullName -Force -ErrorAction Stop; if($current.PSIsContainer -or ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $current.Length -ne $stream.Length -or $current.LastWriteTimeUtc.Ticks -ne $item.LastWriteTimeUtc.Ticks){ throw 'unblock-v25-netload.ps1 changed during held-generation admission.' }; $heldSignature=Get-AuthenticodeSignature -LiteralPath $item.FullName; if($heldSignature.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and $heldSignature.SignerCertificate){ Write-Host ('Reverified held QS3D recovery helper signer: ' + $heldSignature.SignerCertificate.Subject) } elseif($heldSignature.Status -eq [System.Management.Automation.SignatureStatus]::NotSigned){ Write-Warning 'Held QS3D preview recovery helper is unsigned. Its exact package hash was reverified before execution.' } else { throw ('Held QS3D recovery helper signature is invalid or untrusted: ' + $heldSignature.Status) }; & $p -PackageDirectory $root } finally { $stream.Dispose() }"
if errorlevel 1 goto :failed

echo.
echo QS3D package is ready for manual NETLOAD.
echo NETLOAD: %~dp0QS3D.BricsCAD.V25.dll
echo No BricsCAD security setting was changed.
pause
exit /b 0

:failed
echo.
echo QS3D manual NETLOAD recovery FAILED.
echo Do not NETLOAD this package until its integrity issue is resolved.
pause
exit /b 1
