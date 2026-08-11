# Work claim — update manifest output isolation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-update-manifest-output-isolation`
- Registered: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `87f28b33cc89cb7f7ab8576606ce1a5328ec31a3`
- Priority: owner-requested continue-all review; close a destructive manifest-generation boundary where arbitrary `OutputPath` can alias `PackageZip` or a file inside signed staging and later be overwritten by JSON after that exact artifact was verified.

## Reserved scope

Harden `scripts/new-v25-update-manifest.ps1` so manifest output must be a `.json` path outside `PackageDirectory` and must not equal `PackageZip`. Reject unsafe output before signer/staging/ZIP verification and before any output mutation. Add an auto-discovered source/model regression for this manifest helper and align release documentation.

## Expected surfaces

- `scripts/new-v25-update-manifest.ps1`
- `scripts/preflight-update-manifest-output-isolation.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- `UpdateCoordinator.cs` and the ACTIVE generation-safe publication lane; `SecureUpdateLauncher.cs`, `GitHubReleaseClient.cs`, updater selection/network behavior, package/finalizer/signing semantics, installer/uninstaller, workflow dispatch/publication, `src/**`, `tests/**` and licensed V25 runtime.

## Validation plan

- Normalize package root, package ZIP and output path before verification/mutation.
- Require `.json` output; reject output equal to package ZIP or lexically within signed staging.
- Preserve sibling `dist/QS3D-BricsCAD-V25.update.json` and arbitrary safe external JSON parent paths.
- Regression model covers sibling manifest PASS, nested staging manifest FAIL, package-ZIP alias FAIL, staged payload alias/non-JSON FAIL and similarly-prefixed sibling tree PASS.
- Pin output isolation before signer/identity/ZIP verification and before `ShouldProcess`/`Set-Content`.
- No GitHub Actions dispatch/re-run.

## Coordination

The ACTIVE updater generation-publication claim explicitly states `manifest generation` is excluded. No current claim was found for `new-v25-update-manifest.ps1`. This lane is release-helper-only and does not touch updater runtime code.

## Completion condition

Manifest generation cannot overwrite the package ZIP or signed staging and requires a JSON output path before any artifact verification/publication mutation, regression/docs are on `main`, and this claim is `COMPLETED`.
