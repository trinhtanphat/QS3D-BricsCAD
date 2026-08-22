# Work claim — Project metadata null persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-metadata-null-persistability-20260814-1327`
- Registered: `2026-08-14T13:27:00+07:00`
- Completed: `2026-08-14T13:29:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `9c9257d546d218ad9a5fbd0c4e8d7b88c2195d57`

## Confirmed defect

`ProjectState.Metadata` is backed by `ProjectMetadataDictionary`. Its public `IDictionary<string,string>` mutation surface accepted a runtime `null` value and stored that null unchanged. `QsdbProjectStore.Serialize(...)` later writes metadata with `x.Value ?? string.Empty`, so a supported in-memory state was silently rewritten from null to empty string during persistence. The loaded project therefore did not round-trip the state accepted by the domain boundary.

This is separate from Browser-specific null metadata parsing: the defect was the generic project metadata dictionary accepting a representation that its canonical QSDB writer does not preserve.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMetadataNullPersistabilitySmoke.cs`
- this claim file

## Implemented correction

- `3d5663786dd7045bd67b46d5720d672c95aa438b` — `fix(core): canonicalize null project metadata values`
  - incoming null metadata values become `string.Empty` before reserved-key validation and storage;
  - key identity/comparer behavior, add-only semantics, measurement-mapping reserved-key validation, removal and enumeration remain unchanged.
- `56b576be8585ff8dbcec3c824ef381a20fd74c40` — `test(core): guard null project metadata persistability`
  - indexer and `Add` null inputs expose canonical empty strings immediately;
  - non-null metadata remains unchanged;
  - `QsdbProjectStore.SaveNew` -> `Load` preserves the canonical empty representation.

## Validation

- Live `main` read-back confirms `ProjectMetadataDictionary.Set(...)` uses `var normalizedValue = value ?? string.Empty;` before the reserved mapping codec and backing dictionary mutation.
- Live `main` read-back confirms the self-registering smoke source is present with immediate-boundary and QSDB round-trip coverage.
- Ancestry was checked after concurrent merges: `3d5663786dd7045bd67b46d5720d672c95aa438b` is an ancestor of current `main`; later concurrent commits did not replace this source path.
- Executable Core smoke: `NOT_RUN` in this connector-only lane.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD runtime: `NOT_RUN` / not applicable to this Core persistence correction.

## Non-scope preserved

- no metadata key canonicality changes;
- no reserved measurement-work-item codec policy changes;
- no Browser/material/unit/native metadata changes;
- no QSDB schema or migration changes;
- no UI/native changes.
