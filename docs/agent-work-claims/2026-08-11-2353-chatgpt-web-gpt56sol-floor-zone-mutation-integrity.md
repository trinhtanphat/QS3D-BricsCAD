# Work claim — Floor/Zone mutation canonicality and target integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:53:00+07:00`
- Baseline main SHA observed: `e7f718ff50569b20c42ba2b894d12cdb06b36746`
- Priority: P1 — deterministic Core mutation correctness, no native BricsCAD dependency.

## Confirmed defects

`ProjectFloorService` and `ProjectZoneService` already canonicalize mutable Floor/Zone references in read/delete safety paths, but several mutation paths still use weaker raw identity or silently shrink caller target sets:

1. `SetActive()` compares raw `ActiveFloorId` / `ActiveZoneId` with the canonical resolved id. Padded/case-varied semantic identity therefore performs an unnecessary `ProjectState.Touch()` and rewrites the stored active id even though the active item has not changed.
2. `Assign()` compares raw `ProjectElement.FloorId` / `ZoneId` with the canonical resolved id. A padded/case-varied reference to the same Floor/Zone is reported as a change, touches persistence state, rewrites the relation, timestamps the element and marks it dirty.
3. Object-target assignment silently executes `continue` for a caller-supplied null element. A requested batch such as `[ownedElement, null]` can therefore mutate the valid element and report success instead of rejecting the incomplete requested target set atomically.

These behaviors are inconsistent with the repository's current canonical lookup/reference semantics and with the recently hardened Family assignment/bulk target integrity contracts.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneMutationIntegritySmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneMutationIntegritySmokeRegistration.cs` (new)
- `scripts/preflight-project-floor-zone-mutation-integrity.py` (new)
- `docs/plans/2026-08-11-floor-zone-mutation-integrity.md` (new detailed implementation plan)
- this claim file for close-out

## Detailed implementation plan

### Phase 1 — prove and preserve boundaries

- Re-fetch exact current `main` and the two service blobs before any source write.
- Re-check recent claims/commits for Floor/Zone source overlap.
- Preserve the completed Floor/Zone canonical-reference safety work, vertical Bottom/Top Level semantics, duplicate-id fail-closed behavior, exact project-instance ownership and existing dirty flags.

### Phase 2 — canonical no-op semantics

- In `SetActive()`, compare trimmed stored active id against the resolved canonical id case-insensitively before touching the project.
- In `Assign()`, compare trimmed current element Floor/Zone id against the resolved canonical id case-insensitively.
- Treat a semantic same-target assignment as a true no-op: zero changed count, no `Touch()`, no relation rewrite, no dirty/timestamp mutation.
- Do not normalize unrelated data or introduce a persistence migration.

### Phase 3 — fail-closed target validation

- Reject any null caller-supplied object target before semantic mutation instead of silently skipping it.
- Preserve same-project exact-instance ownership checks and case-insensitive target deduplication.
- Ensure a batch containing an owned target plus a null target leaves all semantic and persistence state unchanged.

### Phase 4 — deterministic regression coverage

- Add Floor active canonical no-op regression.
- Add Zone active canonical no-op regression.
- Add Floor assignment canonical no-op regression preserving stored padded identity and element/project state.
- Add Zone assignment canonical no-op regression with the same guarantees.
- Add Floor null-containing assignment batch atomicity regression.
- Add Zone null-containing assignment batch atomicity regression.
- Module-register the smoke without editing the shared registration hotspot.

### Phase 5 — static guard and moving-main integration

- Add an auto-discovered focused preflight requiring trimmed canonical no-op comparisons and explicit null rejection, and forbidding the legacy silent null skip in the reserved assignment paths.
- Implement on an isolated agent branch, compare moving `main` before merge, and rebuild/rebase only if target files have not changed concurrently.
- Squash/merge to `main` using expected head SHA; never force-update `main`.
- Close this claim with exact commit/PR/main SHAs.

## Exclusions

- No WPF/Workspace/native CAD changes.
- No project schema or persistence-format migration.
- No Floor vertical-level policy changes.
- No quantity/reporting/Room/updater/release changes.
- No GitHub Actions dispatch.
- No BricsCAD V25 runtime PASS claim.

## Validation level

Source/static review plus committed CAD-independent Core regression coverage and focused preflight. Native BricsCAD runtime qualification is not required for this pure Core lane; no Actions will be run without separate explicit authorization.

## Completion condition

Floor/Zone assignment and activation use canonical semantic identity for no-op decisions, null-containing object-target batches fail closed before mutation, deterministic regression/preflight coverage is on `main`, and this claim is marked `COMPLETED` with exact evidence.