@echo off
setlocal
set "QS3D_PACKAGE_ROOT=%~dp0"
set "QS3D_INSTALLER=%~dp0install-v25-autoload.ps1"
echo QS3D for BricsCAD V25 - secure installer
echo.
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -Command "$ErrorActionPreference='Stop'; $p=$env:QS3D_INSTALLER; if(-not (Test-Path -LiteralPath $p -PathType Leaf)){ throw 'install-v25-autoload.ps1 is missing from this package.' }; $s=Get-AuthenticodeSignature -LiteralPath $p; if($s.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $s.SignerCertificate){ throw ('QS3D installer signature is not valid: ' + $s.Status) }; Write-Host ('Verified QS3D installer signer: ' + $s.SignerCertificate.Subject)"
if errorlevel 1 goto :failed

powershell.exe -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%QS3D_INSTALLER%"
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
