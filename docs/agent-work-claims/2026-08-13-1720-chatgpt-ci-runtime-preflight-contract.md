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

- `65a2760b1a136a6d0c896e81d979788b2743b8af` — `fix(preflight): pin stronger runtime diagnostics predicate`
- `325a34fe5d228593867eb101d9eb137292ad72ea` — `fix(preflight): keep runtime signature truth contract semantic`
- `736113bbc4bb90c75e84a706b8a6b5109419b325` — `fix(preflight): validate release command at authoritative source`
- `e9ad714cdef6ad0734b6aebf4123c2148dd712f4` — `fix(preflight): align V26 guard with stale-binary hardening`

## Validation evidence

All four scripts were read back from `main` after their writes. The runtime diagnostics source still contains the stronger compile-selected V25/V26 host-major, x64, package-version, on-disk version, and binary-fingerprint predicate. `ReleaseReadinessCommands.cs` still owns `[CommandMethod("QS3DRELEASECHECK", CommandFlags.Modal)]` and resolves project state through `TryGetReadOnly`.

GitHub ancestry check confirmed current `main` was 16 commits ahead of `e9ad714cdef6ad0734b6aebf4123c2148dd712f4`, with that implementation commit as the merge base, so the repaired V26/preflight lane was preserved through concurrent work.

The source/preflight repair is complete. Full workflow success is deliberately not claimed here: a fresh owner-authorized manual `release-v25-cloud.yml` dispatch on a descendant SHA is required for CI proof because rerunning run #122 would re-test its old event/head SHA.