# Work claim — Provenance target-map strict UTF-8 decode

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:15:00+07:00`
- Baseline main SHA: `15c80902fbb2a1c4d40cc6b0b9ce7d60d998c599`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeProvenanceTargetMap.DecodeRecord()` rejected malformed Base64 syntax but decoded valid Base64 through replacement-fallback UTF-8. A syntactically valid target-map record containing invalid UTF-8 bytes could therefore be accepted with replacement characters instead of failing closed as corrupted persisted source→target lineage.

## Reserved scope

Make provenance target-map record decoding fail closed on invalid UTF-8 while preserving record version/layout, Base64 encoding, token keys, source identity checks, target existence checks, rollback behavior, and valid map semantics. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeProvenanceTargetMapUtf8Smoke.cs`
- this claim file

## Excluded scope

- No changes to target-map one-to-one policy, interchange imports, remap/apply semantics, native ownership, UI, or BricsCAD V25 runtime.
- No change to valid persisted record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Seed a structurally valid target-map record with a target-id field whose Base64 bytes are `C3 28` (`wyg=`), then assert `ReadTargetElementId()` throws `InvalidOperationException` before using replacement text.
- Seed a valid Unicode source identity mapping to an existing ASCII target id and confirm it remains readable.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current target-map claim or recent `ProjectInterchangeProvenanceTargetMap` commit was found. The preceding provenance source-handle claims are completed and concern a different codec.

## Completion

- Implementation commits:
  - `6fbb1e613716cf5276cf2523a2614163082f771f` — decode persisted target-map fields with `UTF8Encoding(false, true)` and normalize invalid Base64/UTF-8 to `InvalidOperationException`.
  - `d52b4bdd30ec2710708062ebeffcb1133fa3c053` — add invalid UTF-8 target-id regression plus valid Unicode source-identity mapping coverage.
- Final observed `main` before claim close: `df0df09f65cb9e1da1f20d749984dea4111a548c`.
- Validation actually performed:
  - re-fetched the target-map codec from current `main` and confirmed strict UTF-8 decoding plus `DecoderFallbackException` handling are present;
  - re-fetched the new smoke and confirmed `wyg=` fails closed while `Dự án nguồn` / `Phần tử 01` resolve to existing target `T1`;
  - record version, Base64 format, identity matching and target existence semantics were otherwise unchanged;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-lineage integrity hardening.

## Completion condition

Satisfied: current `main` rejects invalid UTF-8 target-map fields, preserves valid target-map records, includes focused regression coverage, and this claim is released as `COMPLETED`.
