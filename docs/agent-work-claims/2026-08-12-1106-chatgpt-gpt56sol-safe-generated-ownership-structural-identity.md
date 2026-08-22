# Work claim — Safe generated ownership structural claim identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-safe-generated-ownership-structural-identity`
- Registered: `2026-08-12T11:06:00+07:00`
- Completed: `2026-08-12T11:08:00+07:00`
- Last Updated: `2026-08-12T11:08:00+07:00`
- Baseline main SHA: `f2a2f417a9997be3e72307ca3071fe91c925dd27`
- Priority: P1 — distinct generated ownership claims must not collapse through delimiter-concatenated identity
- Task Key: `CORE-SAFE-GENERATED-OWNERSHIP-STRUCTURAL-IDENTITY`

## Confirmed defect

`SafeGeneratedHandleOwnershipHealthService` de-duplicated per-handle claims with `GroupBy(x => x.Token)` where `Token` was `ElementId + "/" + Slot`. `ProjectElement` IDs permit `/`, and `GeneratedHandleOwnershipPolicy.IsOwnerSlot(...)` accepts dynamic `Generated...Handle(s)` property keys which can also contain `/`. Distinct structural `(ElementId, Slot)` claims could therefore stringify identically and be collapsed, hiding a real ownership conflict.

Concrete collision: `(ElementId="E", Slot="GeneratedA/GeneratedBHandles")` and `(ElementId="E/GeneratedA", Slot="GeneratedBHandles")` both stringify as `E/GeneratedA/GeneratedBHandles`.

## Completed change

- Removed delimiter-concatenated claim text from equality/de-duplication.
- Preserved existing structural same-element/same-logical-slot de-duplication in `AddClaims`.
- Deterministic conflict ordering now sorts by `ElementId` then `Slot`.
- Peer selection for conflict messages uses object identity instead of concatenated text identity.
- Malformed-project validation, SourceHandles conflicts, host-solid aliases, owner-slot recognition and inspection read-only behavior remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/SafeGeneratedHandleOwnershipStructuralIdentitySmoke.cs` covers:

- slash-delimiter collision remains two Error-level `GENERATED_HANDLE_OWNERSHIP_CONFLICT` issues, one per owner;
- same-element `GeneratedSolidHandle` / `PhysicalOpeningCutSolidHandle` logical alias remains de-duplicated;
- inspection does not change `ProjectState.ChangeVersion`.

## Integration evidence

- Claim registration: `842cb306cd850861e674c9f68c00be12cae82328`.
- Source fix branch commit: `92cf4c8fbc257c326aed05cde3125d6fe50b91b7`.
- Focused smoke branch commit: `b50956de4fc4ec6d44299e066775eadd9770ca55`.
- Pull Request: `#801`.
- Squash merge: `2f84569519ac4cfd0f0d051159b6cec16b0696da`.
- Main readback source blob: `a1abf2a22f342c2649c5292069597581a1267225`.
- Main readback smoke blob: `a041899366ae6a575592a2a6077bdf9f05c2d980`.
- Ancestry verification: `main` was ahead of the merge by 1 commit, behind by 0, with merge base exactly `2f84569519ac4cfd0f0d051159b6cec16b0696da`.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS was executed or claimed in this connector session.
