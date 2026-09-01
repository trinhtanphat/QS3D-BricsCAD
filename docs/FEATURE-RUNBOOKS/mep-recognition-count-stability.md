# MEP recognition known-Count stability

Issue: #4635, strengthened by #4924  
Lane-Key: `issue-4924`  
Ownership-Key: `core.mep.recognition-current-induced-known-count-stability-v1`

## Defect boundary

`MepRecognitionRule` and `MepRecognitionProfile` accept caller-controlled `IEnumerable<T>` inputs. The earlier Current-integrity fix ensures the independent hard ceilings (100 tokens / 500 rules) are checked before reading the first disallowed `IEnumerator.Current`, and the #4635 known-Count fix binds collection Count evidence before and around `MoveNext`. The remaining boundary was `Current` itself: a hostile counted enumerator could change Count while returning an otherwise malformed token or null rule, and semantic validation would begin before the next Count rebound observed that structural drift.

## Required contract

For both token and rule materialization:

1. snapshot every supported Count surface (`ICollection<T>`, `IReadOnlyCollection<T>`, non-generic `ICollection`) before enumeration;
2. reject negative, conflicting, and over-limit Count evidence before `GetEnumerator`/`MoveNext`;
3. re-read and compare Count before every caller-controlled `MoveNext`;
4. after a successful `MoveNext`, re-read and compare Count again before `Current`;
5. if the admitted Count is already exhausted, reject the unexpected element before `Current`;
6. retain the independent 100/500 safety ceiling before `Current` for pure streaming inputs;
7. read `Current` exactly once for an accepted element and immediately re-read Count before incrementing cardinality or performing token/rule semantic validation;
8. after traversal, reject under-yield and perform a final Count rebound before publishing the immutable token/rule snapshot.

Stable counted collections and pure streaming enumerables retain existing semantic behavior, normalization, duplicate handling, diagnostics, sorting, and recognition results.

## Deterministic evidence

`MepRecognitionCurrentIntegritySmoke` retains historical coverage for streaming ceilings, Count overrun/under-yield, pre-Current transient Count drift, conflicting Count surfaces, and stable counted inputs.

`MepRecognitionCurrentCountDriftSmoke` adds hostile `Current`-induced Count mutation for both token and rule sources. The token probe returns malformed whitespace while changing Count, and the rule probe returns null while changing Count. In both cases the canonical Count-stability failure must win before malformed-token/null-rule validation, with exactly one `Current` read.

Focused guards:

```bash
python scripts/preflight-mep-recognition-current-integrity.py
python scripts/preflight-mep-recognition-count-stability.py
```

The first retains the established hard-ceiling-before-Current contract. The second pins Count discovery/rebinding and the stronger traversal ordering: `MoveNext -> Count -> overrun/ceiling -> Current -> Count -> semantic acceptance`.

## Runtime classification

`REMOTE_SAFE / NOT_APPLICABLE` for licensed BricsCAD runtime. Acceptance is deterministic Core source/smoke plus exact-head/protected CI. No hosted result is represented as `LOCAL_PASS`.
