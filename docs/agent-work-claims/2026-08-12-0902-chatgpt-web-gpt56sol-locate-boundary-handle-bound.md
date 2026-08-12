# Work claim — Locate boundary-handle resource bound

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-locate-boundary-handle-bound-20260812-0902`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `7b16210497167156599a3e4f9080817511054182`
- Priority: evidence-driven persisted-input resource bound during owner-requested review/fix continuation

## Confirmed defect

`SourceHandleResolver.AddBoundaryHandles(...)` read persisted `AutoRoomLifecycle.BoundarySourceHandlesKey` through an unbounded `Split(';')`. Canonical Room boundary discovery already rejects more than 5,000 source segments, but malformed delimiter-dense persisted metadata could allocate a token array far beyond that supported topology before Locate failed or completed.

## Implemented fix

- Added a 5,000 boundary-source-handle capacity matching the canonical Room boundary source-segment limit.
- Persisted tokenization now uses the count-bounded `Split` overload and materializes at most 5,001 non-empty tokens.
- More than 5,000 boundary handles fail closed before any boundary handle is added to the Locate result.
- Root input bound/freshness, direct source-handle canonicality, dependency validation, generated-owner fallback, ordering/deduplication and Auto Room provenance behavior remain unchanged.

## Integration evidence

- Claim registration: `a53c2d7a0039bca089426121d3346680ace2bc04`.
- Source fix: `3ee1f7fc41d06a91a0934a8e4fd588cd48a6e28b`.
- Focused smoke: `38f5005f9fa945a243c51dec62e005618b9876a2`.
- Source read-back confirmed count-bounded tokenization and the pre-loop >5,000 guard on moving `main`.
- Smoke read-back confirmed exact-cap acceptance, 5,001 fail-closed behavior and ordinary trim/remove-empty compatibility.

## Coordination

The immediately preceding source-handle root-freshness claim is `COMPLETED`. This lane did not edit `AutoRoomLifecycle.cs`, Room boundary discovery, command/native Locate code or existing Locate smoke files.

## Validation boundary

Deterministic source and focused smoke coverage were committed and read back. No GitHub Actions were dispatched, no executable full Core smoke/build PASS is claimed, and no licensed BricsCAD runtime qualification is claimed.