# Work claim — Project metadata null persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-metadata-null-persistability-20260814-1327`
- Registered: `2026-08-14T13:27:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `9c9257d546d218ad9a5fbd0c4e8d7b88c2195d57`

## Confirmed defect

`ProjectState.Metadata` is backed by `ProjectMetadataDictionary`. Its public `IDictionary<string,string>` mutation surface accepts a runtime `null` value and stores that null unchanged. `QsdbProjectStore.Serialize(...)` later writes metadata with `x.Value ?? string.Empty`, so a supported in-memory state is silently rewritten from null to empty string during persistence. The loaded project therefore does not round-trip the state that was accepted by the domain boundary.

This is separate from Browser-specific null metadata parsing: the defect is the generic project metadata dictionary accepting a representation that its canonical QSDB writer does not preserve.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs`
- new `tests/QS3D.Core.SmokeTests/ProjectMetadataNullPersistabilitySmoke.cs`
- this claim file

## Intended change

Canonicalize incoming null metadata values to `string.Empty` before reserved-key validation and storage. Keep key identity/comparer behavior, add-only semantics, measurement-mapping reserved-key validation, removal, enumeration and all callers unchanged.

## Regression plan

Self-registering Core smoke will prove:

1. indexer assignment of null immediately exposes the canonical empty string in memory;
2. `Add(key, null)` follows the same canonical representation;
3. non-null metadata remains byte-for-byte unchanged;
4. Save -> Load through `QsdbProjectStore` preserves the canonical empty value without a second normalization step.

## Explicit non-scope

- no metadata key canonicality changes;
- no reserved measurement-work-item codec policy changes;
- no Browser/material/unit/native metadata changes;
- no QSDB schema or migration changes;
- no UI/native changes;
- no GitHub Actions or BricsCAD runtime qualification.

## Validation boundary

Remote source/diff/readback only in this lane. Executable Core smoke PASS will not be claimed unless independent evidence runs it.
