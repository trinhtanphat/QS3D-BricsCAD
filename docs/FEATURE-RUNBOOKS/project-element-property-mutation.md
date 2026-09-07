# ProjectElement property-map mutation boundary

Status: REMOTE_SAFE deterministic Core/Domain/Persistence qualification.

## Defect

`ProjectElement.Properties` was exposed as a raw mutable `Dictionary<string,string>`. Callers using the public `IDictionary` indexer, `Add`, `Remove`, or `Clear` could therefore change persisted semantic property content without passing through `ProjectElement` validation, dirty-state transitions, generated-output invalidation, or `UpdatedUtc` maintenance. QSDB serializes both the property map and those semantic state fields, so the old behavior could persist mutually inconsistent content and freshness state.

## Required behavior

- Public property-map writes route through the owning `ProjectElement` semantic mutation methods.
- Indexer replacement preserves `SetProperty` no-op behavior and canonical/XML-safe validation.
- `Add` preserves dictionary duplicate-key failure semantics and performs no dirty/timestamp mutation when admission fails.
- `Remove` changes semantic state only when a matching key is actually removed; `ICollection<KeyValuePair<...>>.Remove` must also require the value to match.
- `Clear` is a true no-op when empty and marks the surviving element state dirty exactly once when content existed. It must not repopulate generated-stale bookkeeping after clearing the entire map.
- Generated-state bookkeeping inside `ProjectElement` writes the backing store directly so semantic callbacks cannot recurse.

## Persistence hydration boundary

QSDB load is not a semantic user mutation. Persisted properties must be reconstructed exactly as stored and the persisted `Dirty` / `UpdatedUtc` values restored without synthesizing extra generated-stale properties. `QsdbProjectStore.ReadStringMap` therefore recognizes `ProjectElementPropertyDictionary` after the existing project-metadata special case and calls its internal `SetPersistenceValue` bypass. Duplicate map keys still fail before insertion.

## Validation

Run:

```text
python scripts/preflight-project-element-property-mutation.py
```

Then require the repository's fresh exact-head Shared preflight and managed Core validation to be terminal GREEN before merge. The carrier remains REMOTE_SAFE and does not claim licensed BricsCAD runtime coverage.

## Compatibility boundary

The public API remains `IDictionary<string,string>` with case-insensitive key lookup and ordinary dictionary enumeration. This change intentionally tightens mutation behavior to the same canonical semantic rules already used by `SetProperty`; read-only dictionary operations remain side-effect free.
