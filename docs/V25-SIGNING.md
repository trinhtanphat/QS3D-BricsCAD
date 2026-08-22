# BricsCAD V25 production signing

QS3D release binaries should be Authenticode-signed only on a trusted Windows release machine. The repository intentionally contains no private signing certificate, PFX/P12 file, certificate password, token PIN, or signing secret.

## Release-machine prerequisites

- Install the code-signing certificate in `Cert:\CurrentUser\My`.
- The certificate must have an accessible private key and Code Signing EKU (`1.3.6.1.5.5.7.3.3`).
- Keep hardware-token/PIN handling outside this repository and outside command history.
- Use an HTTPS RFC3161/Authenticode timestamp service approved by the certificate provider or organization.

## Signing

After the V25 plugin is built against the installed BricsCAD V25 SDK/runtime references, sign the binaries **before** creating the final package so package hashes describe the signed payload.

```powershell
.\scripts\sign-v25.ps1 `
  -Path @(
    '.\src\QS3D.BricsCAD.V25\bin\Release\net48\QS3D.BricsCAD.V25.dll',
    '.\src\QS3D.BricsCAD.V25\bin\Release\net48\QS3D.Core.dll'
  ) `
  -CertificateThumbprint '<40-HEX-THUMBPRINT>' `
  -TimestampServer 'https://<approved-timestamp-service>'
```

The signer requires SHA-256, validates the certificate/private-key/EKU/validity period, requires HTTPS timestamping, and performs a post-sign `Get-AuthenticodeSignature` verification.

## Verification

```powershell
.\scripts\verify-v25-signatures.ps1 `
  -Path @(
    '.\src\QS3D.BricsCAD.V25\bin\Release\net48\QS3D.BricsCAD.V25.dll',
    '.\src\QS3D.BricsCAD.V25\bin\Release\net48\QS3D.Core.dll'
  ) `
  -ExpectedThumbprint '<40-HEX-THUMBPRINT>'
```

Verification fails if a file is unsigned, has an invalid/untrusted signature, is signed by an unexpected certificate when a thumbprint is supplied, or has no timestamp certificate.

## Repository policy

- Never commit `.pfx` / `.p12` files or private keys.
- Never weaken BricsCAD `SECURELOAD` to make an unsigned package load.
- Do not put certificate passwords or token PINs in GitHub variables, Markdown, scripts, package manifests, or command examples.
- GitHub Actions remain manual-only. Signing is not proof of BricsCAD V25 runtime compatibility; the licensed V25 compile/NETLOAD/runtime gate is still required.
