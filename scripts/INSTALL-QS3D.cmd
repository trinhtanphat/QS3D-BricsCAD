@echo off
setlocal
set "QS3D_PACKAGE_ROOT=%~dp0"
echo QS3D for BricsCAD V25 - secure installer
echo.
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -Command "$ErrorActionPreference='Stop'; $p=Join-Path $env:QS3D_PACKAGE_ROOT 'install-v25-autoload.ps1'; if(-not (Test-Path -LiteralPath $p -PathType Leaf)){ throw 'install-v25-autoload.ps1 is missing from this package.' }; $s=Get-AuthenticodeSignature -LiteralPath $p; if($s.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or -not $s.SignerCertificate){ throw ('QS3D installer signature is not valid: ' + $s.Status) }; Write-Host ('Verified QS3D installer signer: ' + $s.SignerCertificate.Subject); ^& $p; if(-not $?){ exit 1 }"
if errorlevel 1 (
  echo.
  echo QS3D installation FAILED. No BricsCAD security setting was weakened.
  pause
  exit /b 1
)
echo.
echo QS3D installation completed. Start BricsCAD V25 and run QS3D.
pause
exit /b 0
