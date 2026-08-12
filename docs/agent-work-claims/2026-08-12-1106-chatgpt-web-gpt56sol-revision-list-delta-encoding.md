# Work claim — Revision list delta encoding collision safety

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-list-delta-encoding-20260812-1106`
- Registered: `2026-08-12T11:06:00+07:00`
- Completed: `2026-08-12T11:10:00+07:00`
- Priority: P1 revision review fidelity

## Confirmed defect

`RevisionService.Compare(...)` correctly compared `SourceHandles` and `Dependencies` as canonical string sequences, but once it detected a difference it serialized each side with `string.Join(",", ...)`. Revision list values may themselves contain commas. Distinct lists such as `["A,B", "C"]` and `["A", "B,C"]` therefore produced the same rendered `Before`/`After` text (`A,B,C`) even though the service reported the field as changed.

## Resolution

- Claim: `f2a2f417a9997be3e72307ca3071fe91c925dd27`
- Source: `e0be5cab1c0771549633ba4391d3ccb46a9ab326`
- Regression: `14cd3c225f4de985168b3b2cd21bf768f64e499b`

Revision list deltas now use an injective escaped representation: each backslash is escaped first, then each comma is escaped, and tokens remain comma-separated. Ordinary tokens containing neither character retain the exact existing readable output. Case-insensitive canonical list sorting/comparison and RevisionSnapshot persistence are unchanged.

The focused smoke covers comma-bearing source handles and dependencies that previously collapsed to identical text, backslash-bearing tokens, and byte-for-byte ordinary list output compatibility. Exact source/test readback confirmed the intended implementation on moving `main`.

## Excluded scope

The local-owned `RevisionCaptureXmlTextIntegritySmoke.cs`, RevisionSnapshotStore persistence, quantity/property diff semantics, CAD/UI and build/release workflows were not modified.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed.
