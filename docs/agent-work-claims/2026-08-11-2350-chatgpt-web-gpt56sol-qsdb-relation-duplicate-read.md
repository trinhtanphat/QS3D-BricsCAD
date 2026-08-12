# Work claim — QSDB duplicate relation/source identity read guard

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-qsdb-relation-duplicate-read`
- Registered: `2026-08-11T23:50:00+07:00`
- Baseline main SHA: `c10867cbc6e6be2e61f5b01b09f93146e16c3e1b`
- Priority: persisted provenance/dependency ambiguity found during owner-requested continue-all audit

## Confirmed defect

QSDB validation rejected blank/padded source handles and dependency ids, but did not reject duplicates within one element. A tampered file could therefore load values such as `AB12` + `ab12` in `SourceHandles` or `E0` + `e0` in `DependsOn`. These collections represent identity/provenance and dependency edges, so case-insensitive duplicate entries are redundant/ambiguous persisted state rather than separate semantic facts.

## Reserved scope

Harden current-schema XML validation so source handles and dependency ids for each element are unique under `StringComparer.OrdinalIgnoreCase`, after existing canonical-text validation. Preserve input order and all unique canonical values. Runtime authoring APIs and dependency graph algorithms remain unchanged.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPersistedRelationDuplicateReadSmoke.cs`
- this claim file

## Excluded scope

- No primary semantic ID duplicate handling; project/family/element/rule duplicate IDs already have separate guards.
- No cross-element generated-handle ownership analysis.
- No `DependencyGraph`, source reconcile, native CAD ownership, Save/SaveAs or V25 runtime changes.
- No schema-version bump/migration rewrite.
- No GitHub Actions dispatch.

## Delivered behavior

- Each element's persisted source-handle list is checked case-insensitively for duplicate identities after canonical-text validation.
- Each element's persisted dependency list receives the same duplicate check.
- Unique canonical lists retain their original order and values.
- The check is deliberately scoped to each list; it does not invent a cross-element ownership rule.

## Commits

- Registration: `041a5bb9c3f88cb25c99ec6d5179f470b6d1e1c0` — `chore(agent): claim qsdb duplicate relation/source read`.
- Implementation: `c556833316286273dd679f9c6b1d6c8674692d3d` — `fix(persistence): reject duplicate persisted relation lists`.
- Regression: `5d7aae46009bbfaa40bfc5d0871239f6c8e70ca4` — `test(persistence): guard duplicate persisted relation lists`.

## Validation actually performed

- Inspected the exact implementation commit diff; only two per-list case-insensitive identity sets and their duplicate failures were added.
- Re-fetched the focused smoke from current remote `main`; it covers duplicate source handles, duplicate dependencies and unchanged order for unique canonical lists.
- Smoke auto-registers with a module initializer and does not touch the shared registration file.
- No force-push was used and concurrent unrelated work remained intact.
- No GitHub Actions were dispatched.
- This hosted environment has no local .NET SDK/compiler and no licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This persistence/Core change does not create a new native runtime scenario.

## Completion condition

Satisfied: current `main` rejects same-element case-insensitive duplicate persisted source handles/dependencies before object materialization, focused deterministic regression coverage is present, and this claim is closed `COMPLETED`.
