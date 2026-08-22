# Work claim — Core Family assignment member property-map preflight

- Status: `COMPLETED`
- Agent: `gpt56sol-family-assign-member-map-20260814-0802`
- Registered: `2026-08-14T08:02:00+07:00`
- Completed: `2026-08-14T08:07:00+07:00`
- Initial observed main SHA: `a16482b88ec76c2c9942059a003cc09230302c01`
- Claim publication commit: `39f5368f5c94748864882df12115a0eace1a4228` (published after concurrent Rebar-only parent `e010d1a5cf2cf64ae10a2d61d0331e1adf3350bb`)
- Priority: Core model / Family assignment integrity.

## Confirmed defect

`ProjectFamilyService.Assign()` validated target/previous Family default maps, ownership, category and target-enumeration freshness, but actual pending elements' own `Properties` maps were not canonicality-preflighted before `ProjectState.Touch()` and inherited-default rewrite. A legacy/directly-mutated pending element with a padded or blank property key could therefore be reassigned while retaining malformed state and receiving canonical target defaults.

## Implemented scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`: after the existing `pending.Count == 0` no-op exit and before `project.Touch()`, actual pending members are passed through the existing member property-key canonicality preflight.
- Blank, padded and canonical-colliding pending member keys now fail closed before any assignment mutation.
- Already-assigned elements remain true no-ops and are intentionally not preflighted by this mutation guard.
- Canonical previous-default removal, target-default application and explicit override preservation remain unchanged.
- `tests/QS3D.Core.SmokeTests/FamilyAssignmentMemberPropertyMapPreflightSmoke.cs`: focused self-registering regression covers padded/blank pending maps, atomic rejection, malformed already-assigned no-op semantics, and canonical inheritance/override behavior.

## Coordination and commits

- Claim-first commit: `39f5368f5c94748864882df12115a0eace1a4228`.
- Production fix: `aac7de056b08023e40028ffd397d537b00751660`.
- Focused regression: `040aa9374637642839f2f32434eac7858e13d779`.
- Concurrent Rebar, IFC and V25/release commits were preserved. Detached commits built against stale heads were not pushed; publication used the current `main` lineage without force updates.

## Excluded scope

No `BulkEditService`, Family Manager/UI, persistence schema, Cost/Measurement, MAP/IFC behavior, Rebar, V25 release/source-handle/native surfaces, or other agent-owned capability was changed.

## Validation actually executed

- Re-fetched `main` repeatedly during concurrent updates and re-verified the claimed production file remained on the expected pre-fix blob before publication.
- Read back the production commit diff: exactly one production line was added, invoking member-map preflight after the assignment no-op exit and before `project.Touch()`.
- Read back the dedicated regression commit and verified all four focused scenarios are present.
- Verified `aac7de056b08023e40028ffd397d537b00751660` is an ancestor of regression/current lineage; compare reported `behind_by = 0`.
- GitHub returned no combined status checks and no associated workflow runs for regression SHA `040aa9374637642839f2f32434eac7858e13d779`.
- No managed executable smoke/build or licensed BricsCAD/native runtime validation was executed in this lane, so none is reported as PASS.

## Completion condition

Satisfied for this bounded Core lane: malformed actual assignment-member property maps fail closed before mutation, no-op and ordinary Family assignment semantics are preserved by construction and focused regression source, the fix/regression are on remote `main`, concurrent work was retained, and unavailable runtime/native gates remain explicitly unclaimed.
