# Work claim — Curtain Frame config fingerprint canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-fingerprint-canonicality`
- Registered: `2026-08-12T11:15:00+07:00`
- Baseline main SHA: `41ec60f899c8aff4f73b9896299050c5579399a5`
- Priority: P1 — generated Curtain Frame config fingerprints must preserve the exact writer-owned SHA-256 spelling.
- Task Key: `CORE-CURTAIN-FRAME-FINGERPRINT-CANONICALITY`

## Confirmed defect

`CurtainWallFrameFingerprint.Compute(...)` returns SHA-256 as exactly 64 lowercase hex characters via `value.ToString("x2", CultureInfo.InvariantCulture)`. Both line and path Curtain Frame builders persist that exact returned string unchanged into `GeneratedCurtainFrameConfigFingerprint`. `GeneratedCurtainFrameHealthService.ValidateConfigFingerprint(...)` currently compares the recomputed fingerprint against `storedFingerprint.Trim()` with `StringComparison.OrdinalIgnoreCase`, so padded or uppercase aliases can pass stale/config health even though no writer emits those spellings.

## Non-overlap check

Recent claim/commit search found no Curtain Frame config-fingerprint canonicality lane. The earlier signed-zero fingerprint fix changes fingerprint input canonicalization, not persisted fingerprint-string validation. Completed Curtain Frame handle, mode, source-kind, geometry and integer canonicality lanes own different metadata.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for config-fingerprint canonicality
- this claim file

Do not modify fingerprint computation, Curtain Frame builders, handles/counts/mode/source-kind/geometry metadata, native ownership/XData, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- If a stored non-empty fingerprint is semantically equal to the recomputed fingerprint under trim/case-folding but is not exactly the writer-owned lowercase 64-hex text, emit `CURTAIN_FRAME_CONFIG_FINGERPRINT_NON_CANONICAL` as `HealthSeverity.Error`.
- Existing `CURTAIN_FRAME_CONFIG_STALE` remains the diagnostic when the normalized stored fingerprint differs from the recomputed value.
- Missing fingerprint retains `CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING` precedence.
- Existing config-invalid handling remains unchanged.
- Exact writer-owned lowercase fingerprint preserves existing behavior.

## Completion condition

Uppercase/padded fingerprint aliases are fail-visible without changing missing/stale/config-invalid semantics, focused smoke coverage pins alias/stale/missing/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
