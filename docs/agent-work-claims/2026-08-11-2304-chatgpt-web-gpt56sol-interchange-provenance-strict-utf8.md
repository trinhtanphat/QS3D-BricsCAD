# Work claim — Interchange provenance strict UTF-8 decode

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:04:00+07:00`
- Baseline main SHA: `4564b0b8014901ccbdfae2631edd318ced4394d3`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectInterchangeSourceHandleProvenance.DecodeRecord()` rejects malformed Base64 syntax but decodes valid Base64 with replacement-fallback UTF-8. A provenance field containing syntactically valid Base64 for invalid UTF-8 bytes can therefore be accepted as a replacement-character source identity/handle instead of being rejected as corrupted persisted state.

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
- Seed a valid Unicode handle record and confirm it is decoded unchanged.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent provenance claims found in history are completed and target quantity/schedule provenance rather than this codec. No current claim or recent commit was found for strict UTF-8 decoding in `ProjectInterchangeSourceHandleProvenance`.

## Completion condition

Current `main` rejects invalid UTF-8 provenance fields, preserves valid provenance records, includes focused regression coverage, and this claim is marked `COMPLETED`.
