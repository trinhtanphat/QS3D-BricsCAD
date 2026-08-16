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

## GitHub Actions supply-chain policy

- Every external `uses:` reference under `.github` must resolve to one immutable full 40-hex Git commit SHA. Human-readable version comments such as `# v7` are encouraged, but mutable tags or branches are not accepted.
- Local actions referenced with `./...` are allowed. Container actions, if introduced, must use an immutable `sha256:` image digest.
- `scripts/preflight-github-actions-pins.py` is a fail-closed repository gate and is automatically included by `scripts/preflight-all.py`. Do not bypass or weaken it to make CI green.
- Checkout credentials should use `persist-credentials: false` unless a narrowly reviewed workflow has a demonstrated need for Git credential persistence.
- `GITHUB_TOKEN` permissions must remain least-privilege. Read-only build/test jobs use `contents: read`; write scopes are limited to trusted owner-controlled dispatch or release paths that actually require them.
- Untrusted pull-request code must never be executed with a write-capable token. In particular, do not introduce a `pull_request_target` path that checks out or executes untrusted PR content under elevated permissions.
- Dependabot GitHub Actions major upgrades are compatibility/security review events. They must pass the repository CI gates and are not merged by bypassing failed checks; when accepted, the resulting action is still pinned to an immutable full commit SHA.
- Merge/release provenance should prefer GitHub-verified or otherwise cryptographically verified commits where the platform supports it. Release workflows must continue to bind artifacts and tags to the exact qualified source SHA.

## Release and update security expectations

QS3D release automation is expected to fail closed on source identity, package integrity, release-tag/version binding, signing identity where signing is required, and update-manifest validation. A security fix must not weaken those controls merely to restore a green build.

Licensed BricsCAD runtime binaries and signing credentials are external dependencies/secrets and must not be committed to this repository.

## Disclosure

Please allow the maintainer a reasonable opportunity to reproduce, fix, validate and publish a security update before public disclosure. Once a fix is available, public release notes should describe impact and remediation without exposing secrets or unnecessary customer data.
