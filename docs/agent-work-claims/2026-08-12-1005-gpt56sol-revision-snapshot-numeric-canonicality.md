# Work claim — Revision snapshot numeric canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-snapshot-numeric-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Completed: `2026-08-12T10:08:00+07:00`
- Baseline main SHA: `2b42086f53c87111c40566e7f30858248ebbec7a`
- Claim commit: `a9ecf8a36d7e3696e44428eeac75b3c7ea9a3469`
- Source fix commit: `029916d0f898145b84ea858f8f9dc8cb0189fd0c`
- Regression commit: `9768783e4341de12580a530994d6b72933fcd7b2`

## Confirmed defect

`RevisionSnapshotStore.Serialize` emits every persisted quantity value with `ToString("R", CultureInfo.InvariantCulture)`, but `Load` delegated to a permissive `Number(...)` parser that accepted alternate spellings such as `1.0`, `+1`, padded values, or equivalent exponent forms. A revision snapshot could therefore load successfully and then round-trip to a different persisted numeric token even though the surrounding revision format already enforces canonical IDs, categories, XML structure and finite values.

## Completed scope

`RevisionSnapshotStore.Number(...)` now parses and verifies a finite value, computes the serializer-owned round-trip representation with `ToString("R", CultureInfo.InvariantCulture)`, and rejects the persisted token unless it matches that representation exactly using ordinal comparison. Serializer output and accepted numeric values remain unchanged.

## Regression coverage

`RevisionSnapshotNumericCanonicalitySmoke` loads a real `.qsrev` payload and requires canonical `value="1"` to succeed while equivalent but non-canonical `1.0`, `+1`, and padded ` 1 ` tokens fail closed with `InvalidDataException`.

## Validation actually performed

- Re-read integrated `RevisionSnapshotStore.Number(...)` from current `main` and confirmed finite parsing followed by exact serializer-token comparison.
- Re-read the focused persisted-file smoke and confirmed canonical success plus three non-canonical rejection cases.
- Verified regression commit `9768783e4341de12580a530994d6b72933fcd7b2` is an ancestor of main snapshot `4c7eab8a0494a10da5211332a74cbf01d106167d` with `behind_by: 0`; intervening commits did not touch this source/test lane.
- Existing revision timestamp explicit-offset normalization was intentionally preserved because the repository already has regression coverage for that compatibility behavior.
- No GitHub Actions were dispatched. No local .NET build/smoke execution or BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

No timestamp compatibility, serializer shape, revision compare semantics, backup handling, XML schema or BricsCAD adapter behavior was changed.

## Completion

Completed. Revision snapshot quantity tokens now round-trip deterministically under the serializer-owned numeric representation.