# SelectionState known-Count stability

## Scope

This runbook covers the deterministic Core boundary in `SelectionState.Replace(IEnumerable<string>)`. It does not cover licensed BricsCAD runtime behavior, UI selection, PICKFIRST, document affinity, or native entity resolution.

## Integrity contract

`SelectionState.Replace` accepts pure streaming inputs and collection-backed inputs exposing deterministic `Count` metadata through generic `ICollection<string>`, `IReadOnlyCollection<string>`, or non-generic `ICollection`.

For a source exposing deterministic Count metadata, replacement is a two-phase contract:

1. bind all supported Count surfaces before caller-controlled traversal and reject negative, conflicting, or greater-than-10,000 evidence;
2. while traversing, reject before validating/retaining the first item beyond the admitted Count;
3. preserve the independent 10,000-entry cap for pure streaming sources;
4. after traversal, preserve reentrant `SelectionState` freshness checks and reject under-yield against the admitted Count;
5. re-read all supported Count surfaces post-traversal and fail closed when deterministic Count evidence changed or becomes negative/conflicting before publishing the replacement.

The first overrun item is the deterministic failure boundary: its value is never normalized or retained, and traversal must not advance to any later tail merely to discover a different exception. No temporary replacement set is published and no `Changed` event is emitted after a cardinality/freshness failure.

## Preserved behavior

- whitespace-only incoming ids are ignored;
- non-empty ids retain the existing Trim normalization;
- semantic identity deduplication remains case-insensitive;
- stable counted sources and pure streaming sources remain supported;
- the 10,000-entry public safety bound is unchanged;
- reentrant replacement/clear freshness behavior and no-op event semantics remain unchanged.

## Deterministic regression

`SelectionStateKnownCountStabilitySmoke` covers:

- Count overrun with a later throwing tail and verifies the tail is not advanced;
- post-traversal Count drift for generic, read-only generic, and non-generic collection surfaces;
- existing under-yield behavior;
- no publication/event on rejected input;
- stable multi-interface counted input and pure streaming controls.

`scripts/preflight-selection-state-known-count-stability.py` is auto-discovered by aggregate preflight and pins the source/regression/runbook contract.

## Runtime boundary

Runtime is `NOT_APPLICABLE`. Hosted Core smoke and protected Shared CI are sufficient evidence for this source-only integrity package; no licensed BricsCAD or private-DWG `LOCAL_PASS` claim is applicable.
