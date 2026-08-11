# Agent Work Claim — exact signed release runtime qualification

- Claim ID: `RELEASE-SIGNED-RUNTIME-ARTIFACT-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T21:38:30+07:00`
- Updated: `2026-08-11T21:40:00+07:00`
- Baseline main SHA: `b658935a66b4107f6a6ee4c827fb075d59ab5ae7`
- Priority: close the gap between the V25 binary runtime-tested by the manual release workflow and the Authenticode-signed binary actually published to users.

## Verified defect

The manual release workflow currently runs the real BricsCAD V25 NETLOAD/runtime gate immediately after building the adapter, before packaging and Authenticode signing. It later signs `QS3D.BricsCAD.V25.dll` and publishes that signed DLL inside the finalized ZIP. Authenticode signing changes the PE file bytes, so the exact published executable payload is not the exact binary that passed the current runtime step.

For a stable release, source policy requires both runtime qualification and signing, but the two gates are currently applied to different byte identities.

## Reserved scope

- `.github/workflows/release-v25.yml`
- `scripts/preflight-signing.py` (existing release/signing gate; narrow compatibility/order update)
- `scripts/preflight-release-signed-runtime.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md` (narrow runbook correction)
- this claim file

## Scope-extension reason

After the workflow ordering change was committed, re-reading the existing signing regression gate proved that `scripts/preflight-signing.py` pins the old literal `if: ${{ inputs.run_runtime }}`. The safer split conditions `inputs.run_runtime && !inputs.sign_package` and `inputs.run_runtime && inputs.sign_package` would therefore make the aggregate preflight fail even though runtime coverage is stricter. This gate is now explicitly reserved so it can be reconciled before the lane closes.

## Excluded / coordination scope

- No GitHub Actions dispatch or release publication.
- No updater C#, update/manifest/package/signing PowerShell implementation changes.
- No signing-key/certificate handling changes.
- No Core, Quantity, Workspace, Direct Draw, Ribbon/UI or other active feature lanes.

## Planned fix

1. Keep unsigned prerelease compatibility: if runtime is requested while `sign_package=false`, runtime-test the built plugin as today.
2. When `sign_package=true`, defer the release runtime gate until after signing verification/finalization and run it against `dist/QS3D-BricsCAD-V25/QS3D.BricsCAD.V25.dll`, the exact signed plugin payload placed in the release package.
3. Stable releases remain forced to `run_runtime=true` and `sign_package=true`, so their mandatory runtime evidence becomes evidence for the published signed DLL rather than the pre-sign build output.
4. Use a distinct signed-runtime artifact folder so evidence clearly identifies which binary was exercised.
5. Reconcile `preflight-signing.py` with the split runtime conditions and require signed-runtime ordering after finalize and before publish.
6. Add a focused static release gate proving stable signed releases cannot publish while relying only on a pre-sign runtime probe.
7. Update the manual release runbook to state that signed releases runtime-test the signed staged payload.

## Validation / release conditions

- Re-read current workflow/gates before writes and preserve manual-only triggers/publication gates.
- Verify workflow ordering by source and focused preflight; do not dispatch the workflow in this session.
- Re-fetch committed workflow/gates and verify ancestry with `behind_by: 0`.
- Actual signed runtime PASS remains dependent on a future owner-approved manual release run; source hardening must not be reported as such.
- Mark `RELEASED` only after workflow + gates + runbook are committed on `main`.
