# Work claim — QSDB duplicate relation/source identity read guard

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-qsdb-relation-duplicate-read`
- Registered: `2026-08-11T23:50:00+07:00`
- Baseline main SHA: `c10867cbc6e6be2e61f5b01b09f93146e16c3e1b`
- Priority: persisted provenance/dependency ambiguity found during owner-requested continue-all audit

## Confirmed defect

Current QSDB validation rejects blank/padded source handles and dependency ids, but it does not reject duplicates within one element. A tampered file may therefore load values such as `AB12` + `ab12` in `SourceHandles` or `E0` + `e0` in `DependsOn`. These collections represent identity/provenance and dependency edges, so case-insensitive duplicate entries are ambiguous/redundant persisted state rather than separate semantic facts.

## Reserved scope

Harden current-schema XML validation so source handles and dependency ids for each element are unique under `StringComparer.OrdinalIgnoreCase`, after existing canonical-text validation. Preserve input order and all unique canonical values. Do not change runtime authoring APIs or dependency graph algorithms.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPersistedRelationDuplicateReadSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No primary semantic ID duplicate handling; project/family/element/rule duplicate IDs already have separate guards.
- No cross-element generated-handle ownership analysis.
- No `DependencyGraph`, source reconcile, native CAD ownership, Save/SaveAs or V25 runtime changes.
- No schema-version bump/migration rewrite.
- No GitHub Actions dispatch.

## Validation plan

- Same-element source handles differing only by case fail load.
- Same-element dependencies differing only by case fail load.
- Same textual identity may still appear in different elements where the schema allows it; this lane only prevents duplicates within one list.
- Unique canonical handle/dependency lists continue to load in original order.
- Inspect exact implementation diff and read back current remote source/test after integration; never force-push.

## Coordination

The immediately preceding persisted relation canonicality lane is completed and upstream. Search of recent commits found no current claim for duplicate QSDB source-handle/dependency lists; current concurrent claims are on unrelated revision/UI/rebar/installer/export surfaces.

## Completion condition

Current `main` rejects same-element case-insensitive duplicate persisted source handles/dependencies before object materialization, focused deterministic regression coverage is present, and this claim is closed `COMPLETED` with exact commits and actual validation scope.
