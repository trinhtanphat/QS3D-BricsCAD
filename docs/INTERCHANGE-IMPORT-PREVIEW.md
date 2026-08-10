# QS3D Semantic Snapshot — read-only import preview

Updated: 2026-08-10 (UTC+7)

`ProjectInterchangeImportPreview` is a **read-only collision/provenance preview** for a valid `QS3D.SemanticSnapshot` v1 file against an existing `ProjectState`. It does not import, merge, restore, rebind CAD handles, reconstruct native ownership, write `.qsdb`, touch DWG entities or mutate the target project.

The preview always runs `ProjectInterchangeJsonValidator` first. Invalid snapshots stop before collision planning. Valid snapshots are reduced to the minimum identity manifest needed for review: project ID/fingerprint, Zone/Floor IDs, Family ID/category and element ID/category.

A validator PASS is still **not import permission**. A collision-free preview is also not import permission.

## Identity classifications

- `New` — no target identity currently uses the same semantic ID.
- `ExistingNeedsPolicy` — the ID already exists and a future importer needs an explicit merge/replace/skip/rename rule.
- `ExistingIncompatible` — a Family/element ID collides with a target object of a different `ElementCategory`; automatic merge must fail closed.

Zone/Floor collisions always require policy. Family/element collisions with the same category also require policy; the preview never silently chooses property/name/quantity precedence. Duplicate IDs already present in the target project are rejected because a mutating importer must not start from ambiguous identity.

The preview parser intentionally keeps category parsing aligned with the strict v1 validator: exact enum spelling/case plus a defined `ElementCategory` value remains required after validation. The preview must not silently broaden a contract the validator already accepted strictly.

## Project and drawing provenance

The preview reports source/target project IDs, whether project IDs match, and drawing fingerprint relation `Match`, `Different` or `Unknown`. These are review signals only. Matching IDs/fingerprints do not authorize overwrite, and a mismatch does not itself define future copy/merge policy.

## Drawing-local CAD handles remain non-portable

Semantic Snapshot v1 declares `sourceRefScope = drawing-local`. The preview intentionally does not parse or rebind `sourceHandles`. A future importer must explicitly choose whether to discard source handles, prove a same-DWG mapping, or use another stable external-reference contract. A source DWG Handle must never be assumed to identify the same target CAD entity.

## Generated/native ownership remains excluded

The existing validator rejects generated/native ownership properties. The preview does not reconstruct any of them. Portable JSON must not become native CAD ownership authority. Future semantic import should treat generated output as unowned/stale/rebuild-required unless a separately reviewed reconstruction protocol exists.

## Bounds and checks

`ProjectInterchangeImportPreview.MaxDetailedItems = 10000`; complete counts remain available if detail rows are truncated. Preview result items are copied into a read-only collection.

```text
python scripts/preflight-interchange-import-preview.py
```

`ProjectInterchangeImportPreviewSmoke` covers all-new identities without target mutation, same-category collisions, incompatible category collisions, invalid snapshots, fingerprint relation and ambiguous target IDs.

## Still required before any mutating importer

Do **not** add `QS3DINTERCHANGEIMPORT` or mutate live project state until import mode, per-kind collision policy, project/fingerprint policy, property/quantity precedence, catalog merge behavior, dependency ordering, schema migration, drawing-local provenance, generated ownership clearing/rebuild strategy, transaction/rollback, confirmation UX, audit and exact V25 adapter qualification are separately reviewed and guarded.

Until then this feature is **REMOTE_DONE as read-only import planning only**. JSON round-trip/import remains intentionally incomplete.
