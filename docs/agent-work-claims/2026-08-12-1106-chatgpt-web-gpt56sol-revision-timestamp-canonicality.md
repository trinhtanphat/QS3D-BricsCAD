# Work claim — Revision Snapshot timestamp canonicality

- Status: `DONE`
- Agent: `chatgpt-web-gpt56sol-revision-timestamp-canonicality`
- Registered: `2026-08-12T11:06:00+07:00`
- Baseline main SHA: `2599e5bf96e9bc14b97bb39ee63af56e54e926d9`
- Priority: P2 — persisted Revision Snapshot timestamps must round-trip in the exact UTC representation emitted by the writer.

## Confirmed defect

`RevisionSnapshotStore.Save(...)` validates that `CreatedUtc` has `DateTimeKind.Utc`, and `Serialize(...)` writes `createdUtc` with `CreatedUtc.ToString("O", CultureInfo.InvariantCulture)`. The read helper `Date(...)` previously only required an explicit offset and then accepted the broad `DateTimeOffset.TryParse(...)` grammar before normalizing to `UtcDateTime`.

That allowed non-canonical persisted timestamps such as equivalent `+00:00` or non-zero-offset representations even though the writer never emits them. The auto-registered `RevisionSnapshotStoreIntegritySmoke` also encoded that stale tolerant contract by requiring a `+07:00` timestamp to normalize successfully.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs` (`Date(...)` timestamp parse/canonicality only)
- `tests/QS3D.Core.SmokeTests/RevisionSnapshotStoreIntegritySmoke.cs` (timestamp contract alignment only)
- this claim file

## Implemented contract

- `createdUtc` now parses only as exact invariant `"O"` format with `DateTimeStyles.RoundtripKind`.
- Parsed timestamps must retain `DateTimeKind.Utc` and reproduce the exact stored token via `ToString("O", CultureInfo.InvariantCulture)`.
- Equivalent `+00:00`, non-zero-offset, missing-offset, and short-form UTC timestamps fail closed.
- The existing auto-registered integrity smoke now accepts canonical UTC writer-form timestamps and rejects those non-canonical representations.
- Other malformed category/map/source-handle fixtures were moved to canonical timestamps so they continue testing their intended failure causes.

## Commits

- Claim registration: `70076ffd1a917f9db73442f3cd9d5bca126af991`
- Claim scope expansion: `7781fd32a68cd38d4c657b5c6e763f678206e6f3`
- Product fix: `4b153b6e82087ad41754cbc94ff79a25544b4cd4`
- Regression alignment: `0845bb05edd14f09db8fa0cd51894bbe2890585b`

## Validation

- Re-fetched production and auto-registered smoke blobs after claim publication.
- Product commit exact diff changes only `Date(...)` and removes the obsolete permissive offset helper.
- Test commit exact diff changes only timestamp contract coverage/fixtures.
- Read-back from current `main` confirms the exact UTC parser and aligned smoke are present.
- No GitHub Actions dispatched.
- No local C# compile or BricsCAD V25/V26 runtime PASS claimed.
