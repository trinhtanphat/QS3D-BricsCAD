# Project metadata Current-induced Count stability

## Scope

This contract covers `ProjectMetadataDictionary.ReplacePersistenceState` when the caller supplies an enumerable that exposes a supported deterministic `Count` surface.

## Integrity rule

Once a known Count is admitted, persistence must rebind that Count before and after each caller-controlled `MoveNext()`, and again immediately after `enumerator.Current` before the returned key/value can affect staged metadata state.

The ordering is:

```text
admit Count
  -> rebind Count
  -> MoveNext
  -> rebind Count
  -> enforce overrun/ceiling
  -> Current
  -> rebind Count
  -> observedCount/item validation/staging
  -> final Count/traversal validation
  -> reserved-metadata validation
  -> publication
```

A `Current` getter is caller-controlled code. If it changes a previously admitted Count, the canonical traversal Count error must win before null-key, duplicate-key, value normalization, reserved-metadata validation, or publication behavior can observe the returned item.

Pure streaming enumerables that expose no supported Count surface remain valid and are not converted into counted sources.

## Deterministic evidence

`ProjectMetadataCurrentCountSmoke` uses a counted enumerable whose first `Current` read changes Count from 1 to 2 and returns a null-key item. The expected outcome is the canonical `Project metadata persistence input Count changed during traversal.` failure after one `MoveNext` and one `Current` read, while the previously persisted metadata remains unchanged.

`ProjectMetadataPersistenceMidCountIntegritySmoke` retains the broader pre/post-MoveNext, multi-interface conflict/negative Count, stable multi-interface and streaming controls.

`scripts/preflight-project-metadata-current-count-stability.py` pins source ordering and the dedicated hostile regression so later refactors cannot silently move item acceptance ahead of the post-`Current` Count rebound.

## Runtime boundary

No licensed BricsCAD or private DWG evidence is required. This is deterministic Core/domain persistence integrity and is fully covered by repository source guards, build and smoke validation.
