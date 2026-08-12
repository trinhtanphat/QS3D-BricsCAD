# Work claim — License parsed-stream size bound

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:07:00+07:00`
- Completed: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `8054e291cabf8f49b2d2afdc3bb61df9155a6969`
- Claim commit: `c360c8f5867454a6cc432fd1c4e13c19f4d0be55`
- Source commit on branch: `adfe8d44d4666d78f03208b6001bbe899c2f7624`
- Regression-source commit on branch: `8eb58663d4a9fd0770376820cf61da4ee29a4755`
- Pull request: `#804`
- Squash merge commit: `64a1dece913d8131f1a047f8b6746074e8e8f6bb`
- Priority: evidence-driven Core licensing input/resource integrity

## Confirmed defect

`LicenseVerifier.Load(...)` enforced a 64 KiB license-file limit through `FileInfo.Length`, then opened the file in a separate step and parsed that later stream. A file could be replaced or grow between the metadata size check and `FileStream` open, so the byte bound was not attached to the actual stream consumed by `XmlReader`. `MaxCharactersInDocument` is a character bound, not the same byte-size contract.

## Implemented

- Canonical path resolution and the custom missing-file diagnostic remain unchanged.
- The license is opened with the existing `FileMode.Open`, `FileAccess.Read`, `FileShare.Read` mode.
- The existing `0 < stream.Length <= 64 KiB` contract is now evaluated on that exact `FileStream` before `XmlReader` is created.
- Empty/oversized parsed streams retain the existing `License file size is invalid.` diagnostic.
- XML security/schema/content/canonicality, Base64/signature and verification behavior are unchanged.

## Regression source

`LicenseParsedStreamSizeSmoke` covers empty and 64 KiB+1 files through the size guard before XML parsing, plus a valid canonical license with the existing empty-signature behavior.

## Integration evidence

While the branch was open, `main` advanced 12 commits, but `LicenseVerifier.cs` retained exact pre-patch blob SHA `a63e30187043934a7d09287f30081702910412bd`; no concurrent source overlap was present. PR `#804` was squash-merged with expected head SHA `8eb58663d4a9fd0770376820cf61da4ee29a4755` into `64a1dece913d8131f1a047f8b6746074e8e8f6bb`. Merged source was read back from `main` with blob SHA `2fe4a7b63781546ed0705d900591db7550e55dd2`.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched and no executable .NET smoke/build or BricsCAD V25/V26 runtime PASS is claimed.
