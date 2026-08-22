# Work claim — QSDB RawValue gate reconciliation

- Status: `ACTIVE`
- Agent: `codex-qsdb-relation-identity-gate-reconcile-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T15:49:00+07:00`
- Baseline main SHA: `c8302de334d08957588ea27c5938cd304d98c5f7`
- Priority: stale focused QSDB gate after completed project-metadata hydration integrity work

## Confirmed gate drift

Three focused gates require the obsolete one-line literal `target[key] = RawValue(item, "value");`: relation identity, free-text round-trip, and map integrity. Current `ReadStringMap` reads the exact raw value once. Ordinary string maps assign that local unchanged; `ProjectMetadataDictionary` sends the same local through `SetPersistenceValue`, which validates reserved mapping state on a tentative dictionary before backing assignment and avoids semantic revision mutation during persistence hydration.

The current contract is equal or stronger: free text is not trimmed, duplicate map keys still fail closed, reserved mapping metadata is validated before storage, and existing optional relation canonicality guards remain unchanged.

## Reserved scope

- `scripts/preflight-qsdb-relation-identity.py`
- `scripts/preflight-qsdb-free-text-roundtrip.py`
- `scripts/preflight-qsdb-map-integrity.py`
- this claim only

Replace each obsolete literal check with a method-bounded contract requiring raw read, project-metadata branch, validated persistence assignment, ordinary-map fallback and exact local assignment in that order. Reject trimmed `Value(...)` use inside `ReadStringMap`. Each gate remains independently authoritative for its existing identity/free-text/map assertions.

## Explicit exclusions

- no production, Core smoke or persistence behavior changes;
- no LOCAL runner/probe/docs, issue `#1005`, BricsCAD/native/private data, GitHub Actions, release or packaging work.

## Validation

- focused QSDB relation-identity, free-text round-trip and map-integrity gates;
- related QSDB canonical-identities and schema gates;
- Core Release build and full deterministic Core smoke;
- generic and manual-only policy gates.
