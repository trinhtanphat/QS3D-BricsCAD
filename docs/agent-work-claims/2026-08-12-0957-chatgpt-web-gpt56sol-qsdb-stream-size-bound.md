# Work claim — QSDB stream-bound file size validation

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:57:00+07:00`
- Completed: `2026-08-12T10:10:00+07:00`
- Baseline main SHA: `e0c6ec35d8c49c2410824043152a642efe9b566d`
- Claim commit: `bf4814120c243090732922a20edc57997f79aa02`
- Source fix: `04b49c5cd62f502167000c681d478111aa6a6820`
- Regression gate: `b65993879934450ef32878c8e40f77ad35e01a9c`
- Priority: P1 — enforce the documented 64 MiB QSDB byte bound on the exact file handle that is parsed.

## Confirmed defect

`QsdbProjectStore.LoadDocument(string)` read `new FileInfo(fullPath).Length` before opening the file and then parsed a separately opened `FileStream`. A replace/grow race between those operations could make the byte-size check observe one file version while `XmlReader` parsed another. `XmlReaderSettings.MaxCharactersInDocument` limits decoded XML characters, not the raw QSDB byte length, so it was not an equivalent byte-bound fallback.

## Completed scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs` now opens the QSDB file first and checks `stream.Length` on the exact stream that is subsequently passed to `XmlReader.Create`.
- DTD prohibition, null resolver, decoded-character cap, normal load behavior, missing-file behavior and the existing 64 MiB error text remain unchanged.
- `scripts/preflight-qsdb-stream-size-bound.py` pins the open → stream-length guard → parse ordering, requires the existing XML hardening settings, and rejects reintroduction of a `FileInfo` path-metadata size guard inside `LoadDocument`.
- Serialization, schema migration, backup fallback and publication behavior were not changed.

## Validation evidence

- Claim registration: `bf4814120c243090732922a20edc57997f79aa02`
- Source fix on `main`: `04b49c5cd62f502167000c681d478111aa6a6820`
- Focused regression gate on `main`: `b65993879934450ef32878c8e40f77ad35e01a9c`
- Post-integration readback confirmed `FileStream` is opened before `stream.Length > MaxProjectFileBytes`, and `XmlReader.Create(stream, settings)` follows that guard on the same handle.
- Regression source was read back from `main` and confirms the exact contract tokens/order.

## Validation boundary

GitHub Actions, executable Python/.NET smoke, full build and licensed BricsCAD V25/V26 runtime were not run in this hosted session, so no runtime/build PASS is claimed.

## Completion

Completed. The 64 MiB QSDB byte-size limit is now enforced against the exact open file stream that is parsed, eliminating the path-metadata/open TOCTOU gap. Reservation released.
