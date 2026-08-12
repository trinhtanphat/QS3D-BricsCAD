# Work claim — Revision Snapshot timestamp canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-timestamp-canonicality`
- Registered: `2026-08-12T11:06:00+07:00`
- Baseline main SHA: `2599e5bf96e9bc14b97bb39ee63af56e54e926d9`
- Priority: P2 — persisted Revision Snapshot timestamps must round-trip in the exact UTC representation emitted by the writer.

## Confirmed defect

`RevisionSnapshotStore.Save(...)` validates that `CreatedUtc` has `DateTimeKind.Utc`, and `Serialize(...)` writes `createdUtc` with `CreatedUtc.ToString("O", CultureInfo.InvariantCulture)`. The read helper `Date(...)`, however, only requires an explicit offset and then accepts the broad `DateTimeOffset.TryParse(...)` grammar before normalizing to `UtcDateTime`.

As a result, non-canonical persisted timestamps such as an equivalent `+00:00` form or a non-zero-offset representation can be accepted even though the writer never emits them. That makes Revision Snapshot read-side canonicality weaker than its own write contract and weaker than the file's already-canonical category and numeric token handling.

The auto-registered `RevisionSnapshotStoreIntegritySmoke` also still asserts the old tolerant contract by requiring a `+07:00` timestamp to normalize successfully. That assertion must change atomically with the parser hardening or the Core smoke executable will deterministically retain a stale expectation.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs` (`Date(...)` timestamp parse/canonicality only)
- `tests/QS3D.Core.SmokeTests/RevisionSnapshotStoreIntegritySmoke.cs` (timestamp contract alignment only)
- this claim file

## Intended contract

- `createdUtc` must parse as the exact invariant round-trip representation produced by a UTC `DateTime` writer.
- Equivalent `+00:00`, non-zero-offset, padded, lowercase/alternate, or otherwise non-canonical timestamp spellings fail closed.
- The existing auto-registered integrity smoke accepts canonical UTC writer output and rejects tolerant offset representations.
- Writer serialization, backup recovery, size bounds, numeric/category canonicality, and revision semantics stay unchanged.

## Excluded scope

- No Revision delta encoding changes.
- No Revision XML schema/size/backup changes.
- No QSDB timestamp policy changes.
- No native BricsCAD runtime changes.
- No GitHub Actions dispatch and no runtime qualification claim.

## Validation plan

- Publish this claim before source writes and verify it remains reachable from current `main`.
- Re-fetch the exact production/test blobs after claim publication.
- Tighten `Date(...)` to exact invariant `"O"` UTC parsing plus canonical round-trip equality.
- Align the existing auto-registered `RevisionSnapshotStoreIntegritySmoke` with canonical UTC acceptance and equivalent/non-zero-offset rejection.
- Inspect exact source/test diffs and read-back, close this claim with exact commit SHAs, then verify ancestry.
- No local compile/runtime PASS will be claimed unless actually executed.
