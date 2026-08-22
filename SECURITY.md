# Security policy

## Reporting a vulnerability

Please do **not** report a suspected exploitable vulnerability, leaked credential, signing material, private customer drawing, or other sensitive security issue in a public GitHub Issue, Discussion, pull request, CI log, or screenshot.

Use GitHub private vulnerability reporting for this repository when it is available. If private vulnerability reporting is not available, contact the repository owner through the GitHub account `@trinhtanphat` to establish a private reporting channel before sending exploit details or sensitive evidence.

A useful report includes:

- affected QS3D release tag and/or exact commit SHA;
- BricsCAD V25/V26 and host version when relevant;
- impact and realistic attack preconditions;
- minimal reproduction steps or proof of concept;
- whether credentials, signing keys, customer data, or release/update channels may be exposed;
- suggested mitigation when known.

Sanitize drawings, logs, screenshots, paths and machine/user identifiers before sharing them. Never include private keys, PFX/P12 contents, passwords, access tokens, or unredacted customer data in repository content.

## Scope

Security reports are accepted for the current repository code and published QS3D release/update mechanisms. Preview releases may change quickly, but vulnerabilities in preview package integrity, update verification, provenance, signing, or protected-main/release automation are still security-relevant.

## Release and update security expectations

QS3D release automation is expected to fail closed on source identity, package integrity, release-tag/version binding, signing identity where signing is required, and update-manifest validation. A security fix must not weaken those controls merely to restore a green build.

Licensed BricsCAD runtime binaries and signing credentials are external dependencies/secrets and must not be committed to this repository.

## Disclosure

Please allow the maintainer a reasonable opportunity to reproduce, fix, validate and publish a security update before public disclosure. Once a fix is available, public release notes should describe impact and remediation without exposing secrets or unnecessary customer data.
