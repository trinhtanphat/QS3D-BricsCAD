# Work claim — XLSX Handle reader exact-header precedence

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xlsx-handle-exact-header-precedence-20260812-0810`
- Registered: `2026-08-12T08:10:00+07:00`
- Completed: `2026-08-12T08:16:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: P2 evidence-driven remote-safe XLSX header-resolution correctness

## Confirmed defect

`XlsxHandleReader.ReadHandleLookup(...)` placed both an exact `CAD Handle (hex)` header and every fuzzy header containing `handle` into the same `handleColumns` set. A valid modern QS3D worksheet could therefore be rejected as ambiguous merely because it also contained an unrelated descriptive column such as `Handle Notes`; the modern schema gate saw two Handle columns even though one exact semantic Handle header was present.

## Completed fix

- Exact `CAD Handle (hex)` headers remain in the authoritative Handle-column set.
- Fuzzy compatibility headers are collected separately and used only when no exact Handle header exists.
- Duplicate exact Handle headers remain ambiguous and fail the existing modern-schema gate.
- Legacy/fuzzy-only sheets remain readable through the existing compatibility path.
- Explicit Handle precedence over `$decimal`, Element ID/fingerprint checks, worksheet selection and all unrelated XLSX parsing behavior remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- existing `tests/QS3D.Core.SmokeTests/XlsxHandleExactHeaderPrecedenceSmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `480fbe6e757a7880ea675cef6e21a75bd6180ac9`.
- Focused regression already on `main`: `bf80642a78cb715a088241453ef849cc5dcd146a`.
- Source branch commit: `62e8bd4c6381a71b90c360966252d5e09fe25035`.
- Source diff from branch base `b3000e17411399e472639d869ae94f15d3f30ee1` was exactly one file, +4/-2 in `XlsxHandleReader.cs`.
- Moving-main comparison before merge showed 15 concurrent commits and no modification of `XlsxHandleReader.cs`.
- PR `#640` squash-merged to `main` at `03d4072f469bfa2e6ab138a48997572711d47322` with expected-head lock.

## Validation boundary

The focused smoke source covers exact+fuzzy precedence, duplicate exact ambiguity, and fuzzy-only compatibility. Source and diff were re-read remotely. No GitHub Actions were dispatched and no BricsCAD runtime PASS is claimed from this web session.

## Completion condition

Satisfied: exact Handle headers cannot be made ambiguous by unrelated fuzzy Handle headers, fuzzy compatibility remains available without an exact header, duplicate exact headers remain rejected, focused regression source and implementation are on current `main`, and the claim is released as `COMPLETED`.
