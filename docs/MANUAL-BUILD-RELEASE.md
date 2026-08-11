# Manual BricsCAD V25 build and release runbook

Updated 2026-08-11.

## Policy

This runbook builds and releases the **QS3D BricsCAD V25 plugin package**. The expected product artifacts are the adapter/Core DLLs plus install/update/checksum/sample helpers loaded by BricsCAD; a standalone `QS3D.exe` is not part of this release contract. See `docs/PRODUCT-BOUNDARY.md`.

QS3D does **not** use automatic GitHub CI/CD.

Every workflow in `.github/workflows/` must remain `workflow_dispatch` only. Every executable job is additionally hard-guarded to `github.event_name == 'workflow_dispatch'`. Commits, pushes, pull requests, merges, documentation updates, source fixes, reviews and `continue all` requests must leave GitHub Actions idle.

A workflow may be dispatched only after the repository owner explicitly requests the run. See `CI_POLICY.md`.

## Available manual workflows

- `ci.yml` — Core/static validation.
- `bricscad-v25.yml` — V25 plugin integration build/runtime evidence.
- `curved-opening.yml` — focused curved-opening validation.
- `geometry-extensions.yml` — focused geometry-extension validation.
- `project-data-gate.yml` — Zone/Floor/Family/Material/Project Tools/project-assignment-integrity validation.
- `schedule-gate.yml` — Schedule Hub / Room Finish / Material / Door-Opening schedule-export validation.
- `release-v25.yml` — complete owner-approved V25 plugin build/package/GitHub Release flow.

Focused workflows also run `scripts/preflight-ci-manual-only.py` so policy drift is detected when the owner explicitly dispatches one.

## Preferred release workflow

Use `.github/workflows/release-v25.yml` only after the owner explicitly requests a release.

Required inputs:

- `release_tag` — semantic-style tag such as `v0.1.0` or `v0.1.0-rc.1`;
- `confirm_release` — must be exactly `RELEASE` or the release job is skipped;
- `run_runtime` — normally `true` for a release candidate/final release when the licensed interactive V25 runner is available; stable releases require it;
- `sign_package` — Authenticode-sign/timestamp the release payload; stable releases require it;
- `prerelease` — `true` for RC/beta builds, otherwise `false`.

The workflow deliberately has no automatic/event-driven trigger. Its release job also checks `github.event_name == 'workflow_dispatch'` and `confirm_release=RELEASE` before execution.

## What the manual release workflow does

1. checks out the explicitly selected commit;
2. validates the release tag and strict manual CI policy;
3. runs generic and auto-discovered feature preflights;
4. compiles `QS3D.Core` in Release;
5. runs deterministic Core smoke tests;
6. verifies `BRICSCAD_V25_DIR` and required licensed V25 runtime files;
7. compiles `QS3D.BricsCAD.V25` Release/x64 **plugin DLL** against the installed V25 assemblies;
8. for an explicitly unsigned prerelease with `run_runtime=true`, runtime-tests the unsigned build output before packaging;
9. runs `scripts/package-v25.ps1` against `bin/x64/Release/net48` and validates release-tag/package version binding;
10. when `sign_package=true`, Authenticode-signs the executable payload, verifies publisher/timestamp, and finalizes the signed package/ZIP;
11. when both `run_runtime=true` and `sign_package=true`, runs the real V25 NETLOAD/runtime gate against **`dist/QS3D-BricsCAD-V25/QS3D.BricsCAD.V25.dll`**, the exact signed plugin payload staged into the published package;
12. for signed releases, creates the schema-v2 `QS3D-BricsCAD-V25.update.json` manifest after the signed-runtime gate succeeds;
13. creates the release ZIP checksum and uploads package/runtime evidence artifacts;
14. creates a draft GitHub Release, uploads/verifies expected assets, then publishes the draft only after all required preceding gates succeed.

A stable release is forced to `run_runtime=true` and `sign_package=true`. Therefore stable runtime evidence must refer to the finalized signed plugin payload, not only the pre-sign build output. Authenticode signing changes PE bytes even when managed code is unchanged, so the signed staged DLL is the release binary that matters for publication evidence.

The package script generates `COMMANDS.txt` from current `[CommandMethod]` source declarations, verifies required QS3D DLLs, includes synthetic sample fixtures/install-update helpers and excludes BricsCAD-owned runtime assemblies. It does not expect a standalone QS3D executable.

## Required runner

The release workflow uses:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

The machine must have a licensed BricsCAD V25 installation and repository variables configured for:

- `BRICSCAD_V25_DIR`
- `BRICSCAD_V25_PROFILE` when runtime validation uses a dedicated profile;
- `QS3D_SIGNING_CERT_THUMBPRINT` and `QS3D_TIMESTAMP_SERVER` when `sign_package=true`.

Runtime/screenshot validation requires an interactive Windows session.

