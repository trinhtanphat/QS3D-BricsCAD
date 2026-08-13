@echo off
setlocal
set "QS3D_PACKAGE_ROOT=%~dp0"
set "QS3D_INSTALLER=%~dp0install-v25-autoload.ps1"
echo QS3D for BricsCAD V25 - secure installer
echo.
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -Command "$ErrorActionPreference='Stop'; $p=$env:QS3D_INSTALLER; if(-not (Test-Path -LiteralPath $p -PathType Leaf)){ throw 'install-v25-autoload.ps1 is missing from this package.' }; $s=Get-AuthenticodeSignature -LiteralPath $p; if($s.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and $s.SignerCertificate){ Write-Host ('Verified QS3D installer signer: ' + $s.SignerCertificate.Subject) } elseif($s.Status -eq [System.Management.Automation.SignatureStatus]::NotSigned){ Write-Warning 'This QS3D preview installer is unsigned. Continuing in preview mode; production one-click update still requires signed releases.' } else { throw ('QS3D installer signature is invalid or untrusted: ' + $s.Status) }; Unblock-File -LiteralPath $p -ErrorAction Stop"
if errorlevel 1 goto :failed

powershell.exe -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%QS3D_INSTALLER%" -Confirm:$false
if errorlevel 1 goto :failed

echo.
echo QS3D installation completed. Start BricsCAD V25 and run QS3D.
pause
exit /b 0

:failed
echo.
echo QS3D installation FAILED. No BricsCAD security setting was weakened.
pause
exit /b 1
