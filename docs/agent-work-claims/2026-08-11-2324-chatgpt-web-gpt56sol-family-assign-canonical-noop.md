# Work claim — Family assignment canonical no-op identity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `11486e07d818ec1df718f3775a4b3c23e15123da`
- Priority: evidence-driven remote-safe Core mutation correctness

## Reason

`ProjectFamilyService.Assign()` resolves previous Family references using a trimmed `FamilyId`, and `ResolveFamilyMembers()` also uses trimmed case-insensitive identity, but its early no-op check compares raw `element.FamilyId` directly with the target ID. An element whose mutable `FamilyId` is `" TARGET "` is therefore treated as a real reassignment to Family `TARGET`, causing unnecessary `ProjectState.Touch()`, `FamilyId` rewrite, dirty flags, and changed-count reporting even though semantic ownership is already the target Family.

## Reserved scope

Use the same canonical trimmed/case-insensitive Family identity for the `Assign()` no-op decision. Preserve stored padded identity on true no-op, target/default validation, corrupt previous-Family fail-closed behavior, assignment of genuinely different Families, instance overrides, dirty semantics, and batch atomicity. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No Workspace/WPF/native UI changes.
- No Family delete/property propagation changes.
- No project schema or persistence-format changes.
- No BricsCAD V25 runtime work.
- No GitHub Actions dispatch/workflow edit.

## Validation plan

- Re-fetch exact current source/test blobs before implementation.
- Add a regression with `FamilyId` mutated after construction to padded/case-varied target identity, mark the element clean, then call `Assign()`.
- Assert changed count is zero, project ChangeVersion/timestamp remain unchanged, stored `FamilyId` remains untouched, properties/dirty/timestamp remain untouched.
- Preserve all existing assignment atomicity regressions.
- Re-check moving `main` for target-file overlap before PR/merge.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

Recent Family work covers active-Family delete canonicalization and target default validation. No active claim or recent commit was found for the `Assign()` semantically-identical target no-op check. Current active Grid/Quantity/Release work is on separate surfaces.

## Completion condition

Semantically identical padded/case-varied target Family assignments are true no-ops without persistence or element mutation, regression coverage is on `main`, and this claim is marked `COMPLETED` with exact SHAs.