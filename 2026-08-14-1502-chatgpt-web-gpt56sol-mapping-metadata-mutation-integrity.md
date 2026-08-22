# Agent work claim — reserved mapping metadata mutation integrity

Status: `COMPLETED`

Agent: `chatgpt-web-gpt56sol-mapping-metadata-mutation-integrity-20260814-1502`

Registered: `2026-08-14T15:02:00+07:00`

Completed: `2026-08-14T15:27:00+07:00`

Baseline `main`: `8899b1012c3ed8ccdc12c12efd107dd5cef46e53`

Priority: `P1` Core semantic/persistence-integrity hardening.

## Completed commits

- Claim: `f3e8b3f796756686fa21fcb6b338186950bad114`
- Reserved metadata owner/revision implementation: `cde542e1ba5bc1f991fc1a00b877a5e96e0d2f31`
- Mapping collection owned-write routing: `9c9b6921f5fce053ad9152d8c307784a2d3329fe`
- QSDB no-touch metadata hydration: `ca66f3dd80796a7926e5336365c41b2657f14482`
- Snapshot no-touch metadata restore: `f3bd1d722d7242d427f0d69c751bc21452697607`
- Focused regression source: `58b90c315ed9942331b5f1aff4a43852df4af2a5`

## Confirmed defect

`ProjectState.MeasurementWorkItemMappings` is canonical project-owned semantic state backed by reserved `QS3D.Mapping.v1.*` entries inside the publicly exposed `ProjectState.Metadata` dictionary. The mapping collection already advanced `ProjectState.ChangeVersion` before Add, successful Remove, and non-empty Clear, but callers could bypass that owner-aware collection by directly mutating the same reserved entries through `ProjectState.Metadata` without advancing the project revision.

That allowed different canonical measurement/work-item mapping states to carry the same `ChangeVersion`, defeating semantic freshness and persistence-dirty identity.

## Completed change

- `ProjectMetadataDictionary` is now bound to its owning `ProjectState` as part of mapping collection construction.
- Direct public Add / real indexer Set of valid reserved mapping metadata advances the project revision before the backing write; same-value reserved Set is a true no-op.
- Direct successful Remove, matching `Remove(KeyValuePair<...>)`, and Clear containing reserved mapping state advance the revision exactly once; missing/non-matching removals and ordinary metadata remain revision-neutral.
- Reserved catalog validation still runs before mutation/revision, and `ChangeVersion` overflow fails before backing state changes.
- `MeasurementWorkItemMappings` retains its existing exact-once `Touch()` policy and uses internal owned-write helpers to avoid double revision increments.
- QSDB hydration uses an internal persistence write path so loading reserved mapping metadata does not synthesize revisions.
- `ProjectStateSnapshot` replaces project metadata through an internal persistence path so clone/restore does not synthesize revisions or overflow while restoring a captured `long.MaxValue` revision.
- Mapping codec/schema and generic metadata semantics are unchanged.

## Regression coverage

Added self-registering `ProjectMappingMetadataMutationIntegritySmoke` covering:

1. direct reserved Add / real Set exact-once revision and same-value Set no-op;
2. matching/non-matching pair removal and named removal semantics;
3. reserved Clear exact-once revision and generic-only metadata revision neutrality;
4. ambiguous reserved mutation failure before project/backing-state mutation;
5. direct reserved mutation overflow before write at `ChangeVersion == long.MaxValue` while generic metadata remains allowed;
6. QSDB hydration of mapping metadata with a persisted `long.MaxValue` revision;
7. `ProjectStateSnapshot` capture/restore at `long.MaxValue` without synthetic revision changes.

## Validation actually performed

- Remote GitHub diff of `ca66f3dd80796a7926e5336365c41b2657f14482` confirms the QSDB change is limited to routing `ProjectMetadataDictionary` hydration through `SetPersistenceValue` (plus final-newline normalization).
- Remote GitHub diff of `f3bd1d722d7242d427f0d69c751bc21452697607` confirms the snapshot change is limited to replacing public Clear/indexer hydration with `ReplacePersistenceState`.
- Remote GitHub diff of `9c9b6921f5fce053ad9152d8c307784a2d3329fe` confirms the mapping collection binds the metadata owner and routes Add/Remove/Clear through internal owned-write helpers while keeping its single `project.Touch()` boundary.
- Remote readback at `1839e5ccca8cf32897e83286970ce6896932cf96` confirms the final reserved metadata implementation, QSDB hydration hook, and focused smoke source remain present.
- GitHub compare from regression `58b90c315ed9942331b5f1aff4a43852df4af2a5` to `1839e5ccca8cf32897e83286970ce6896932cf96` reports the regression as merge base; the two later concurrent commits modify only another claim and a separate relation-persistability smoke file.
- GitHub Actions: `NOT_RUN` / not dispatched.
- .NET Core smoke execution: `NOT_RUN` in this environment; no executable PASS claimed.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claimed.

## Explicit non-scope

- No mapping schema/codec format or QSDB schema-version change.
- No semantic change to mapping identity/ambiguity rules.
- No broad semantic versioning for ordinary metadata.
- No recognition/template mapping work, report/UI change, or BricsCAD/native work.

## Completion condition

Satisfied: claim-first reservation, reserved metadata ownership/revision fix, owned collection routing, hydration/rollback-safe persistence paths, focused regression source, and remote ancestry/readback verification are all present on `main`, with validation limits stated explicitly.
