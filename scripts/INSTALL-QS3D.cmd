@echo off
setlocal
set "QS3D_PACKAGE_ROOT=%~dp0"
set "QS3D_INSTALLER=%~dp0install-v25-autoload.ps1"
echo QS3D for BricsCAD V25 - secure installer
echo.

if not exist "%QS3D_INSTALLER%" goto :missing_companion

powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -Command "$ErrorActionPreference='Stop'; $p=$env:QS3D_INSTALLER; $s=Get-AuthenticodeSignature -LiteralPath $p; if($s.Status -eq [System.Management.Automation.SignatureStatus]::Valid -and $s.SignerCertificate){ Write-Host ('Verified QS3D installer signer: ' + $s.SignerCertificate.Subject) } elseif($s.Status -eq [System.Management.Automation.SignatureStatus]::NotSigned){ Write-Warning 'This QS3D preview installer is unsigned. Continuing in preview mode; production one-click update still requires signed releases.' } else { throw ('QS3D installer signature is invalid or untrusted: ' + $s.Status) }; Unblock-File -LiteralPath $p -ErrorAction Stop; & $p -Confirm:$false"
if errorlevel 1 goto :failed

echo.
echo QS3D installation completed. Start BricsCAD V25 and run QS3D.
pause
exit /b 0

:missing_companion
echo.
echo ERROR: install-v25-autoload.ps1 is missing next to INSTALL-QS3D.cmd.
echo Extract All / Giai nen tat ca the complete QS3D ZIP to a normal local folder.
echo Keep INSTALL-QS3D.cmd and install-v25-autoload.ps1 in the same extracted folder.
echo PowerShell was not started; no installation changes were made.
pause
exit /b 2

:failed
echo.
echo QS3D installation FAILED. No BricsCAD security setting was weakened.
pause
exit /b 1
