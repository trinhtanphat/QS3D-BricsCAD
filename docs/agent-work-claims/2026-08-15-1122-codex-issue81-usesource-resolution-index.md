# UseSource interchange indexed resolution claim

- Status: COMPLETED
- Agent: Codex `/root/audit_performance_next`
- Registered: 2026-08-15 11:22 +07:00
- Baseline `main`: `54b43af253a4b22565e41d0dece86f0ecf307d75`
- Issue: #81
- Claim branch: `agent/codex/issue81-usesource-resolution-index-claim-20260815`
- Claim merge: `82c19d8d51f0035425e29b7b1043d94760316b8f`
- Implementation baseline `main`: `079e0e760cc0eac8704909ab042228641c703f4d`
- Implementation branch: `agent/codex/issue81-usesource-resolution-index-impl-20260815`
- Implementation commit: `0c045a667eb424f60a980cbac656df9c22e26ad7`
- Implementation PR: #1589
- Integration-branch merge: `3cd4140bbd88199d8bdf1d6f4331bc2ec82f39c2`
- Main integration PR: #1597
- Main integration merge: `e2dbb1e03748047f69a556240f8f85b2e7ccc17e`
- Closeout baseline `main`: `a81bc3e771bdc10c2fbb794b5a9dcb1508ee6e66`

## Defect

`ProjectInterchangeUseSourceSemanticImporter.Import(...)` iterates every validated source Zone, Floor, Family, and Element. For each identity, its private `ShouldAdd(...)` runs `plan.Items.Single(...)` over the complete resolution plan. A valid 20,000-Element same-category source/target collision import with no generated handles stays within the 100,000 Element, 250,000 total-identity, and 16 MiB JSON snapshot bounds, yet its executable UseSource decisions require roughly `N(N+1)/2 = 200,010,000` resolution-item predicate checks. Planning has already authorized and validated exactly one executable resolution item per source identity, so rescanning the full plan during each mutation is unnecessary quadratic work.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeUseSourceSemanticImporter.cs`: after existing plan/cleanup authorization and validation, build one private case-insensitive `(kind, id)` resolution-action index and replace only the four Zone/Floor/Family/Element per-identity full-plan scans with indexed lookup.
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceSemanticImporterSmoke.cs` or one separate auto-registered focused smoke: preserve mixed-case Add/UseSource behavior and resulting state.
- One focused remote-safe structural preflight under `scripts/` may pin the private index, its placement after authorization/validation, and removal of the four mutation-loop scans.
- This claim record.

## Preservation and exclusions

Preserve cleanup authorization and requirements, valid-plan exception behavior, deterministic source ordering, UseSource/Add choices, plan/result counts, active context, affected closure/invalidation, metadata/audit, target validation, atomic rollback, public APIs, and all existing messages. The index is private to `ProjectInterchangeUseSourceSemanticImporter` and consumes only the already-authorized/validated plan.

Do not modify the shared planner, KeepTarget importer, FieldMerge planner/importer, any other importer, V25/V26/native adapters, UI, licensed runtime evidence, LOCAL/private data, release surfaces, workflows, GitHub Actions, PR #1543/#1556 lanes, or the completed RateBook/KeepTarget/Schedule/Rebar-bounds lanes. Do not claim native or end-to-end timing improvement from remote structural evidence.

## Validation plan

- Run the focused structural preflight and focused UseSource semantic smoke coverage.
- Run existing UseSource semantic and interchange resolution-contract gates.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release with zero warnings/errors.
- Run the full Core smoke executable.
- Run the repository remote-safe preflight aggregate without dispatching or modifying GitHub Actions.
- Refresh `origin/main`, re-audit collisions, inspect the final diff, push the implementation branch, and open a PR; stop before merge and keep broad issue #81 open.

## Implementation branch evidence before integration

- Focused `preflight-interchange-usesource-resolution-index.py`: PASS.
- Existing UseSource-all, UseSource-semantic, import-resolution, and resolution-contract preflights: PASS.
- `QS3D.Core` Release build: PASS, 0 warnings / 0 errors.
- `QS3D.Core.SmokeTests` Release build: PASS, 0 warnings / 0 errors.
- At the implementation baseline, the full smoke executable was blocked before the UseSource registration by `FloorGeneratedIdentityUnicodeSmoke.MalformedNamesAreRejected`: `FloorDefinition` rejected the malformed name in its constructor, outside the implementation diff, before that smoke's later exception assertion.
- At that baseline, aggregate remote-safe preflight included and passed the new focused gate, then reported four failures outside the implementation diff: Direct Draw, Level native-host placement, drawing-identity touch order, and Floor/Zone mutation-integrity token checks.
- No GitHub Actions, native/BricsCAD runtime, LOCAL/private data, release, or workflow operation was used.

## Completion

`COMPLETED`: claim PR #1584 merged at `82c19d8d51f0035425e29b7b1043d94760316b8f`. Implementation PR #1589, carrying implementation commit `0c045a667eb424f60a980cbac656df9c22e26ad7`, merged into the owner integration branch at `3cd4140bbd88199d8bdf1d6f4331bc2ec82f39c2`. Main integration PR #1597 then merged normally at exact `main` SHA `e2dbb1e03748047f69a556240f8f85b2e7ccc17e`.

The integrated importer builds one private case-insensitive resolution-action index after the existing cleanup-authorization and plan-validation boundary, then uses it for the Zone, Floor, Family, and Element mutation selections instead of repeating full-plan scans. Cleanup authorization/requirements, valid-plan exception messages and ordering, Add/UseSource choices, deterministic mutation ordering, counts, active context, affected closure/invalidation, metadata/audit, target validation, immutable rollback snapshot, public APIs, and approved exclusions remain preserved.

Independent validation on exact main merge `e2dbb1e03748047f69a556240f8f85b2e7ccc17e` reported:

- focused UseSource resolution-index gate: PASS;
- `QS3D.Core` Release build: PASS, 0 warnings / 0 errors;
- `QS3D.Core.SmokeTests` Release build: PASS, 0 warnings / 0 errors;
- full Core smoke executable: `ALL PASS`;
- V25 installed-reference build: PASS, 0 warnings / 0 errors;
- aggregate remote-safe preflight: PASS, all 824 discovered gates.

This narrow claim is released. Broad issue #81 remains **OPEN** for its other performance qualification and optimization work. No native timing, BricsCAD runtime, LOCAL/private-data, release, workflow, or GitHub Actions result is claimed by this closeout.
