# RateBook transient known-Count stability

Issue: #4640
Lane-Key: issue-4640
Runtime: NOT_APPLICABLE

## Defect boundary

`RateBook` consumes caller-controlled `IEnumerable<RateItem>` inputs. Count-bearing inputs can expose deterministic cardinality through `ICollection<RateItem>`, `IReadOnlyCollection<RateItem>`, or non-generic `ICollection`.

Historical hardening already validates Count at admission, rejects the first over-yield before `IEnumerator.Current`, enforces the independent 10,000-item ceiling, rejects under-yield, and rechecks Count after traversal. That leaves a transient-stability gap: Count metadata can change while traversal is in progress and return to its admitted value before the final recheck.

## Required traversal contract

When deterministic Count is available and admitted, every caller-controlled traversal step follows this ordering:

1. re-read all supported Count surfaces and require the admitted Count;
2. call `MoveNext`;
3. after a successful `MoveNext`, re-read all supported Count surfaces again;
4. reject known-Count over-yield;
5. reject the independent 10,000-item streaming ceiling;
6. only then observe `IEnumerator.Current`;
7. after traversal, reject under-yield and perform one final Count rebound before sorting/publication.

Negative or conflicting Count evidence keeps the existing fail-closed diagnostics from `TryGetKnownCount`. A changed otherwise-valid Count fails with the RateBook known-count-changed diagnostic. Pure streaming enumerables without supported Count surfaces do not gain extra Count assumptions and retain the independent capacity guard.

## Deterministic regression

`RateBookTransientCountStabilitySmoke` uses a custom `IReadOnlyCollection<RateItem>` and custom enumerator with independent Count, `MoveNext`, and `Current` instrumentation. It proves:

- transient growth before an advance fails with zero `MoveNext` and zero `Current` reads;
- transient growth after a successful advance fails after exactly one `MoveNext` and before `Current`;
- transient shrink before an advance fails before traversal;
- transient negative Count after an advance retains the invalid-negative-known-count failure before `Current`;
- stable counted input still consumes exactly two admitted `Current` values plus the terminal false `MoveNext`.

The historical `RateBookKnownCountTraversalSmoke` remains authoritative for over-yield, under-yield, post-traversal drift/conflict, honest multi-interface Count, and pure-streaming controls.

## Source guards

- `scripts/preflight-ratebook-known-count-stability.py` preserves the historical Count/no-overread contract while pinning the strengthened traversal shape.
- `scripts/preflight-ratebook-transient-count-stability.py` pins pre-advance and post-advance Count revalidation before `Current` and verifies focused hostile smoke registration.

## Validation / landing

Run the discovered feature guards and deterministic Core smoke on the exact branch head. Automatic exact-head branch Shared CI must be terminal green. Refresh protected `main`, reconcile non-force if it advanced, obtain fresh exact-head evidence after any SHA change, then use one canonical protected PR. Merge only with current protected `preflight` + `core` SUCCESS and expected-head protection, then verify the exact protected-main merge ancestry.

Licensed BricsCAD execution is not applicable to this Core commercial-integrity contract and must not be claimed as `LOCAL_PASS`.
