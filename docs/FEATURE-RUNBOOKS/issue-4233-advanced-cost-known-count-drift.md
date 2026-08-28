# Issue #4233 — Advanced Cost known-count traversal integrity

Lane-Key: `issue-4233`

## Problem

Advanced Cost accepts both counted collections and pure streaming `IEnumerable<T>` inputs. When a collection exposes a trustworthy `Count`, traversal must not process more elements than that Count advertises. Rejecting the mismatch only after enumeration allows an unexpected element to reach null/duplicate validation, dictionary mutation, aggregation, or other semantic work first.

## Repository-safe contract

For every consumer of `AdvancedCostCollectionContract.TryGetKnownCount`:

1. reject a known Count above `MaximumEntries` before enumeration;
2. before processing each yielded item, call `AdvancedCostCollectionContract.RequireCanProcessNext(...)`;
3. for a known Count, reject when `observedCount >= knownCount` before any semantic processing of that unexpected item;
4. for an unknown streaming source, retain the independent `MaximumEntries` bound;
5. after enumeration, call `RequireKnownCountMatchesTraversal(...)` so under-yield remains fail-closed;
6. preserve existing null, duplicate, ordering, arithmetic, and domain validation for elements that are inside the declared cardinality.

The affected Advanced Cost paths are rate build-up components, historical records, build-up analysis rates, trade-analysis items, BQ library entries/imports, tender quote lines/requirements/bids, and progress contract/claim lines.

## Deterministic regression

`AdvancedCostKnownCountTraversalSmoke` covers over-yield, under-yield, honest counted inputs, pure streaming inputs, and ordering between Count-integrity and semantic validation. The early-overrun matrix uses a valid first item plus an unexpected null second item with advertised Count `1`; the expected result is the Count mismatch, proving the second item did not reach semantic null validation.

`scripts/preflight-advanced-cost-known-count-early-drift.py` locks the shared helper, all eleven consumer call sites, and the smoke controls.

## Validation boundary

This is Core-only commercial/data-integrity work. No BricsCAD host, private DWG, signing evidence, or `LOCAL_PASS` is applicable. Required landing evidence is deterministic Core smoke plus the repository's protected current-candidate `preflight` and `core` checks.