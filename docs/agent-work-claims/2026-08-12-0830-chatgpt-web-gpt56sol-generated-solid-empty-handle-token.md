# Work claim — generated solid empty handle token

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-solid-empty-handle-20260812-0830`
- Registered: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `2ecb42affc613707e5b25d1760411738be8d6701`
- Priority: owner-requested continue-all residual runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` distinguished a missing `GeneratedSolidHandle` from malformed non-hex data only after a combined early-return guard. That guard also treated a present-but-empty/null/whitespace `GeneratedSolidHandle` as if the property were absent, so corrupt generated-solid metadata could be silently hidden from runtime health even though non-empty malformed handles were reported as `GENERATED_SOLID_HANDLE_INVALID`.

## Reserved scope

- Preserve missing `GeneratedSolidHandle` as the valid no-generated-solid case.
- Treat a present null/empty/whitespace handle token as `GENERATED_SOLID_HANDLE_INVALID` through the existing fail-visible path.
- Keep native ownership inspection strictly read-only and preserve all existing unresolved/unreadable/erased/type/ownership diagnostics.
- Add one focused static regression preflight for the present-empty token contract.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-generated-solid-empty-handle-token.py`
- this claim file

## Integration evidence

- Claim registration: `1c8d7a6e81c65f4376b358282714d26c885101b8`
- Source fix: `92fe422a809309bd818fb6be68baa90dfd1f53cd`
- Focused regression preflight: `8d2bc1be27d7b23db229a9d67a7d590806dc93f9`

## Validation performed

- Re-fetched the claim and exact V25 source from current `main` after registration before editing; the old combined `TryGetValue(...) || string.IsNullOrWhiteSpace(...)` skip was still present.
- Source now skips only when `GeneratedSolidHandle` is absent, then normalizes the present value with `(rawHandle ?? string.Empty).Trim()` so null/empty/whitespace values reach the existing `GENERATED_SOLID_HANDLE_INVALID` branch.
- Re-fetched the final source from current `main`; blob `54dcdde1102b38fb81275757b591897fd010c9f4` retained the fix after concurrent integrations.
- Re-fetched `scripts/preflight-generated-solid-empty-handle-token.py`; blob `9a70ff8341216099ac42abad5162c366c2c51bc4` rejects the old false-clean predicate and requires missing-only skip, null-safe normalization and the invalid-handle diagnostic.
- Re-read the existing `scripts/preflight-generated-solid-runtime-health-integrity.py`; it still requires `OpenMode.ForRead`, the existing fail-visible diagnostic flow and rejects mutation/write tokens in the ownership inspection path.

## Validation boundary

Remote source/static readback only. This session did not execute the repository preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No generated geometry creation/regeneration/cleanup changes.
- No changes to sibling generated rebar/mesh/curtain/tag/grid health lanes.
- No provider-isolation rewrite.
- No V26/native runtime behavior claims.
- No GitHub Actions dispatch, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer silently skips a present empty `GeneratedSolidHandle`, focused regression source pins that behavior, the pre-existing read-only/fail-visible generated-solid integrity contract remains intact, and exact integration evidence is recorded above.
