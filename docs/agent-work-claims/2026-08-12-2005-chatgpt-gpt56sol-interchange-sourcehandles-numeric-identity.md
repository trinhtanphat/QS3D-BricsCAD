# Work claim — Interchange sourceHandles numeric CAD identity

- Status: `ACTIVE`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12T20:05:00+07:00`
- Baseline main SHA: `9e65b58d40b0d0937c4de4dc7dbfbd6bbb55838b`
- Priority: evidence-driven remote-safe Core interchange hardening

## Reason

`ProjectInterchangeJsonValidator.ValidateSourceHandles()` currently deduplicates `sourceHandles` with raw case-insensitive text identity. Core ownership and `SourceHandleResolver` already use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity()` so CAD numeric aliases such as `A`, `0A`, and `0x000a` identify the same handle. An interchange payload can therefore pass validation with duplicate aliases and only become ambiguous/conflicting after entering the shared ownership/resolution boundary.

## Reserved scope

Use the existing shared generated-handle numeric identity only for duplicate detection inside interchange `sourceHandles`. Preserve the original trimmed token for diagnostics and payload semantics, preserve malformed/non-hex textual identities, preserve whitespace/length validation, and do not canonicalize or rewrite exported JSON values.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleIdentitySmoke.cs`
- this claim file

## Excluded scope

- No changes to `GeneratedHandleOwnershipPolicy`, `GeneratedHandleIdentity`, ownership/resolver behavior, interchange schema, provenance drawing-scope policy, import authority, exporters, UI, or BricsCAD runtime.
- No GitHub Actions dispatch.

## Validation plan

- `A` plus `0A` within one element must report `SOURCE_HANDLE_DUPLICATE`.
- `A` plus `0x000a` must report the same duplicate code.
- Distinct handles remain accepted.
- Malformed/non-hex provenance tokens retain textual identity and are not numerically merged.
- Re-fetch current `main` and validator blob immediately before the product write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` or licensed runtime run from this hosted session.

## Coordination

The currently ACTIVE interchange provenance drawing-scope claim explicitly excludes generic validator changes and reserves different product/test files, so this slice does not overlap it.

## Completion condition

Current `main` rejects numeric aliases of the same CAD source handle as duplicates at the interchange validator boundary, focused CAD-independent regression coverage is present, and this claim is marked `COMPLETED`.
