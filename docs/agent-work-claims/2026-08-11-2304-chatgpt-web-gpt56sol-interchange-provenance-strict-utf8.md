# Work claim — Interchange provenance strict UTF-8 decode

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:04:00+07:00`
- Baseline main SHA: `4564b0b8014901ccbdfae2631edd318ced4394d3`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeSourceHandleProvenance.DecodeRecord()` rejected malformed Base64 syntax but decoded valid Base64 with replacement-fallback UTF-8. A provenance field containing syntactically valid Base64 for invalid UTF-8 bytes could therefore be accepted as a replacement-character source identity/handle instead of being rejected as corrupted persisted state.

## Reserved scope

Make persisted interchange source-handle provenance records fail closed on invalid UTF-8 bytes while preserving record version/layout, Base64 format, tokenization, identity matching, handle-count validation, rollback behavior, and all valid provenance semantics. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSourceHandleProvenanceUtf8Smoke.cs`
- this claim file

## Excluded scope

- No changes to interchange JSON import/merge semantics, source-handle ownership policy, provenance token keys, UI/confirmation flows, exporters, or BricsCAD V25 runtime.
- No change to valid persisted record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Seed a structurally valid `v1` provenance record for source project `SRC` / element `E1` whose one handle field is Base64 `wyg=` (bytes `C3 28`, invalid UTF-8), and assert `ReadSourceHandles()` throws `InvalidOperationException`.
- Seed a valid Unicode identity record with a normal CAD handle and confirm it is decoded unchanged.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent provenance claims found in history are completed and target quantity/schedule provenance rather than this codec. No current claim or recent commit was found for strict UTF-8 decoding in `ProjectInterchangeSourceHandleProvenance`.

## Completion

- Implementation commits:
  - `a4331d4f54d760a84c1df47f6a1d74528ffd73b3` — decode persisted provenance fields with `UTF8Encoding(false, true)` and normalize invalid Base64/UTF-8 to `InvalidOperationException`.
  - `10cf8e20fc2b6f5936752e3f615516ff3837cdad` — add corrupted UTF-8 handle regression plus valid Unicode source-project/source-element identity coverage with an ordinary CAD handle.
- Final observed `main` before claim close: `10cf8e20fc2b6f5936752e3f615516ff3837cdad`.
- Validation actually performed:
  - re-fetched the codec from current `main` and confirmed strict UTF-8 decoding plus `DecoderFallbackException` handling are present;
  - re-fetched the new smoke and confirmed `wyg=` is rejected while `Dự án nguồn` / `Phần tử 01` identity tokenization and handle `ABCD` remain readable;
  - valid record version, field count, Base64 layout, tokenization, and source-element identity matching were otherwise unchanged;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-provenance integrity hardening.

## Completion condition

Satisfied: current `main` rejects invalid UTF-8 provenance fields, preserves valid provenance records, includes focused regression coverage, and this claim is released as `COMPLETED`.
