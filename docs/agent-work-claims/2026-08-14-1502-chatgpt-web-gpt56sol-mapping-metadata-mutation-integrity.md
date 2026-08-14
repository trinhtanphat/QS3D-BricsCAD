# Agent work claim — reserved mapping metadata mutation integrity

Status: `ACTIVE`

Agent: `chatgpt-web-gpt56sol-mapping-metadata-mutation-integrity-20260814-1502`

Registered: `2026-08-14T15:02:00+07:00`

Baseline `main`: `8899b1012c3ed8ccdc12c12efd107dd5cef46e53`

Priority: `P1` Core semantic/persistence-integrity hardening.

## Confirmed defect

`ProjectState.MeasurementWorkItemMappings` is canonical project-owned semantic state backed by reserved `QS3D.Mapping.v1.*` entries inside the publicly exposed `ProjectState.Metadata` dictionary. The mapping collection correctly advances `ProjectState.ChangeVersion` before Add, successful Remove, and non-empty Clear.

However, callers can bypass that owner-aware collection by mutating the same reserved entries directly through `ProjectState.Metadata`: valid reserved Add/indexer Set, successful Remove, `Remove(KeyValuePair<...>)`, and Clear currently mutate the mapping backing store without advancing the project revision. That permits different canonical measurement/work-item mapping states to carry the same `ChangeVersion`, defeating semantic freshness and persistence-dirty identity.

The existing reserved Set/Add validation already fail-closes malformed or ambiguous catalogs, so this lane is specifically about semantic ownership/revision atomicity and persistence hydration/rollback safety.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs`
- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCollection.cs`
- `src/QS3D.Core/Domain/ProjectState.cs` only construction/binding needed for owned metadata
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs` only metadata hydration needed to avoid synthetic revision changes
- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs` only metadata restore needed to avoid synthetic revision changes/overflow
- new focused `tests/QS3D.Core.SmokeTests/ProjectMappingMetadataMutationIntegritySmoke.cs`
- this claim file

## Acceptance

1. Direct public Add or real indexer Set of a valid reserved mapping entry advances project `ChangeVersion` exactly once before the backing write; same-value Set remains a true no-op.
2. Direct successful Remove / matching `Remove(KeyValuePair)` advances the revision exactly once; missing/non-matching removal remains a no-op.
3. Direct Clear containing reserved mapping state advances the revision exactly once; generic-only metadata mutations remain revision-neutral.
4. `ChangeVersion` overflow and existing reserved-catalog validation failures occur before direct reserved metadata changes.
5. Normal `MeasurementWorkItemMappings` Add/Remove/Clear remain exact-once revision mutations, not double-touched.
6. QSDB load preserves the persisted `ChangeVersion` even when reserved mapping metadata is hydrated, including `long.MaxValue` fixtures.
7. `ProjectStateSnapshot` clone/restore can hydrate/replace mapping metadata without synthetic revision changes or overflow, while restoring the captured revision exactly.
8. Mapping codec/schema, generic metadata behavior, MAP-02/03 business logic, and native boundaries remain unchanged.

## Explicit non-scope

- No mapping schema/codec format or QSDB schema-version change.
- No semantic change to mapping identity/ambiguity rules.
- No broad semantic versioning for ordinary metadata.
- No recognition/template mapping work, report/UI change, or BricsCAD/native work.

## Evidence / history

- Prior completed MAP-01 mapping mutation ChangeVersion lane fixed the owner-aware `MeasurementWorkItemMappings` path but explicitly excluded broad `ProjectMetadataDictionary` versioning.
- Live baseline `ProjectMetadataDictionary` validates reserved Add/Set but directly exposes Remove/Clear/Remove-pair and performs no project revision callback.
- Live mapping collection touches the project before its own backing writes, proving the reserved namespace is semantic state rather than generic presentation metadata.
- No matching current commit-history/branch reservation was found for reserved mapping metadata mutation integrity.

## Validation plan

- Add focused self-registering Core smoke for direct reserved Add/Set/Remove/Remove-pair/Clear, exact-once/no-op revision semantics, overflow-before-write, generic metadata neutrality, QSDB max-version hydration, and snapshot restore.
- Re-read every changed source/test file from remote `main` and verify ancestry after concurrent reconciliation.
- GitHub Actions: `NOT_RUN` / do not dispatch.
- .NET Core smoke execution: `NOT_RUN` unless an executable runner becomes available in this environment.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claim.

## Completion condition

Claim-only reservation is visible on remote `main`; implementation + focused regression are reconciled against current `main`; remote source/test readback verifies final state; then this claim is changed to `COMPLETED` with exact commit SHAs and only validation actually performed.
