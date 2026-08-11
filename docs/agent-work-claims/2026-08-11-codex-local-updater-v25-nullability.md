# Work claim — Updater V25 nullable contract integration

- Status: `COMPLETED`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-11T21:31:00+07:00`
- Baseline main SHA: `f373d932fd90faf7355283234da7e711633339d8`
- Priority: restore the exact BricsCAD V25 `Release|x64` build after the released remote updater lane landed without nullable-reference compilation proof.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs`
- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Updates/SemanticReleaseVersion.cs`
- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs`
- `scripts/preflight-updater-nullability.py` (new)
- this claim file

## Contract

- Model genuinely absent UI state, in-flight tasks, events, release/manifest data and untrusted GitHub JSON as nullable; do not suppress warnings with `!`, `#nullable disable`, or dummy non-null defaults.
- Preserve strict SemVer/channel behavior, GitHub host allowlist, signed-manifest gating, WinVerifyTrust, publisher-thumbprint pinning, graceful close/no-kill behavior and detached updater semantics.
- Validate using installed BricsCAD V25 references plus updater security/product-version gates. Do not run GitHub Actions or publish releases.

## Coordination

- The parent GitHub updater claim is `RELEASED`; Authenticode verification is `COMPLETED`.
- The active product-SemVer claim owns only `scripts/new-v25-update-manifest.ps1`, `scripts/update-v25.ps1`, and its new product-version gate; those files are excluded here.
- Quantity Insight, Workspace and Wall Quantity compile errors remain under their separate active claims and are excluded.

## Completion evidence

- Nullable contracts now truthfully cover Update Center state/window lifetime, coordinator dispatcher/in-flight/events, optional release/manifest data, nullable SemVer parse output, current-process executable discovery and every untrusted GitHub JSON field.
- Scheduling snapshots one validated non-null release/manifest before handoff; GitHub page/manifest URIs retain HTTPS + `github.com` validation; WinVerifyTrust and signer pinning remain unchanged.
- `scripts/preflight-updater-nullability.py`: PASS and forbids nullable suppression/dummy non-null JSON contracts.
- `scripts/preflight-auto-update.py`, `scripts/preflight-update-product-version-binding.py`, and `scripts/preflight-v25-runtime-diagnostics.py`: PASS.
- The installed BricsCAD V25 `Release|x64` build reports no updater errors after this patch and proceeds to one separately active Right Panel duplicate-handler error.
- Scoped `git diff --check`: PASS. No Actions, release publication, signed update or private drawing was used.
