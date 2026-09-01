# Recognition known-Count integrity

## Scope

Core-only deterministic validation for `RecognitionInputBounds.Materialize<T>` in `src/QS3D.Core/Recognition/RecognitionEngine.cs`.

This helper is shared by recognition rule terms, recognition rule collections, candidate lists, snapshot batches, and recognition result batches. Supported `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count surfaces are integrity evidence, not allocation hints.

## Required behavior

For counted inputs:

1. reject negative, conflicting, or over-limit Count at admission;
2. revalidate the admitted Count before and after `MoveNext`;
3. reject declared-count overrun and the hard max before reading an unexpected `Current`;
4. revalidate Count immediately after `Current` and before retaining the item;
5. reject under-yield and final Count drift after traversal.

Pure-streaming `IEnumerable<T>` inputs remain supported and single-pass. The existing 10,000 rule/term limits and 250,000 batch limit remain unchanged.

## Deterministic regression

`RecognitionBoundedEnumerationSmoke` covers:

- Count drift induced by `MoveNext`;
- Count drift induced by `Current`;
- stable counted input;
- pure-streaming input;
- pre-existing oversize admission and lazy max+1 sentinel behavior.

The hostile enumerables restore their Count at the next traversal boundary, proving that admission/final materialized-cardinality comparison alone is insufficient.

## Validation

Run:

```text
python scripts/preflight-recognition-known-count-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

No licensed BricsCAD runtime evidence is required for this Core boundary.
