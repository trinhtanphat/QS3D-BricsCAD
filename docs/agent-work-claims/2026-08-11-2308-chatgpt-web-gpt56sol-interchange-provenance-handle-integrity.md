# Work claim — Interchange provenance handle integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:08:00+07:00`
- Baseline main SHA: `c7339fe76259bd7b6ff97e7d6a722c54abf90969`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

The interchange JSON validator requires every source handle to be non-empty, unpadded, at most 128 characters, and unique per element case-insensitively. `ProjectInterchangeSourceHandleProvenance.ReadSourceHandles()` verifies record version, source element identity, and handle count, but currently returns decoded handle fields without revalidating those source-handle invariants. Tampered persisted provenance can therefore expose states that the source interchange validator would reject.

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

## Completion condition

Current `main` revalidates decoded provenance handles against the established interchange source-handle contract, includes focused regression coverage, and this claim is marked `COMPLETED`.
