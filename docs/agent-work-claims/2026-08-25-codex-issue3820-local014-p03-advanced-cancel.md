# Work claim — #3820 LOCAL-014/P03 advanced prompt cancellation

- Status: `LOCAL_PASS / P03 COMPLETE`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3820`
- Lane-Key: `issue-3820`
- Branch: `agent/codex/issue3820-local014-p03-adv-cancel`
- Registered: `2026-08-25T04:37:58+07:00`
- Baseline `origin/main`: `2589d00dd0842cfe9a529edeb3dfcd235e0d36cb`
- Priority: `LOCAL-014 / P1 / P03`

## Reserved runtime cell

Qualify the existing production `QS3DCONVERT2DADV` command on licensed BricsCAD V25.2.10 x64 by reaching each of its three numeric prompts and cancelling with physical ESC delivered only to the exact process launched by the guarded local runner:

1. cancel Thickness;
2. accept Thickness, then cancel Height;
3. accept Thickness and Height, then cancel BottomOffset.

Each case uses an eligible Model Space LINE in a disposable non-customer DWG. It must return to `CMDACTIVE=0`, preserve the source and native object counts, and create no project/cache/sidecar, semantic element, generated Solid3d, audit mutation, backup/lock/script residue, or cross-DWG mutation.

## Identity and safety requirements

- Candidate must be a clean pushed SHA descended from the registered baseline.
- Build the canonical V25 `Release|x64` output against installed BricsCAD references and require portable-PDB SourceLink identity for that exact SHA.
- Require zero pre-existing BricsCAD processes, exact launched-PID input, fixed DemandLoad restoration, unchanged disposable fixture hash, graceful host cleanup, restored environment values and ignored raw evidence.
- Publish only sanitized aggregates, exact Git SHA, host/product identity, plugin/Core hashes and cleanup results.

## Excluded scope

No production source edits, preview-context drift, forced mid-batch rollback, Undo/Redo, save/reopen, multi-DWG, V26, private/customer DWGs, release operation or GitHub Actions. P01/P02 are already bounded PASS and will not be rerun. Any runtime product defect is handed to a separate remote/source issue; this local lane does not patch ordinary source.

## Completion boundary

A PASS qualifies only `P03_ADV_PHYSICAL_ESC_PROMPTS`. It does not promote overall LOCAL-014, whose drift, rollback, Undo/Redo, save/reopen and multi-DWG matrix remains pending.

## Exact candidate and build gates

- Tested source/candidate: `1736ae8db0086041f0b1e8ce4b79839469b10061` (clean, pushed and equal to its upstream before runtime).
- Registered baseline: `origin/main@2589d00dd0842cfe9a529edeb3dfcd235e0d36cb`.
- Pre-publication refresh advanced `origin/main` to `73d3750510c9c7f45652a684efff124c7b82dc5b`. Those intervening commits changed only QuantityEvidence/Curtain fingerprint source and their tests, not Plan-to-3D, the V25 adapter command, local queue/claim files or this runtime boundary. The publication carrier incorporates that current main; the licensed result remains explicitly bound to tested source `1736ae8d...` and was not relabeled as a current-main runtime run.
- External platform submodule: repository-pinned `a5778f4abcf3b5c308c5d6854040dbc0c3082390`.
- The initial source-build gate stopped at Core build because the new worktree had not initialized the pinned submodule. That was `LOCAL_ENVIRONMENT / NO PRODUCT VERDICT`; no product defect was inferred.
- After initializing only the pinned submodule, the complete source-build gate passed: generic/manual-CI checks, all `1024/1024` aggregate preflights, Core `Release` build, deterministic Core smoke, installed-reference V25 `Release|x64` build and offline WPF/Workspace/RightPanel smoke. Both builds completed with `0 warnings / 0 errors`.
- Exact V25 adapter/Core SHA-256: `4352707C5D715996CACB82D414CC7A3C28378A413EBCC35952249FE734D8948C` / `1D42F9835898AD475E4DBAA6106FE028AADF194DA92C38B27E6FC235DCC6ED4F`.

## Licensed V25 result

The final fresh guarded matrix passed on licensed BricsCAD V25.2.10 x64 using product version `0.1.0-preview.10081` and the repository-generated disposable fixture SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`:

1. Thickness prompt: physical ESC before any numeric input.
2. Height prompt: accepted Thickness `0.25`, then physical ESC.
3. BottomOffset prompt: accepted Thickness `0.25` and Height `3.5`, then physical ESC.

All three fresh processes loaded the exact candidate V25 assembly and returned the same bounded result after the production `QS3DCONVERT2DADV` command:

- `CMDACTIVE=0` at an application Idle boundary and the production command had ended;
- exactly one original LINE and zero Solid3d remained in Model Space;
- the same source ObjectId stayed alive with unchanged endpoints and no XData;
- `DBMOD` stayed equal to its per-process pre-command baseline;
- document count stayed one;
- no `ProjectContextCoordinator` cache entry, pending project changes, project sidecar, semantic/audit state or native output was created.

Each process presented exactly one native BricsCAD drawing-save dialog during guarded shutdown. The runner selected `No` only on the exact `#32770` dialog owned by the launched PID, observed graceful exit code `0`, removed only its disposable drawing locks/script/copy and restored DemandLoad from isolated `LoadCtrls=4` to installed `LoadCtrls=2`.

Final aggregate/post-run checks passed: three of three phases, zero BricsCAD processes, zero disposable `.dwg`/`.scr`/`.dwl`/`.dwl2`/`.qsdb`/`.bak`/`.lock` residue, unchanged repository fixture hash, clean Git worktree, exact HEAD/upstream equality and zero BricsCAD/QS3D-related Application Error, Windows Error Reporting, Application Hang or .NET Runtime events in the run window.

Earlier runner-only diagnostics were not accepted as product verdicts. They exposed Windows PowerShell generic-list serialization, asynchronous drawing-lock cleanup, a native modal save dialog that blocked queued `_N`, and a final process-enumeration race. The final PASS came from a new artifact root, three new processes and three new nonces after those harness defects were corrected. No production source file was changed.

## Disposition

`P03_ADV_PHYSICAL_ESC_PROMPTS` is bounded `LOCAL_PASS` at exact tested source `1736ae8db0086041f0b1e8ce4b79839469b10061`. P01 and P02 remain prior bounded passes and were not rerun. Overall LOCAL-014 remains `PENDING_LOCAL` for prompt-time document/UCS/unit/source/project drift, ownership-scoped batch compensation and rollback, Undo/Redo, save/cold reopen and multi-DWG isolation.
