# Work claim — QSDB save-size preflight

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:57:00+07:00`
- Completed: `2026-08-12T11:03:00+07:00`
- Baseline main SHA: `9ea748b2fde921248287e0eeaae3e86aca1beb3b`
- Claim commit: `ef3ea41e99f100bf1803b15a5bfee90f63b8db2c`
- Source commit on branch: `a7ee811950a0afeadcaecd26e29654c25164647d`
- Regression-source commit on branch: `d144c39f89d7a311de753b6032ca8c658f314393`
- Pull request: `#795`
- Squash merge commit: `88d301c5c3313037e17875a2c8a9dd2c4e8e8a71`
- Priority: evidence-driven Core QSDB persistence filesystem atomicity

## Confirmed defect

`QsdbProjectStore` enforces a hard 64 MiB load limit and validates the written temp file through that same bounded loader. Before this change, `SaveCore(...)` validated semantic/XML content, resolved the destination, created the destination directory/temp path, mutated the in-memory persistence stamp (`SchemaVersion` / `Touch()`), serialized and wrote the whole temp file, and only then discovered that an oversized serialized QSDB could not be loaded.

The failed save restored the project persistence stamp, but output guaranteed to exceed the supported 64 MiB contract could still mutate the filesystem before failing. The completed read-side QSDB stream-size lane remains unchanged; this lane closes the distinct write-side preflight gap.

## Implemented

- Existing project/XML validation and destination-path resolution remain before persistence-stamp mutation.
- Public Save/SaveNew/SavePreservingValidatedBackup signatures and publication behavior are unchanged.
- The exact post-`Touch()` `XDocument` is serialized into a bounded counting stream using the same `SaveOptions.DisableFormatting` stream path as the real temp write.
- Oversized output is rejected with the existing 64 MiB `InvalidDataException` contract before destination-directory creation, temp-path creation or temp-file write.
- The same preflighted document is used for the actual temp write.
- On any pre-commit failure, `SchemaVersion`, `UpdatedUtc` and `ChangeVersion` are restored exactly as before.
- Existing post-write validation, create-new publication, primary-only replacement and backup rotation semantics are preserved.

## Regression source

`QsdbSaveSizePreflightSmoke` covers:

- oversized post-`Touch()` serialization using a small private test bound, proving the destination directory is not created and the original persistence stamp is restored;
- a normal public Save/Load round trip preserving metadata and the persisted post-save `ChangeVersion`.

## Integration evidence

While the branch was open, `main` advanced 22 commits, but `QsdbProjectStore.cs` retained exact pre-patch blob SHA `e1b9418686c2b27a04cb68ffc34f15cddb8a3f57`; no concurrent source overlap was present. PR `#795` was squash-merged with expected head SHA `d144c39f89d7a311de753b6032ca8c658f314393` into `88d301c5c3313037e17875a2c8a9dd2c4e8e8a71`. Merged source was read back from `main` with blob SHA `f092e0c8500f71853114586d1eb9e26db8e5b1dc`.

## Validation boundary

Remote/static source + regression review only. The available container does not have `dotnet`, so the smoke source was not executed here and no executable .NET smoke/build PASS is claimed. No GitHub Actions/build/release was dispatched and no BricsCAD V25/V26 runtime PASS is claimed.
