# ProjectFamily property persistence admission

## Scope

This REMOTE_SAFE Core/Domain carrier protects `ProjectFamily.Properties`, a persistence-aware case-insensitive dictionary whose accepted state is serialized into QSDB project XML.

## Invariant

A property mutation must be admissible for deterministic persistence before it can advance project persistence state. Property keys are required, canonical (no surrounding whitespace), free of control characters, XML-safe, and bounded to the persistence limit of 120 UTF-16 code units. Property values are XML-safe and bounded to 1000 UTF-16 code units; caller-supplied `null` is normalized to `string.Empty` before equality/no-op evaluation and storage. These bounds match `ProjectFamilyService` persistence validation.

Invalid indexer or `Add` writes therefore fail before the mutation callback, preserving the dictionary contents, `ProjectState.ChangeVersion`, and `UpdatedUtc`. Snapshot restore validates every family-property entry before replacing live dictionary state.

The fix preserves the existing case-insensitive dictionary contract, duplicate-`Add` exception precedence, no-op same-value semantics, remove/clear behavior, and accepted caller spelling for canonical keys. Exact boundary values remain admitted: a 120-character key and 1000-character value are valid; 121/1001 are rejected without persistence mutation.

Compatibility tests that intentionally model malformed legacy Family state bypass public admission only inside test fixtures by injecting the private backing dictionary. This keeps downstream fail-closed snapshot/rule/template/inspector coverage independent without weakening production admission.

## Deterministic validation

Run:

```text
python scripts/preflight-project-family-property-admission.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The auto-discovered preflight pins validation-before-callback ordering, bounded key/value admission, `Add` ordering/duplicate precedence, and snapshot admission. `ProjectFamilyPropertyAdmissionSmoke` exercises rejected invalid and oversized keys/values without persistence mutation, exact maximum accepted lengths, null normalization/no-op behavior, case-insensitive replacement and duplicate semantics, plus remove/clear lifecycle behavior.

Licensed BricsCAD runtime is not required and no LOCAL_PASS claim belongs to this carrier.
