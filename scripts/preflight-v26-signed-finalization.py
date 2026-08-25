from pathlib import Path

root = Path(__file__).resolve().parents[1]
runner = (root / 'scripts' / 'test-v26-signed-finalization.ps1').read_text(encoding='utf-8')
required = [
    "ExpectedGitSha", "ExpectedSignerThumbprint", "TimestampServer", "PackageUri",
    "status','--porcelain", "HasPrivateKey", "sign-v26.ps1", "verify-v26-signatures.ps1",
    "finalize-v26-signed-package.ps1", "new-v26-update-manifest.ps1", "Get-AuthenticodeSignature",
    "Get-FileHash", "QS3D_V26_SIGNED_FINALIZATION_LOCAL_PASS", "signerThumbprintSha256",
]
missing = [token for token in required if token not in runner]
if missing:
    raise SystemExit('V26 signed-finalization runner contract missing: ' + ', '.join(missing))
for forbidden in ('http://', 'Set-AuthenticodeSignature', 'workflow_dispatch', 'gh workflow run'):
    if forbidden.lower() in runner.lower():
        raise SystemExit('V26 signed-finalization runner contains forbidden weakening/dispatch token: ' + forbidden)
for script in ('sign-v26.ps1','verify-v26-signatures.ps1','finalize-v26-signed-package.ps1','new-v26-update-manifest.ps1'):
    if not (root / 'scripts' / script).is_file():
        raise SystemExit('Required V26 signing/finalization script missing: ' + script)
print('V26 signed finalization qualification source guard: PASS')
