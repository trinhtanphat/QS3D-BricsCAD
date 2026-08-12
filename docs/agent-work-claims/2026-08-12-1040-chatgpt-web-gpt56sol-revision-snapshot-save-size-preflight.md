# Work claim — Revision snapshot save-size preflight

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:40:00+07:00`
- Completed: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `8583e783369fa87bf76551d3edbc31abe8c82ce1`
- Claim commit: `e38e25077759ff5a7c3f5bb6874324829f46de2d`
- Source commit on branch: `5d0fe6d9af78553f3cbed68ad303f19a26413fc9`
- Regression-source commit on branch: `2c29e071aeb3b359af1a085d9b98e44b924014a9`
- Pull request: `#771`
- Squash merge commit: `8f6a22713f03e8408b3ceba535aeecca794c165d`
- Priority: evidence-driven Core persistence preflight / filesystem atomicity

## Confirmed defect

`RevisionSnapshotStore` declares a hard 64 MiB supported revision-file limit and `Load(...)` rejects a file whose byte length exceeds that limit. `Save(...)` previously validated semantic/XML content first, but did not validate the serialized byte length until after destination resolution, directory creation, temp-file creation/write and `ValidateSerializedFile(temp)` / `Load(temp)`.

A snapshot whose serialized representation was guaranteed to be unsupported could therefore mutate the filesystem before the save failed.

## Implemented

- The validated snapshot is serialized once to an `XDocument` before any destination filesystem operation.
- A bounded write-only counting stream measures the exact `XDocument.Save(Stream, SaveOptions.DisableFormatting)` byte stream against the existing 64 MiB limit.
- Size rejection occurs before `Path.GetFullPath`, `Directory.CreateDirectory`, temp-path creation or temp-file write.
- The same preflighted `XDocument` is used for the actual temp-file write.
- Existing post-write `Load(temp)` validation, validated-backup preservation, atomic replacement and temp cleanup remain unchanged.
- Oversized output preserves the existing `InvalidDataException` 64 MiB contract.

## Regression source

`RevisionSnapshotSaveSizePreflightSmoke` covers:

- oversized serialized output using a small private test bound, proving failure before destination directory mutation without allocating a 64+ MiB fixture;
- a normal public `Save(...)` / `Load(...)` round trip after the preflight change.

## Integration evidence

While the feature branch was open, `main` advanced 29 commits, but `RevisionSnapshotStore.cs` retained exact pre-patch blob SHA `5350eba14b94204dce420ef9c0014e8c241cc53a`; no concurrent source overlap was present. PR `#771` was squash-merged with expected head SHA `2c29e071aeb3b359af1a085d9b98e44b924014a9` into `8f6a22713f03e8408b3ceba535aeecca794c165d`. Source was read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. The available container does not have `dotnet`, so the smoke source was not executed here and no executable .NET smoke/build PASS is claimed. No GitHub Actions/build/release was dispatched and no BricsCAD V25/V26 runtime PASS is claimed.
