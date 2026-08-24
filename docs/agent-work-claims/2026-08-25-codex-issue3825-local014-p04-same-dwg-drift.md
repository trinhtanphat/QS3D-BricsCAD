# Work claim — #3825 LOCAL-014/P04 same-DWG prompt drift

- Status: `ACTIVE / LOCAL_ONLY`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3825`
- Lane-Key: `issue-3825`
- Branch: `agent/codex/issue3825-local014-p04-drift`
- Registered: `2026-08-25T05:29:07+07:00`
- Baseline `origin/main`: `73d3750510c9c7f45652a684efff124c7b82dc5b`
- Priority: `LOCAL-014 / P1 / P04`

## Reserved runtime cell

Qualify the existing production `QS3DCONVERT2DADV` freshness boundary on licensed BricsCAD V25.2.10 x64. Each fresh exact-PID process will select one eligible Model Space LINE, reach the first Thickness prompt, prove the production command is active, induce exactly one same-DWG drift, then complete all numeric prompts:

1. edit the selected LINE endpoint while retaining its ObjectId;
2. replace the current UCS with a different planar UCS;
3. change native `INSUNITS` from the previewed policy;
4. make a project/cache appear after a projectless preview;
5. advance `ChangeVersion` on the same reviewed existing project.

Production must fail closed before `ProjectStateSnapshot`, semantic capture or native generation. Only the explicitly armed harness mutation may remain. No new ArchitecturalWall, Solid3d, audit mutation, sidecar, backup or unrelated state may be attributed to the command.

## Identity and safety requirements

- Use a clean pushed exact SHA descended from the registered current-main baseline and build canonical V25 `Release|x64` output against installed BricsCAD references.
- Require portable-PDB exact source identity, a licensed real host, zero pre-existing BricsCAD processes, exact launched-PID input, one fresh process/nonce/disposable copy per case and prompt-active proof before drift.
- Capture deterministic before/after native entity/source, CAD context, project/cache and semantic counts. Assertions must distinguish each intentional harness drift from any production mutation.
- Restore DemandLoad, environment variables, UCS/unit/project test state where applicable; preserve the repository fixture hash; clean the disposable copy, native locks, scripts, sidecars and backups; keep raw evidence ignored and publish sanitized aggregates only.

## Excluded scope

No production source edits, document switch, Paper Space switch, source delete/retype, project replacement, mid-batch failure injection, compensation/rollback, Undo/Redo, save/reopen, multi-DWG, V26, private/customer DWG, release operation or GitHub Actions dispatch. P01-P03 are already bounded PASS and will not be rerun. Any production defect is handed to a separate remote/source issue; this local lane does not patch ordinary source.

## Completion boundary

A PASS qualifies only `P04_SAME_DWG_ADV_PROMPT_DRIFT`. It does not promote overall LOCAL-014. Document/space/retype/replacement drift, ownership-scoped compensation, Undo/Redo, save/cold reopen and multi-DWG remain pending.