For provisioning a dedicated Windows runner from a local/cached MSI, `scripts/install-bricscad-v25.ps1` treats the filename only as an advisory. Before invoking `msiexec`, the helper verifies the optional requested SHA-256, enforces the configured Authenticode publisher policy, reads the MSI Property table, requires ProductName to identify BricsCAD, and requires ProductVersion major 25. A renamed Bricsys-signed MSI for another BricsCAD major version must therefore fail before installation. This source-side identity check does not replace licensed first-launch/runtime qualification.

## Project/DWG readiness before publication

`QS3DRELEASECHECK` is a project-level release-readiness command. On representative release drawings it checks semantic/source/generated health, dependency-cycle health, policy-safe ownership, all current generated rebar families including Foundation mesh, generated-rebar mode semantics, Curtain live state/stale state and BOM/live-solid guards.

A blank drawing is not meaningful private-DWG release evidence. `QS3DRELEASECHECK` should complement—not replace—the licensed V25 representative-DWG runtime regression.

## Transactional install and secure update qualification

The source-side install/update path is hardened, but a production release should exercise it with an actually signed package before publication:

- per-user DemandLoad installation/replacement snapshots the targeted prior payload and registry registration;
- a forced replacement of an existing `InstallDirectory` must first prove that directory is a canonical QS3D BricsCAD V25 installation from its package metadata plus managed DLL assembly/product identities; `-Force` alone never authorizes moving/deleting an arbitrary existing file or foreign directory;
- uninstall file removal always requires canonical QS3D V25 metadata plus matching plugin/Core assembly and ProductVersion identity; `-Force` only authorizes an intentional verified custom path outside the default QS3D LocalAppData scope and never bypasses ownership validation;
- if a replacement fails, the installer restores the previous files/registration; if a first install fails, partial new state is removed;
- signed-package finalization requires a `.zip` output outside `PackageDirectory`, then revalidates `QS3D / BricsCAD V25 x64`, metadata AssemblyVersion/productVersion and exact plugin/Core managed identities after executable signature verification and before mutating metadata, regenerating hashes, deleting any prior output ZIP or rebuilding the release ZIP;
- the updater accepts only the intended HTTPS/package-host path and verifies archive/path/size limits, SHA-256 and Authenticode signer expectations;
- update manifests use schema 2 and bind `productVersion` to package metadata and the signed plugin ProductVersion, while AssemblyVersion remains an independent binary/package check;
- product SemVer must advance monotonically; equal-AssemblyVersion prerelease upgrades are allowed only when product SemVer is strictly newer, and replay/downgrade is rejected;
- expected-version mismatch, package substitution or replay/relabel conditions must fail before install;
- installer/updater must never lower BricsCAD `SECURELOAD`.

Production certificate/key custody, timestamping and publication infrastructure remain external release operations. Source-side verification does not itself prove that a production signing key was used correctly.

## Release safety rules

- Never publish from an ambiguous moving head. Resolve the exact commit/tag first.
- Never dispatch a release merely because source landed; owner approval is a separate action.
- Never mark a signed release runtime-verified unless the V25 runtime step actually completed successfully against the signed staged plugin payload that is packaged for publication.
- Never silently skip a failed preflight/build/runtime step to force a release.
- Never replace an existing release tag from this workflow.
- Keep `confirm_release=RELEASE` as an explicit publication gate.
- Keep `scripts/preflight-ci-manual-only.py` in the aggregate gate.
- Never package BricsCAD-owned DLLs, BLT/vendor source, customer/private DWGs, signing secrets or certificates.
- Never relabel the plugin ZIP/DLL delivery as a standalone QS3D CAD application.
- For a production candidate, exercise upgrade rollback from a known previous version and reject an intentionally mismatched/relabelled update package before publication.

The only repository DWG/DXF fixtures allowed by source policy are the explicitly reviewed synthetic samples under `samples/generated`.

## When the owner says “build” but not “release”

Use `bricscad-v25.yml` or the appropriate focused manual workflow. Do not publish a GitHub Release unless the request explicitly asks for release/publish.

## When the owner says “build and release”

Use the exact requested commit/tag, run `release-v25.yml` manually, keep runtime validation enabled when the licensed V25 runner is available, and report separately:

- source/preflight result;
- Core build/smoke result;
- V25 plugin adapter build result;
- runtime/NETLOAD result, explicitly identifying whether the tested payload was unsigned preview or finalized signed release DLL;
- representative-DWG / `QS3DRELEASECHECK` result when performed;
- install/update rollback + signed-manifest/product-version-binding qualification when performed;
- package SHA-256;
- GitHub Release tag and attached artifact names.

Source implementation progress and static review remain distinct from CI/runtime proof.

See `docs/PRODUCT-BOUNDARY.md` and `docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md` for the product boundary, latest deep source review and remaining product/runtime gates.
