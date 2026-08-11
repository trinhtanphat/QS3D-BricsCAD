# Work claim — BOM quantity key canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bom-quantity-key-20260812-0027`
- Registered: `2026-08-12T00:27:00+07:00`
- Completed: `2026-08-12T00:30:00+07:00`
- Baseline main SHA observed before registration: `d7eb1dda291328e8c24eb2ec18fba564898120a8`
- Claim commit: `46f1c22dcb3919b10df7bf2e9bb2dd23f1cd6eb6`
- Source fix commit: `b5579e12bf871a5c01f9316fdcb5a28a56f1acdc`
- Regression commit: `db883e2575b1d8d7c95cac8e04e831c9e9fc2d1a`
- Priority: P2 source-proven release-integrity regression hardening

## Reserved scope

Fix the Core BOM release guard mismatch where `QsdbProjectStore` rejects quantity dictionary keys with leading/trailing whitespace, while `BomReleaseGuardService` previously reported only blank quantity keys. `ProjectElement.Quantities` is publicly mutable, so a key such as `" NetConcreteM3 "` can exist in memory; `ProjectQuantityReportBuilder` performs canonical quantity-name lookup and can therefore treat that malformed key as missing/zero while the release guard fails to raise the existing `BOM_QUANTITY_KEY_INVALID` blocker.

## Implemented surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file

## Implemented fix

- Reused the existing `BOM_QUANTITY_KEY_INVALID` error code for blank quantity keys and non-blank keys with surrounding whitespace.
- The issue remains `HealthSeverity.Error` and is attached to the owning semantic element, so malformed quantity identity is an explicit BOM release blocker.
- Finite-value, report grouping, provenance, generated-handle liveness, and exception-redaction behavior remain unchanged.
- Added `NonCanonicalQuantityKeyBlocksRelease()` to the already-registered `BomReleaseGuardSmoke.Run()` suite. The regression directly mutates the public quantity dictionary with `" NetConcreteM3 "` and requires an Error-level `BOM_QUANTITY_KEY_INVALID` for that element.

## Explicit exclusions honored

- No `ProjectQuantityReportBuilder` behavior changes.
- No QSDB persistence/schema changes.
- No quantity calculation/rule semantics changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- The claim was committed separately and verified as an ancestor of current `main` before substantive writes.
- Re-fetched exact current source/test blobs after the preceding BOM exception-redaction owner completed and used blob SHA guards for both writes.
- Re-read current `main` after implementation and verified the source contains the canonical-whitespace guard and the existing smoke suite invokes `NonCanonicalQuantityKeyBlocksRelease()`.
- Verified the preceding BOM exception-redaction behavior remains present; no stale overwrite, reset or force push was used.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

The preceding BOM diagnostics exception-redaction claim completed before this claim was registered. This batch then reserved only quantity-key canonicality in the same guard and its existing smoke, preserving the earlier redaction behavior.

## Completion condition

Completed. BOM release diagnostics now reject padded quantity keys that QSDB persistence already rejects, focused regression coverage is committed on `main`, current source/test were re-read, and this claim records the exact implementation SHAs and actual validation boundary.
