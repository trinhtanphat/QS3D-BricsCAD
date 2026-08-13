# Work claim — Runtime diagnostics CI preflight contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-ci-release-v25`
- Registered: `2026-08-13T17:20:00+07:00`
- Baseline main SHA: `52c946738bb9423d58e6fff18eb8582072f4a19c`
- Completed: `2026-08-13T17:25:00+07:00`
- Priority: Repair the four deterministic feature-source guards blocking the owner-authorized V25 cloud release CI loop after the stale-process diagnostics hardening.

## Reserved scope

Align static preflight assertions with the stronger stale-DLL/runtime diagnostics semantics already present on `main`, without weakening runtime, customer-release, V26, Authenticode-truthfulness, or read-only guarantees.

## Expected surfaces

- `scripts/preflight-runtime-diagnostics-readonly.py`
- `scripts/preflight-runtime-diagnostics-truth.py`
- `scripts/preflight-bricscad-v26.py`
- `scripts/preflight-customer-release.py`
- This claim file for close-out.

`src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs` remained read-only in this lane because an independent ACTIVE display-clarity claim owned that source. `ReleaseReadinessCommands.cs` was used as authoritative evidence only.

## Contract repaired

- Read-only qualification now pins the stronger `diskVersionMatches` + `diskFingerprintMatches` fail-closed success predicate, not the older weaker exact line.
- Truthfulness keeps the explicit statement that runtime diagnostics only report metadata and that cryptographic publisher/timestamp verification belongs to the signed installer/release gate; the preflight no longer requires a duplicate legacy wording fragment.
- Customer release validates the actual `QS3DRELEASECHECK` command registration and read-only behavior in `ReleaseReadinessCommands.cs`, rather than requiring that command name to remain embedded in runtime PASS prose.
- V26 shared-source qualification pins host-major selection plus stale-process/disk-fingerprint semantics, rather than a legacy PASS-message instruction string.

## Implementation commits

- `65a2760128b2943a5d110396387728aaa0f22e9b` — `fix(preflight): pin stronger runtime diagnostics predicate`
- `325a34fad9721b44fc48022c7d3566a93b141c1f` — `fix(preflight): keep runtime signature truth contract semantic`
- `73611391d7872f14e8f6804323ddd4aa6d8b0902` — `fix(preflight): validate release command at authoritative source`
- `e9ad714a820da825593c8d2ca81d2b2628df9528` — `fix(preflight): align V26 guard with stale-binary hardening`

## Validation evidence

All four scripts were read back from `main` after their writes. The runtime diagnostics source still contains the stronger compile-selected V25/V26 host-major, x64, package-version, on-disk version, and binary-fingerprint predicate. `ReleaseReadinessCommands.cs` still owns `[CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]` and resolves project state through `TryGetReadOnly`.

The source/preflight repair is complete. Full workflow success is deliberately not claimed here: a fresh owner-authorized manual `release-v25-cloud.yml` dispatch on a descendant SHA is required for CI proof because rerunning run #122 would re-test its old event/head SHA.