# Work claim — Interchange sourceHandles numeric CAD identity

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12T20:05:00+07:00`
- Baseline main SHA: `9e65b58d40b0d0937c4de4dc7dbfbd6bbb55838b`
- Priority: evidence-driven remote-safe Core interchange hardening

## Reason

`ProjectInterchangeJsonValidator.ValidateSourceHandles()` previously deduplicated `sourceHandles` with raw case-insensitive text identity. Core ownership and `SourceHandleResolver` use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity()` so CAD numeric aliases such as `A`, `0A`, and `0x000a` identify the same handle. An interchange payload could therefore pass validation with duplicate aliases and only become ambiguous/conflicting after entering the shared ownership/resolution boundary.

## Reserved scope

Use the existing shared generated-handle numeric identity only for duplicate detection inside interchange `sourceHandles`. Preserve the original trimmed token for diagnostics and payload semantics, preserve malformed/non-hex textual identities, preserve whitespace/length validation, and do not canonicalize or rewrite exported JSON values.

## Landed evidence

- Claim registration: `ace7972b60f693bfc97d00aba4cac659c86e3be3`.
- Source fix: `c2cd0dbd3fb68becc6ea9c3bb434594f3270d694`.
  - `ValidateSourceHandles()` now deduplicates with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle)`.
  - Commit readback shows a one-line product diff only; diagnostics and payload values remain unchanged.
- Focused regression: `aa8f84e2c908f7323018dd74def8440eb67a098f`.
  - Covers `A` + `0A` => `SOURCE_HANDLE_DUPLICATE`.
  - Covers `A` + `0x000a` => `SOURCE_HANDLE_DUPLICATE`.
  - Covers distinct `A` + `B` remaining valid.
  - Covers malformed textual `BAD-G` + `0BAD-G` remaining distinct/valid.
- Concurrent-main readback: at `140e283edbedff5fd02966b45bc6581a3489cc56`, the validator still contains the intended shared numeric identity dedupe and source blob is `e6ea542c68cf8cb27d2bf2c6b07820f8c1f69ca7`.
- Combined status query for the regression commit returned no statuses. No GitHub Actions were dispatched.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleIdentitySmoke.cs`
- this claim file

## Excluded scope

- No changes to `GeneratedHandleOwnershipPolicy`, `GeneratedHandleIdentity`, ownership/resolver behavior, interchange schema, provenance drawing-scope policy, import authority, exporters, UI, or BricsCAD runtime.
- No GitHub Actions dispatch.

## Validation result

- Static/source regression evidence is present and read back from GitHub.
- Current `main` readback confirms the fix survived concurrent commits.
- No executed repository `dotnet` test or licensed BricsCAD runtime PASS is claimed from this hosted session.

## Coordination

The concurrent interchange provenance drawing-scope claim explicitly excluded generic validator changes and reserved different product/test files, so this slice did not overlap it.

## Completion condition

Satisfied: current `main` rejects numeric aliases of the same CAD source handle as duplicates at the interchange validator boundary, focused CAD-independent regression coverage is present, and this claim is `COMPLETED`.
