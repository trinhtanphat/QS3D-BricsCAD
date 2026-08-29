# SelectionState known-Count Current observation integrity

Lane-Key: `issue-4532`

## Purpose

`SelectionState.Replace` accepts caller-controlled `IEnumerable<string>` input. The method already binds supported collection Count surfaces, rejects malformed or conflicting Count evidence, enforces the 10,000-entry ceiling, rejects under-yield, rebinds Count after traversal, and preserves selection-state atomicity. However, the historical outer C# `foreach` evaluates `IEnumerator.Current` before the loop body. A source advertising Count=N could therefore expose item N+1 before the existing overrun guard rejected it.

## Contract

For replacement input:

1. Keep initial generic `ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection` Count validation and conflict rejection.
2. Traverse explicitly as successful `MoveNext -> known-Count admission -> independent 10,000 cap -> Current`.
3. Never read `Current` for the first item beyond an admitted known Count.
4. Preserve exact post-traversal under-yield rejection and post-traversal Count rebind/drift rejection.
5. Preserve selection `_changeVersion` freshness before any publication.
6. Preserve no-op behavior when the canonical set is unchanged, deterministic canonical ID normalization, pure-streaming input support, and one `Changed` event only after successful state publication.

## Deterministic regression

`SelectionStateKnownCountStabilitySmoke` uses a Count=1 adversarial collection whose enumerator independently records `MoveNext` calls and `Current` reads. The second `MoveNext` succeeds, proving an overrun exists, but the overrun must be rejected with exactly one `Current` read. A later third `MoveNext` throws, so the known-Count diagnostic must also retain precedence without advancing into that throwing tail.

Existing generic/read-only/non-generic Count drift, under-yield, stable multi-interface and streaming assertions remain active. The auto-discovered `preflight-selection-state-known-count-current-integrity.py` pins the production traversal/publication ordering and smoke registration.

## Runtime boundary

This is deterministic Core selection/state correctness. BricsCAD licensed runtime is `NOT_APPLICABLE`; hosted CI must not be reported as `LOCAL_PASS`.

## Landing

Require exact-head Shared CI, latest-main collision-clean reconciliation if necessary, one canonical PR with `Lane-Key: issue-4532`, fresh protected current-candidate `preflight + core`, expected-head merge, and exact protected-main verification.
