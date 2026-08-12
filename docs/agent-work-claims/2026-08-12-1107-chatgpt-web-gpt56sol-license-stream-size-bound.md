# Work claim — License parsed-stream size bound

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:07:00+07:00`
- Baseline main SHA: `8054e291cabf8f49b2d2afdc3bb61df9155a6969`
- Priority: evidence-driven Core licensing input/resource integrity

## Confirmed defect

`LicenseVerifier.Load(...)` enforces a 64 KiB license-file limit through `FileInfo.Length`, then opens the file in a separate step and parses that later stream. A file can be replaced or grow between the metadata size check and `FileStream` open, so the enforced byte bound is not attached to the actual stream consumed by `XmlReader`. `MaxCharactersInDocument` is a character bound, not the same byte-size contract.

The completed QSDB parsed-stream size lane already establishes the repository pattern: bind the byte-length guard to the exact stream that will be parsed.

## Intended scope

- resolve the canonical full path as today;
- open the license file once with the existing read/share mode;
- check `stream.Length` on that exact parsed stream for the existing `0 < length <= 64 KiB` contract before creating `XmlReader`;
- preserve current missing-file behavior, XML security settings, schema/content/canonicality checks, Base64/signature rules and verification behavior;
- add focused Core smoke coverage proving empty/oversized parsed streams fail through the existing size diagnostic and a valid canonical license still loads.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseParsedStreamSizeSmoke.cs`
- this claim file

## Excluded scope

Do not modify license signing/verification semantics, status/value-object behavior, Base64 canonicality, XML grammar/canonical timestamps/token rules, LOCAL-003 license fixture repair, CAD/UI adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim executable .NET smoke/build or BricsCAD V25/V26 runtime PASS without actual execution.
