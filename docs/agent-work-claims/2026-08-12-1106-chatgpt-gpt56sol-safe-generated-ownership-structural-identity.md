# Work claim — Safe generated ownership structural claim identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-safe-generated-ownership-structural-identity`
- Registered: `2026-08-12T11:06:00+07:00`
- Last Updated: `2026-08-12T11:06:00+07:00`
- Baseline main SHA: `f2a2f417a9997be3e72307ca3071fe91c925dd27`
- Priority: P1 — distinct generated ownership claims must not collapse through delimiter-concatenated identity
- Task Key: `CORE-SAFE-GENERATED-OWNERSHIP-STRUCTURAL-IDENTITY`

## Confirmed defect

`SafeGeneratedHandleOwnershipHealthService` de-duplicates per-handle claims with `GroupBy(x => x.Token)` where `Token` is `ElementId + "/" + Slot`. `ProjectElement` IDs permit `/`, and `GeneratedHandleOwnershipPolicy.IsOwnerSlot(...)` intentionally accepts dynamic `Generated...Handle(s)` property keys, which can also contain `/`. Two distinct `(ElementId, Slot)` pairs can therefore produce the same concatenated token and be collapsed to one claim, causing a real generated-handle ownership conflict to false-clean.

A concrete collision is `(ElementId="E", Slot="GeneratedA/GeneratedBHandles")` versus `(ElementId="E/GeneratedA", Slot="GeneratedBHandles")`; both are valid owner slots and both stringify to `E/GeneratedA/GeneratedBHandles`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SafeGeneratedHandleOwnershipStructuralIdentitySmoke.cs`
- this claim file

## Intended contract

- Ownership identity remains structural: semantic element identity and logical owner slot are separate fields, never delimiter-concatenated for equality/deduplication.
- The concrete slash-collision case must emit the existing `GENERATED_HANDLE_OWNERSHIP_CONFLICT` Error for both owners.
- Existing same-element same-logical-slot de-duplication remains unchanged.
- Existing SourceHandles/generated-owner conflicts and malformed-project validation remain unchanged.
- Inspection stays read-only; do not change `GeneratedHandleOwnershipIndex`, owner-slot recognition, builders, persistence or CAD runtime code.

## Validation plan

Add an auto-registered Core smoke covering the slash-collision false-clean and a same-logical-slot alias non-regression. Review exact PR diff, guarded squash-merge to moving `main`, read back source/test, verify merge ancestry and close this claim with exact evidence.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
