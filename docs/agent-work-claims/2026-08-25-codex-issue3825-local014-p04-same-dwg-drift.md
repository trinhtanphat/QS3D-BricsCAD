# Work claim — #3825 LOCAL-014/P04 same-DWG prompt drift

- Status: `LOCAL_PASS / COMPLETE`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3825`
- Lane-Key: `issue-3825`
- Branch: `agent/codex/issue3825-local014-p04-drift`
- Registered: `2026-08-25T05:29:07+07:00`
- Baseline `origin/main`: `73d3750510c9c7f45652a684efff124c7b82dc5b`
- Latest `origin/main` read after runtime: `991fdcfb00f49d7ad5b66f38a07b304f719db53b`
- Exact pushed runtime source: `f70c694d94f26bb1cc1be8025931d9a1a6559bb0`
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

## Sanitized result

`LOCAL_PASS` on licensed BricsCAD V25.2.10 x64 for exact pushed source `f70c694d94f26bb1cc1be8025931d9a1a6559bb0`.

- The candidate passed the manual-CI/generic gate and all `1024 / 1024` aggregate preflights. Core `Release`, Core deterministic smoke, installed-reference V25 `Release|x64`, and offline WPF/Workspace/RightPanel validation all passed; both Core and V25 builds reported `0 warnings / 0 errors`.
- Exact-source identity was verified from the colocated portable PDB before host launch. The V25 adapter ProductVersion was `0.1.0-preview.10081`; adapter SHA-256 was `62043F7CDE29D082BBCD9F69E70DB4229BF8BC165881A405D2571D5F2FA264DC`; Core SHA-256 was `BE9E3AA7AF2795BBA1880DA8080F69B81A97D26453E0AD51A4CD81EB71660F05`.
- Five fresh exact-PID processes independently exercised source endpoint edit with the same ObjectId, alternate planar UCS, `INSUNITS 4 -> 6`, project/cache appearance after projectless preview, and same-project `ChangeVersion +1`. A host prompt-event probe proved drift occurred while the first Thickness `GetDouble` was active and before numeric acceptance; every case then accepted exactly `3 / 3` advanced numeric inputs.
- Every production command ended fail-closed with exactly one retained LINE, zero Solid3d, the same source ObjectId, no source XData, no production-attributable native output, semantic element, Family, audit event, sidecar, backup or lock. Project cases retained exactly the intentional in-memory project snapshot/version state and no additional production mutation.
- Each case restored its intentional source/UCS/unit/project drift, exited BricsCAD gracefully with code `0`, dismissed exactly one native discard dialog owned by the launched PID, removed the disposable drawing/script/locks, and preserved repository fixture SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- Aggregate cleanup independently confirmed zero BricsCAD processes, DemandLoad `LoadCtrls=2`, zero private drawing/script/sidecar/backup/lock residue, and zero matching Application Error, Windows Error Reporting or .NET Runtime host events during the aggregate run.
- Earlier setup/debug attempts were `HARNESS_NO_RESULT`, never product verdicts: an intermediate probe command had cleared PICKFIRST and a diagnostic `LASTPROMPT` discriminator was not suitable. The corrected ignored harness reattached the exact source ObjectId and used BricsCAD `PromptingForDouble` / `PromptedForDouble` events; only the final clean five-process aggregate above counts as evidence.

Raw marker files, disposable DWGs, scripts and local probe binaries remain under ignored `artifacts/`. No private/customer drawing or raw Handle list is committed. P01-P03 were not rerun.
