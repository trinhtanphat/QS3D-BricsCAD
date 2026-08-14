# Work claim — unified Project Interchange FieldMerge workflow

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-business-fieldmerge`
- Registered: `2026-08-14T22:14:00+07:00`
- Baseline main SHA: `548e8ad7c57eb7c542611b3041cafb4eee4f7aa6`
- Implementation branch: `agent/chatgpt-gpt56sol/business-fieldmerge-coordinator`
- Integration batch: `integration/20260814-business-functions`
- Priority: owner requested business-function completeness from the current ~80% assessment toward 100% remote-safe coverage; issue #84 records the unified Project Interchange FieldMerge exposure gap.

## Reserved scope

Expose the existing dedicated FieldMerge planner/importer through the generic `ProjectInterchangeImportCoordinator` as an explicit, review-first mode. Preserve fail-closed authorization semantics, deterministic normalized diagnostics/counts, and the dedicated native-cleanup boundary rather than reimplementing field merge logic.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeImportCoordinator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeImportCoordinatorSmoke.cs`
- `scripts/preflight-interchange-import-coordinator.py`
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
- Inspect the final agent-branch diff against refreshed `main`; use repository CI/integration evidence for compile/smoke proof after the lane joins the combined candidate.

## Coordination

Fresh `main` and open-PR checks at registration found no FieldMerge/`ProjectInterchangeImportCoordinator` implementation PR. The newest active claims reserve `ComprehensiveModelHealthService` multicore diagnostics and CI/package integrity surfaces, which do not overlap this lane. If a concurrent claim begins reserving one of the three expected write surfaces above, this lane stops and reconciles before further writes.

## Completion condition

The generic coordinator exposes an explicit FieldMerge mode backed by the existing canonical FieldMerge planner/importer; reviewed-plan authorization remains fail-closed; deterministic smoke/preflight coverage protects the workflow; the implementation branch SHA is recorded and accepted into the declared integration batch; the final integrated `main` contains the intended behavior and the claim is then marked `COMPLETED`. LOCAL_ONLY BricsCAD/runtime gates remain separate and are not represented as remote completion evidence.
