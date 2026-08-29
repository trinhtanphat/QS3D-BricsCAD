# MEP recognition known-Count stability

Issue: #4635  
Lane-Key: `issue-4635`  
Ownership-Key: `core.mep.recognition-known-count-stability`

## Defect boundary

`MepRecognitionRule` and `MepRecognitionProfile` accept caller-controlled `IEnumerable<T>` inputs. The earlier Current-integrity fix ensures the independent hard ceilings (100 tokens / 500 rules) are checked before reading the first disallowed `IEnumerator.Current`, but current `main` did not bind supported collection `Count` evidence. A collection could therefore advertise `Count=N`, drift while traversal was active, or yield a cardinality different from N without the constructor rejecting that unstable snapshot identity.

## Required contract

For both token and rule materialization:

1. snapshot every supported Count surface (`ICollection<T>`, `IReadOnlyCollection<T>`, non-generic `ICollection`) before enumeration;
2. reject negative, conflicting, and over-limit Count evidence before `GetEnumerator`/`MoveNext`;
3. re-read and compare Count before every caller-controlled `MoveNext`;
4. after a successful `MoveNext`, re-read and compare Count again before `Current`;
5. if the admitted Count is already exhausted, reject the unexpected element before `Current`;
6. retain the independent 100/500 safety ceiling before `Current` for pure streaming inputs;
7. after traversal, reject under-yield and perform a final Count rebound before publishing the immutable token/rule snapshot.

Stable counted collections and pure streaming enumerables retain existing semantic behavior, normalization, duplicate handling, diagnostics, sorting, and recognition results.

## Deterministic evidence

`MepRecognitionCurrentIntegritySmoke` covers:

- streaming 101st-token and 501st-rule ceiling rejection before `Current`;
- Count=N over-yield with the first unexpected `MoveNext` observed but no unexpected `Current` read;
- known-Count under-yield;
- transient Count growth/shrink/negative values detected after `MoveNext` and before `Current`;
- conflicting Count surfaces rejected before enumeration;
- stable counted token/rule collections accepted unchanged.

Focused guards:

```bash
python scripts/preflight-mep-recognition-current-integrity.py
python scripts/preflight-mep-recognition-count-stability.py
```

The first retains the established ceiling-before-Current contract. The second pins Count discovery/rebinding and hostile regression evidence.

## Runtime classification

`REMOTE_SAFE / NOT_APPLICABLE` for licensed BricsCAD runtime. Acceptance is deterministic Core source/smoke plus exact-head/protected CI. No hosted result is represented as `LOCAL_PASS`.