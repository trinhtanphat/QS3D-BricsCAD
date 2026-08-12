# Agent work claim — Release #34 ProjectStateSnapshot gate reconciliation

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:18 Asia/Ho_Chi_Minh`

## Scope

Reconcile three Release #34 preflights with the current stronger `ProjectStateSnapshot` implementation. Snapshot cloning/restoration now routes through a richer `CopyInto` overload that can preserve captured object identity, project Metadata is cleared/copied directly, and element Dirty/UpdatedUtc restoration happens on the target element. The gates must pin those semantics rather than obsolete variable names/signatures.

## Files

- `scripts/preflight-native-documentation-tables-integration.py`
- `scripts/preflight-semantic-capture-integrity.py`
- `scripts/preflight-wall-snap-atomicity.py`
- this claim file

## Out of scope

- production `ProjectStateSnapshot.cs`
- `ProjectPersistenceStamp` scalar-drift lane
- native Table behavior
- Wall Snap production behavior
- semantic capture production behavior
- release/updater/signing/runtime qualification

## Acceptance checks

- native Table gate requires Metadata clear/copy plus project persistence-state restore without requiring nullable-value normalization that production no longer uses;
- semantic-capture gate requires detached clone to call the richer `CopyInto(..., null, null, null, null)` path;
- Wall Snap gate requires element state restoration through `target.RestorePersistenceState(source.Dirty, source.UpdatedUtc)`;
- existing rollback/atomicity/read-only assertions remain intact.
