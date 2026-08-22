# Agent Work Claim — exact signed release runtime qualification

- Claim ID: `RELEASE-SIGNED-RUNTIME-ARTIFACT-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T21:38:30+07:00`
- Released: `2026-08-11T21:42:00+07:00`
- Baseline main SHA: `b658935a66b4107f6a6ee4c827fb075d59ab5ae7`
- Priority: close the gap between the V25 binary runtime-tested by the manual release workflow and the Authenticode-signed binary actually published to users.

## Verified defect

The manual release workflow ran the real BricsCAD V25 NETLOAD/runtime gate immediately after building the adapter, before packaging and Authenticode signing. It later signed `QS3D.BricsCAD.V25.dll` and published that signed DLL inside the finalized ZIP. Authenticode signing changes PE bytes, so the exact published executable payload was not the exact binary that passed the runtime step.

For a stable release, source policy requires both runtime qualification and signing, but the two gates were applied to different byte identities.

## Reserved scope

- `.github/workflows/release-v25.yml`
- `scripts/preflight-signing.py`
- `scripts/preflight-release-signed-runtime.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file

## Completed changes

### Exact signed runtime workflow — `f8032e762077e33d69701f49a994c66cb3875565`

- split runtime validation into two explicit branches:
  - `run_runtime && !sign_package`: unsigned prerelease runtime uses the build output and stores evidence under `artifacts/bricscad-v25-runtime-unsigned`;
  - `run_runtime && sign_package`: signed release runtime runs only after Authenticode sign -> verify -> finalize and NETLOAD-tests `dist\QS3D-BricsCAD-V25\QS3D.BricsCAD.V25.dll`, the exact signed staged plugin payload used by the release package;
- signed runtime evidence is stored separately under `artifacts/bricscad-v25-runtime-signed`;
- stable releases remain forced to both `run_runtime=true` and `sign_package=true`, so their mandatory runtime gate now targets the signed publish payload;
- release notes identify whether runtime evidence belongs to the exact signed payload or an unsigned preview;
- manual-only dispatch, explicit confirmation, draft-first asset publication and signed update-manifest flow remain intact.

### Existing signing gate reconciliation — `f9a1ac269332b5efba70669415abda358a8af491`

Re-reading the existing signing regression gate after the workflow change exposed an expected static regression: it pinned the old literal `if: ${{ inputs.run_runtime }}`. The claim was extended before editing this file. The gate now:

- requires both split runtime conditions;
- requires unsigned runtime before package/signing;
- requires signed runtime after finalize and before checksum/publish;
- requires the signed runtime block to target `dist\QS3D-BricsCAD-V25\QS3D.BricsCAD.V25.dll` and rejects fallback to the pre-sign `bin` output;
- preserves certificate-store, SHA-256, timestamp, draft publication and stable-release policy checks.

### Focused exact-artifact gate — `45a24544aaf73987272c2ab4d7cb5e34b5503304`

Added auto-discovered `scripts/preflight-release-signed-runtime.py` to independently lock sign -> verify -> finalize -> signed-runtime -> manifest -> publish ordering and the exact staged DLL path.

### Runbook — `f0c6ed7cca5d9545f3ebe97e1a28ad9b0f6c5198`

Updated `docs/MANUAL-BUILD-RELEASE.md` so operators know:

- `sign_package` is a required release input and mandatory for stable releases;
- stable runtime evidence must come from the finalized signed staged plugin payload;
- unsigned prerelease runtime is a separate path;
- signed auto-update uses schema-v2 product-version binding;
- source hardening is not equivalent to an actual owner-approved signed runtime PASS.

## Validation / coordination

- Re-read the current workflow and the existing signing gate before and after edits.
- Compare from `f0c6ed7cca5d9545f3ebe97e1a28ad9b0f6c5198` to then-current `main` reported `behind_by: 0`; the only later compared change was unrelated Core smoke registration.
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched and no GitHub Release was published by this lane.
- Static workflow/preflight source was committed, but this connector session did not execute an owner-approved signed release run. Therefore no new signed V25 runtime PASS is claimed.

## Result

The stable release workflow is now designed to runtime-test the exact Authenticode-signed QS3D plugin payload that is staged into the published package, closing the previous pre-sign-vs-published-binary evidence gap. Actual proof remains the future result of an explicitly owner-dispatched manual release run.
