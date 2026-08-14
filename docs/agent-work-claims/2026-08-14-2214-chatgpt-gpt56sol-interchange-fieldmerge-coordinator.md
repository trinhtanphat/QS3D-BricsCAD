# Work claim — unified Project Interchange FieldMerge workflow

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-business-fieldmerge`
- Registered: `2026-08-14T22:14:00+07:00`
- Baseline main SHA: `548e8ad7c57eb7c542611b3041cafb4eee4f7aa6`
- Implementation branch: `agent/chatgpt-gpt56sol/business-fieldmerge-coordinator`
- Implementation commit SHA: `458d88165fdf7ef1018792953b6e774e7e4e7479`
- Pull request: `#1360`
- Integration batch: `integration/20260814-business-functions`
- Priority: owner requested business-function completeness from the current ~80% assessment toward 100% remote-safe coverage; issue #84 records the unified Project Interchange FieldMerge exposure gap.

## Reserved scope

Expose the existing dedicated FieldMerge planner/importer through the generic `ProjectInterchangeImportCoordinator` as an explicit, review-first mode. Preserve fail-closed authorization semantics, deterministic normalized diagnostics/counts, and the dedicated native-cleanup boundary rather than reimplementing field merge logic. Keep the canonical coordinator documentation synchronized with the implemented mode/authorization contract.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeImportCoordinator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeImportCoordinatorSmoke.cs`
- `scripts/preflight-interchange-import-coordinator.py`
- `docs/INTERCHANGE-IMPORT-COORDINATOR.md` — documentation parity for the explicit FieldMerge mode, policy/authorization path, metrics and native-cleanup boundary.
- existing `ProjectInterchangeFieldMergePlanner` / `ProjectInterchangeFieldMergeImporter` APIs are read-only dependencies unless a compile-correctness defect proves a minimal compatible adjustment is required.

## Excluded scope

- BricsCAD/V25 adapter, document/database/transaction/editor/native mutation code.
- Licensed BricsCAD runtime, private DWG, native UI, signing, installer, packaging, performance and release qualification.
- BCF/package interchange, IFC bridge behavior, other interchange modes, or unrelated issue #84 feature families.
- Any surface reserved by the active Core multicore diagnostics or CI/package-integrity claims.
- No implementation source/test/script commit directly to `main`.

## Validation plan

- Add deterministic smoke coverage for explicit FieldMerge planning through the unified coordinator.
- Prove missing or mismatched FieldMerge execution authorization fails closed and exact reviewed-plan authorization succeeds.
- Preserve dedicated FieldMerge source/target choice counts, blockers, diagnostics and native cleanup requirements in the normalized coordinator plan/result.
- Reject unsupported provenance combinations explicitly rather than silently ignoring them.
- Extend the source preflight so future regressions cannot remove the unified FieldMerge mode/delegation/authorization path unnoticed.
- Keep `docs/INTERCHANGE-IMPORT-COORDINATOR.md` aligned with the actual five-mode coordinator and the separate FieldMerge authorization contract.
- Inspect the final agent/integration diff against refreshed `main`; use repository exact-main CI evidence for compile/smoke proof after the source lane joins the combined candidate.

## Coordination

Fresh `main` and open-PR checks at registration found no FieldMerge/`ProjectInterchangeImportCoordinator` implementation PR. The newest active claims reserve `ComprehensiveModelHealthService` multicore diagnostics and CI/package integrity surfaces, which do not overlap this lane. If a concurrent claim begins reserving one of the expected write surfaces above, this lane stops and reconciles before further writes.

Scope amendment on 2026-08-14 after source integration: the canonical coordinator documentation was found stale because it still listed only the four pre-FieldMerge modes. This claim is amended on `main` before editing that added documentation surface, per the scope-expansion rule. The amendment does not expand into other issue #84 features.

## Handoff evidence

- Claim-only main commit: `58420f009381a4d38ca8bb5ae9e7e7743c8ed8d8`.
- Atomic implementation commit: `458d88165fdf7ef1018792953b6e774e7e4e7479` on `agent/chatgpt-gpt56sol/business-fieldmerge-coordinator`.
- Agent PR: `#1360`, squash-integrated into `integration/20260814-business-functions` as `9729a408f69e6cd17abc8764f0b48054226dcbc6`.
- Final source integration PR: `#1363`; main landing SHA: `cf786992754c2d8c7cc0d8a471280a6ac9d539e1`.
- Automatic post-integration dispatcher run: `31816166954` for the integrated main tree; exact-main V25 cloud result remains pending while this documentation parity addendum is prepared.
- Repository Core CI is `workflow_dispatch`-only, so the earlier PR/agent head had no automatic build/smoke status; empty status was not treated as PASS.
- Container cloning in this session is unavailable because outbound DNS to GitHub is disabled; no local `dotnet` PASS is claimed.

## Completion condition

The generic coordinator exposes an explicit FieldMerge mode backed by the existing canonical FieldMerge planner/importer; reviewed-plan authorization remains fail-closed; deterministic smoke/preflight coverage protects the workflow; canonical coordinator documentation matches the implemented contract; the implementation is represented in final `main`; exact-main automated remote-safe evidence is recorded; and the claim is then marked `COMPLETED`. LOCAL_ONLY BricsCAD/runtime gates remain separate and are not represented as remote completion evidence.
