# Work claim — Floor/Zone mutation canonicality and target integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:53:00+07:00`
- Completed: `2026-08-12T00:00:03+07:00`
- Baseline main SHA observed: `e7f718ff50569b20c42ba2b894d12cdb06b36746`
- Claim commit: `52ee71a169e743578468edd04e9703ff6c03d80a`
- PR: `#545`
- Squash merge on `main`: `a19e4033ccdc2deaa3f947dd486260fe59d17d95`
- Priority: P1 — deterministic Core mutation correctness, no native BricsCAD dependency.

## Defects closed

1. `ProjectFloorService.SetActive()` and `ProjectZoneService.SetActive()` previously compared mutable stored active ids raw against resolved canonical ids, causing padded/case-varied same-item requests to touch and rewrite project state unnecessarily.
2. Floor/Zone `Assign()` previously compared mutable element relation ids raw against resolved canonical ids, so semantically identical padded/case-varied relations could be rewritten, dirtied and counted as changes.
3. Floor/Zone object-target assignment previously silently skipped caller-supplied null targets, allowing an incomplete requested batch to proceed rather than failing closed before mutation.

## Implemented

- `ProjectFloorService.SetActive()` now compares trimmed stored active identity before `Touch()`.
- `ProjectZoneService.SetActive()` now compares trimmed stored active identity before `Touch()`.
- `ProjectFloorService.Assign()` now treats trimmed/case-varied same-Floor relation identity as a true no-op.
- `ProjectZoneService.Assign()` now treats trimmed/case-varied same-Zone relation identity as a true no-op.
- Floor shared owned-target resolution now rejects null before ownership dereference; because the resolver is shared, Floor vertical-level and clear-level mutations inherit the same fail-closed target-set integrity.
- Zone assignment now rejects null inline before any semantic write.
- Added `ProjectFloorZoneMutationIntegritySmoke` with six deterministic regressions and isolated module registration.
- Added `scripts/preflight-project-floor-zone-mutation-integrity.py`.
- Added detailed plan `docs/plans/2026-08-11-floor-zone-mutation-integrity.md`.

## Regression contract

- Padded/case-varied active Floor/Zone identity is a no-op: no `ChangeVersion` churn and raw stored identity is preserved.
- Padded/case-varied element Floor/Zone assignment to the same semantic target returns `0`, preserves raw relation identity, dirty flags, `UpdatedUtc`, and project version.
- `[ownedElement, null]` Floor/Zone assignment batches throw before mutation and leave relation, dirty flags, timestamp and project version unchanged.
- Existing exact-instance ownership, duplicate-id validation, vertical-level semantics and real-assignment dirty flags remain unchanged.

## Moving-main safety

- Source was re-fetched after claim at `04c2b48dd420a3a635612876926c64080816f8e1` before writes.
- Before PR creation, `main` had advanced 47 commits from that branch point; compare showed no overlap with this lane.
- Four additional commits landed before PR metadata stabilized; again no overlap.
- Eighteen more commits landed before merge; compare again showed no overlap with the six PR files.
- PR #545 was squash-merged with expected head `e3e0a419c4f7d51e09a6330264cf2d6222d454bd`; no force update was used.

## Validation

- PR #545 changed exactly six expected files: two Core services plus the new smoke, module registration, focused preflight and detailed plan.
- GitHub reported the final PR head mergeable before merge.
- Source/diff review caught and corrected an initial preflight bug before merge: the Floor null guard lives in shared `ResolveOwnedElements()`, not directly inside `Assign()`; the final gate validates resolver-before-`Touch()` ordering and null-before-ownership-dereference ordering correctly.
- A direct container checkout/test attempt was made, but the execution environment could not resolve `github.com`; therefore no smoke/preflight runtime execution is claimed.
- No GitHub Actions workflow was dispatched, in accordance with repository manual-only policy.
- No BricsCAD V25 runtime PASS is claimed; this lane is pure Core semantic logic and introduces no native CAD surface.

## Completion evidence

PR #545 is merged on `main` as `a19e4033ccdc2deaa3f947dd486260fe59d17d95`. Floor/Zone activation and assignment now use canonical semantic identity for no-op decisions, and null-containing object-target batches fail closed before mutation with committed deterministic regression and static-gate coverage.