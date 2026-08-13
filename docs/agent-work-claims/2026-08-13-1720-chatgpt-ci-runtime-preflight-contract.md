# Work claim — Runtime diagnostics CI preflight contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-ci-release-v25`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `52c946738bb9423d58e6fff18eb8582072f4a19c`
- Priority: Repair the four deterministic feature-source guards blocking the owner-authorized V25 cloud release CI loop after the stale-process diagnostics hardening.

## Reserved scope

Align static preflight assertions with the stronger stale-DLL/runtime diagnostics semantics already present on `main`, without weakening runtime, customer-release, V26, Authenticode-truthfulness, or read-only guarantees.

## Expected surfaces

- `scripts/preflight-runtime-diagnostics-readonly.py`
- `scripts/preflight-runtime-diagnostics-truth.py`
- `scripts/preflight-bricscad-v26.py`
- `scripts/preflight-customer-release.py`
- This claim file for close-out.

`src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs` is intentionally read-only in this lane because an independent ACTIVE display-clarity claim owns that source. `ReleaseReadinessCommands.cs` may be read as authoritative evidence but is not reserved for edits.

## Contract being repaired

- Read-only qualification must pin the stronger `diskVersionMatches` + `diskFingerprintMatches` fail-closed success predicate, not the older weaker exact line.
- Truthfulness must keep the explicit statement that runtime diagnostics only report metadata and that cryptographic publisher/timestamp verification belongs to the signed installer/release gate; it must not require a duplicate legacy wording fragment.
- Customer release must validate the actual `QS3DRELEASECHECK` command registration in `ReleaseReadinessCommands.cs`, rather than requiring that command name to remain embedded in the runtime PASS prose.
- V26 shared-source qualification must pin host-major selection plus stale-process/disk-fingerprint semantics, rather than a legacy PASS-message instruction string.

## Excluded scope

- No edits to runtime diagnostics source while the display claim is ACTIVE.
- No relaxation of release identity, package signing, runtime-major, x64, stale-process, or project read-only checks.
- No local BricsCAD/AutoCAD/BLT3D work.
- No automatic workflow trigger changes.

## Validation plan

Run/reason through the four focused Python source guards against current `main`, then the aggregate preflight registration path. Read back all pushed scripts and confirm the stronger assertions are present. A fresh owner-authorized workflow dispatch is required for final CI proof on the new SHA.

## Completion condition

All four blocking preflights are aligned to current stronger source semantics, pushed to `main`, read back, and this claim is closed with implementation/evidence SHAs. CI is not called green until a fresh workflow run on a descendant SHA succeeds.