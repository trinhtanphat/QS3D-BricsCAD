# ProjectFamily property persistence admission

## Scope

This REMOTE_SAFE Core/Domain carrier protects `ProjectFamily.Properties`, a persistence-aware case-insensitive dictionary whose accepted state is serialized into QSDB project XML.

## Invariant

A property mutation must be admissible for deterministic persistence before it can advance project persistence state. Property keys are required, canonical (no surrounding whitespace), free of control characters, and XML-safe. Property values are XML-safe; caller-supplied `null` is normalized to `string.Empty` before equality/no-op evaluation and storage.

Invalid indexer writes therefore fail before the mutation callback, preserving the dictionary contents, `ProjectState.ChangeVersion`, and `UpdatedUtc`. Snapshot restore validates every family-property entry before replacing live dictionary state.

The fix preserves the existing case-insensitive dictionary contract, duplicate-`Add` behavior, no-op same-value semantics, remove/clear behavior, and accepted caller spelling for canonical keys.

## Deterministic validation

Run:

```text
python scripts/preflight-project-family-property-admission.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The auto-discovered preflight pins validation-before-callback ordering and snapshot admission. `ProjectFamilyPropertyAdmissionSmoke` exercises rejected invalid keys/values without persistence mutation, null normalization/no-op behavior, case-insensitive replacement and duplicate semantics, plus remove/clear lifecycle behavior.

Licensed BricsCAD runtime is not required and no LOCAL_PASS claim belongs to this carrier.
