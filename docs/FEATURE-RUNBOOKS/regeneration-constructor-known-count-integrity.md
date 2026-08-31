# Regeneration constructor known-Count integrity

## Scope

Core-only integrity contract for `RegenerationEngine(DependencyGraph, IEnumerable<IElementRegenerator>)`. Licensed BricsCAD execution is not required.

## Acceptance

The constructor must keep pure-streaming regenerator sources single-pass while treating any supported caller-known Count surface as integrity evidence.

For `ICollection<IElementRegenerator>`, `IReadOnlyCollection<IElementRegenerator>`, and non-generic `ICollection` sources:

- reject negative or conflicting Count values;
- revalidate the admitted Count before and after caller-controlled `MoveNext`, immediately after `Current`, and after traversal;
- reject declared-count overrun before reading unexpected `Current`;
- reject under-yield/final drift;
- validate null semantics only after the post-`Current` Count rebound and retain entries only after both checks;
- preserve stable counted and pure-streaming construction behavior.

## Deterministic verification

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-regeneration-constructor-known-count-integrity.py
```

`RegenerationConstructorIntegritySmoke` covers the existing null contracts plus Count=0 over-yield/no-Current, transient `MoveNext` drift, transient `Current` drift, known-count under-yield, stable counted input, and pure-streaming input.

No hosted/source result may be described as licensed BricsCAD `LOCAL_PASS`.
