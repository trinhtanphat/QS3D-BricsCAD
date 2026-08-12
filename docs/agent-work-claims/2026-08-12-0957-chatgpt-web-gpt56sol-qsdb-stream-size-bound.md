# Work claim — QSDB stream-bound file size validation

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:57:00+07:00`
- Baseline main SHA: `e0c6ec35d8c49c2410824043152a642efe9b566d`
- Priority: P1 — enforce the documented 64 MiB QSDB byte bound on the exact file handle that is parsed.

## Confirmed defect

`QsdbProjectStore.LoadDocument(string)` currently reads `new FileInfo(fullPath).Length` before opening the file and then parses a separately opened `FileStream`. A replace/grow race between those operations can make the byte-size check observe one file version while `XmlReader` parses another. `XmlReaderSettings.MaxCharactersInDocument` limits decoded XML characters, not the raw QSDB byte length, so it is not an equivalent byte-bound fallback.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs` (`LoadDocument` only)
- one focused Core smoke/regression for the exact stream-bound contract
- this claim file

## Intended contract

- Open the QSDB file once for reading.
- Enforce `MaxProjectFileBytes` against `stream.Length` on that exact open handle before constructing the XML reader.
- Preserve DTD prohibition, null resolver, character cap, normal load behavior, missing-file behavior and the existing 64 MiB error text.
- Do not change serialization, migration, backup fallback or publication behavior.

## Validation boundary

Source and focused regression will be read back from `main` after integration. GitHub Actions, executable .NET smoke/build and BricsCAD V25/V26 runtime PASS are not claimed unless actually executed.
