# MEP recognition rule/token Current integrity

Issue: #4581  
Lane-Key: `issue-4581`  
Ownership-Key: `core.mep.recognition-rule-token-limit-current-integrity`

## Contract

`MepRecognitionRule` and `MepRecognitionProfile` accept caller-controlled `IEnumerable<T>` inputs with hard ceilings of 100 tokens per rule and 500 rules per profile. A successful `IEnumerator.MoveNext()` for element 101/501 must be rejected by the ceiling check before the implementation reads `IEnumerator.Current`.

The required traversal order is therefore:

1. call `MoveNext()`;
2. check the admitted token/rule ceiling;
3. reject the first disallowed element without reading `Current`;
4. only for allowed elements, read `Current` and perform the existing text/null/duplicate/snapshot processing.

Existing exact 100/500 boundaries, diagnostics, malformed-text rejection, duplicate handling, rule ordering, and bounded termination behavior remain unchanged.

## Deterministic regression

`MepRecognitionCurrentIntegritySmoke` uses hostile enumerators that independently count `MoveNext` and `Current` access. It requires the first disallowed `MoveNext` to be observed while proving element 101 and element 501 are never read through caller-controlled `Current`.

Run the focused source guard with:

```bash
python scripts/preflight-mep-recognition-current-integrity.py
```

The guard pins the required `MoveNext -> ceiling -> Current` ordering for both token and rule traversal, rejects regression to the vulnerable caller-controlled `foreach` shape, and requires the deterministic smoke evidence.

## Runtime classification

`REMOTE_SAFE / NOT_APPLICABLE` for licensed BricsCAD runtime. This is deterministic Core collection-integrity behavior and is qualified by source guards, Core build/smoke, and protected exact-head CI; it does not claim `LOCAL_PASS`.
