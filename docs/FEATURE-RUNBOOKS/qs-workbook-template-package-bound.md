# QS workbook template package materialization bound

Lane-Key: `issue-5242`
Reservation-Protocol: `v2`
Canonical carrier: `agent/longnguyentuan2107-maker-c02-20260901-template-bound/issue-5242-template-package-bound`
Ownership-Key: `core.export.qs-workbook-template-package-materialization-bound-v1`
Runtime: `NOT_APPLICABLE`

## Defect

`QsWorkbookTemplateExporter.Export` previously copied a user supplied XLSX template and parsed workbook, relationship and worksheet XML with raw `XDocument.Load(entry.Open())`. A small compressed template could therefore cause large decompression/DOM materialization before the post-write package validator ran.

## Correctness contract

- Reject source templates larger than 128 MiB before copy/update work.
- Parse workbook and workbook-relationship metadata with a 4 MiB decompressed/XML character ceiling.
- Parse worksheet and shared-string XML with a 64 MiB ceiling.
- Reject negative/oversized entry lengths before opening the XML stream.
- Use `XmlReaderSettings` with `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersInDocument`, and `MaxCharactersFromEntities = 0`.
- Preserve atomic destination semantics: hostile templates must not replace an existing destination.
- Preserve canonical template export behavior, formulas/styles outside mapped cells, worksheet resolution, provenance and package validation.

## Deterministic validation

Run:

```text
python scripts/preflight-qs-workbook-template-package-bound.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The hostile smoke creates a small ZIP whose `xl/workbook.xml` expands beyond the metadata ceiling and requires `InvalidDataException` while the pre-existing destination remains byte-for-byte unchanged. A canonical control template must still export successfully.

No licensed BricsCAD runtime evidence is required or claimed for this Core-only correction.
