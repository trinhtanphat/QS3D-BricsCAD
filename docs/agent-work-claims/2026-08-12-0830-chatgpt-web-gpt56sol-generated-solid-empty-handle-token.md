# Work claim — generated solid empty handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-solid-empty-handle-20260812-0830`
- Registered: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `2ecb42affc613707e5b25d1760411738be8d6701`
- Priority: owner-requested continue-all residual runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` distinguishes a missing `GeneratedSolidHandle` from malformed non-hex data only after a combined early-return guard. The current guard also treats a present-but-empty/null/whitespace `GeneratedSolidHandle` as if the property were absent, so corrupt generated-solid metadata can be silently hidden from runtime health even though non-empty malformed handles are reported as `GENERATED_SOLID_HANDLE_INVALID`.

## Reserved scope

- Preserve missing `GeneratedSolidHandle` as the valid no-generated-solid case.
- Treat a present null/empty/whitespace handle token as `GENERATED_SOLID_HANDLE_INVALID` through the existing fail-visible path.
- Keep native ownership inspection strictly read-only and preserve all existing unresolved/unreadable/erased/type/ownership diagnostics.
- Add one focused static regression preflight for the present-empty token contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-generated-solid-empty-handle-token.py`
- this claim file

## Excluded scope

- No generated geometry creation/regeneration/cleanup changes.
- No changes to sibling generated rebar/mesh/curtain/tag/grid health lanes.
- No provider-isolation rewrite.
- No V26/native runtime behavior claims.
- No GitHub Actions dispatch, release publication, force push, or licensed BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current `main`, this claim, and the exact source after claim registration before editing.
- Split the missing-property check from token normalization so a present null/empty/whitespace value reaches the existing invalid-handle issue path.
- Add a narrow source preflight that rejects the old combined skip predicate and requires null-safe normalization plus `GENERATED_SOLID_HANDLE_INVALID`.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close this claim with exact SHAs.

## Completion condition

Completed only when current `main` no longer silently skips a present empty `GeneratedSolidHandle`, focused regression source pins that contract, existing read-only/fail-visible generated-solid diagnostics remain intact, and this claim is updated to `COMPLETED` with exact integration evidence.
