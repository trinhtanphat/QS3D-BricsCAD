# CI strategy

## Repository policy

The repository-wide source of truth is `CI_POLICY.md`. GitHub Actions on `main` are owner-controlled and **manual-only**:

- every workflow must use `workflow_dispatch` as its only trigger;
- every executable job must hard-guard `github.event_name == 'workflow_dispatch'`;
- commits, pushes, merges, fixes, reviews, documentation changes and `continue all` do not authorize an Actions run;
- preparing or editing a workflow is not permission to dispatch it;
- release workflows additionally require explicit `confirm_release=RELEASE`.

`scripts/preflight-ci-manual-only.py` enforces these rules across all workflow YAML files and is auto-discovered by `scripts/preflight-all.py`.

Because multiple agents may write concurrently, refresh current `main` before shared-file writes and never overwrite another lane.

## Manual workflows

### Core/static

`.github/workflows/ci.yml`

- hosted Windows runner;
- generic + auto-discovered source preflights;
- Core Release build;
- deterministic Core smoke tests;
- no BricsCAD installation required.

### BricsCAD V25 integration

`.github/workflows/bricscad-v25.yml`

- self-hosted Windows x64 runner labeled `bricscad-v25`;
- `BRICSCAD_V25_DIR` external host references;
- V25 adapter build from `bin/x64/Release/net48`;
- optional licensed NETLOAD/runtime evidence when explicitly dispatched.

### BricsCAD V26 integration

`.github/workflows/bricscad-v26.yml`

- self-hosted Windows x64 runner labeled `bricscad-v26`;
- .NET 8 SDK + Microsoft Windows Desktop Runtime 8.x;
- `BRICSCAD_V26_DIR` must resolve a `bricscad.exe` with file major 26 plus V26 `BrxMgd.dll` / `TD_Mgd.dll`;
- V26 adapter build from `bin/x64/Release/net8.0-windows`;
- optional licensed V26 NETLOAD/runtime evidence through `scripts/test-bricscad-v26-runtime.ps1`.

### Focused source gates

Focused workflows such as curved-opening, geometry, project-data and schedule gates remain manual-only and also execute the strict manual-CI policy preflight.

## Manual release workflows

### V25

`.github/workflows/release-v25.yml`

- owner-dispatched only;
- requires `confirm_release=RELEASE`;
- builds Core + V25, packages V25 assets, applies signing/runtime gates according to release type and publishes only after its release-integrity checks pass.

See `docs/MANUAL-BUILD-RELEASE.md`.

### V26

`.github/workflows/release-v26.yml`

- owner-dispatched only;
- requires `confirm_release=RELEASE`;
- stable release requires `run_runtime=true` and `sign_package=true`;
- builds Core + `QS3D.BricsCAD.V26` on .NET 8;
- packages only `QS3D-BricsCAD-V26` assets;
- verifies/finalizes Authenticode-signed payloads when signing is enabled;
- runs the exact V26 runtime gate against the signed staged DLL for a stable signed release;
- generates `QS3D-BricsCAD-V26.update.json` only for the V26 signed package;
- creates a draft GitHub Release and verifies exact expected V26 asset names before publication.

See `docs/MANUAL-BUILD-RELEASE-V26.md` and `docs/LOCAL-V26-QUALIFICATION.md`.

## Runner matrix

| Lane | Labels | Host variable | Managed target |
| --- | --- | --- | --- |
| V25 | `self-hosted`, `windows`, `x64`, `bricscad-v25` | `BRICSCAD_V25_DIR` | `net48` |
| V26 | `self-hosted`, `windows`, `x64`, `bricscad-v26` | `BRICSCAD_V26_DIR` | `net8.0-windows` |

Optional host profiles are `BRICSCAD_V25_PROFILE` and `BRICSCAD_V26_PROFILE` respectively. Runtime validation requires an interactive licensed Windows session.

## Build-surface isolation

- `QS3D.sln` remains the established V25-oriented solution.
- `QS3D.V26.sln` contains Core + V26 + Core SmokeTests and maps the V26 adapter to x64.

This avoids requiring both proprietary host-major installations merely to build one adapter lane.

## Update-channel isolation

V25 and V26 can share the repository's GitHub release history, but automatic update discovery is host-major isolated before latest selection:

- V25 release membership requires `QS3D-BricsCAD-V25.update.json`;
- V26 release membership requires `QS3D-BricsCAD-V26.update.json`.

The subsequent manifest/package/signature checks remain host-specific. Do not publish a V25 ZIP/manifest into the V26 lane or vice versa.

## Static/local review versus CI/runtime

Repository/source preflights may be executed locally without dispatching Actions. A static PASS is not a GitHub Actions PASS and is not licensed BricsCAD runtime proof.

Similarly, source presence of V26 packaging, signing, updater or release tooling does not prove:

- a real code-signing private key was used;
- clean-machine install/update/uninstall passed;
- BricsCAD V26 NETLOAD/UI/native geometry passed;
- representative customer/release DWGs passed.

Only report those results after the corresponding exact candidate payload was actually exercised.

## Owner-approved release gate

When the owner explicitly asks for a release:

1. resolve the exact commit/tag;
2. choose the host-major release workflow (`release-v25.yml` or `release-v26.yml`);
3. dispatch manually with `confirm_release=RELEASE`;
4. do not bypass source/Core/host build/runtime/signing/package checks;
5. publish only the host-major assets produced by that lane.

`QS3DRELEASECHECK` should be run on representative project data during runtime qualification; blank-DWG checks do not replace representative-DWG evidence.

Never source-control or publish proprietary BricsCAD runtime DLLs, signing secrets or private/customer CAD/project data.
