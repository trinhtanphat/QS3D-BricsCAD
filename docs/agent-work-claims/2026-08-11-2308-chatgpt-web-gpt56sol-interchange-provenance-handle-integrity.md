# Work claim — Interchange provenance handle integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:08:00+07:00`
- Baseline main SHA: `c7339fe76259bd7b6ff97e7d6a722c54abf90969`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

The interchange JSON validator requires every source handle to be non-empty, unpadded, at most 128 characters, and unique per element case-insensitively. `ProjectInterchangeSourceHandleProvenance.ReadSourceHandles()` verified record version, source element identity, and handle count, but returned decoded handle fields without revalidating those source-handle invariants. Tampered persisted provenance could therefore expose states that the source interchange validator would reject.

## Reserved scope

Revalidate decoded persisted provenance handles against the same non-empty/canonical/max-length/duplicate rules as interchange source handles. Preserve strict UTF-8 decoding, record layout/tokenization, handle count, ordering, source identity matching, and all valid provenance behavior. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceIntegritySmoke.cs`
- this claim file

## Excluded scope

- No changes to JSON validation rules, import/merge behavior, target CAD ownership, UI, exporters, or BricsCAD V25 runtime.
- No change to valid provenance record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Assert persisted records with a blank handle, padded handle, over-128-character handle, or duplicate handles differing only by case fail closed.
- Assert a valid two-handle record preserves its encoded ordering and values.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The immediately preceding strict UTF-8 provenance claim is `COMPLETED`. This is a separate invariant lane derived directly from the existing `ProjectInterchangeJsonValidator.ValidateSourceHandles` contract; no current claim was found for persisted provenance handle integrity.

## Completion

- Implementation commits:
  - `379f9f8f4955ae5c6b08073a08ff37abf97e5253` — revalidate decoded provenance handles for non-empty canonical form, 128-character limit, and case-insensitive uniqueness.
  - `7b3364405dad7cae15f025125fdd794a95b4d1b1` — add blank, padded, overlong, duplicate, and valid-order regression coverage.
- Final observed `main` before claim close: `939b51a3fe1478e848be9d97f1d6f60a7b280a0d`.
- Validation actually performed:
  - re-fetched the reader from current `main` and confirmed handle checks mirror the established interchange validator contract;
  - re-fetched the new smoke and confirmed all four malformed states fail closed while `B2`, `A1` remain readable in encoded order;
  - confirmed strict UTF-8 decoding from the prior completed lane remains intact;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-provenance integrity hardening.

## Completion condition

Satisfied: current `main` revalidates decoded provenance handles against the established interchange source-handle contract, includes focused regression coverage, and this claim is released as `COMPLETED`.
