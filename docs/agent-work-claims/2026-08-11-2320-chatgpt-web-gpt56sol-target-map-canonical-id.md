# Work claim — Provenance target-map canonical target id

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:20:00+07:00`
- Baseline main SHA: `4edc480c8e8ad539643eeef33db3c06e23bb95b0`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeProvenanceTargetMap.Store()` normalizes source/target semantic ids before encoding them, so a valid persisted target id never contains surrounding whitespace. `ReadTargetElementId()` decoded a target id and passed it through the same trimming `Required()` helper, silently accepting a tampered record such as `" T1 "` and resolving it as `T1`. Persisted non-canonical identity should fail closed rather than be repaired during read.

## Reserved scope

Require decoded persisted target ids to already be canonical (no surrounding whitespace) before target lookup, while preserving public `Store()` input normalization, max-length/control-character validation, source identity matching, strict UTF-8 decoding, target existence checks, and valid mapping behavior. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapCanonicalIdSmoke.cs`
- this claim file

## Excluded scope

- No changes to one-to-one mapping semantics, source identity tokenization, interchange import/remap behavior, UI, native ownership, or BricsCAD V25 runtime.
- No change to valid persisted record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Seed a target-map record whose decoded target id is `" T1 "` while target `T1` exists; assert `ReadTargetElementId()` fails closed instead of trimming and resolving it.
- Use public `Store()` with padded caller source/target ids and confirm its existing normalization still writes a mapping that reads back as canonical `T1`.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The preceding target-map strict UTF-8 claim is `COMPLETED`. This is a separate canonical persisted-identity lane; no current target-map claim was found.

## Completion

- Implementation commits:
  - `2914e0e6d9fb2a1ee909e2f316aa38a9282eb6eb` — reject persisted target Element ids whose decoded text differs from its trimmed canonical identity.
  - `7336a6d2b7e7f5a9286b9a64d652b5a1e3ed29cd` — add tampered padded-record regression and preserve public Store input normalization coverage.
- Final observed `main` before claim close: `e1e68fe8dff63f8ab9bd6cb106fd6f8088a27879`.
- Validation actually performed:
  - re-fetched `ReadTargetElementId()` from current `main` and confirmed the persisted raw target id must equal its canonical trimmed value before lookup;
  - re-fetched the smoke and confirmed persisted `" T1 "` fails while public Store accepts padded caller ids and produces canonical readable `T1`;
  - strict UTF-8 decoding from the preceding completed lane remains intact;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-lineage canonicalization hardening.

## Completion condition

Satisfied: current `main` rejects non-canonical persisted target ids without changing valid public Store normalization, includes focused regression coverage, and this claim is released as `COMPLETED`.
