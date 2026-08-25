# Work claim — #3830 LOCAL-008/P05 P0 geometry-prompt context drift

- Status: `COMPLETED / LOCAL_PASS / BOUNDED_EXECUTABLE_SUBMATRIX`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3830`
- Parent: `#74`
- Canonical local parent: `#72`
- Lane-Key: `issue-3830`
- Branch: `agent/codex/issue3830-local008-p05-context-drift`
- Registered: `2026-08-25T06:13:00+07:00`
- Completed: `2026-08-25T07:58:33+07:00`
- Baseline `origin/main`: `991fdcfb00f49d7ad5b66f38a07b304f719db53b`
- Runtime source SHA: `6cab2d33d6b19f9d8ce00a884bc1bdd4e9980259`
- Refreshed `origin/main` before closeout: `33a5b2629f9541edd3d67e86fff3002511d6ffd3`
- Priority: `LOCAL-008 / P1 / P05`

## Reserved runtime cell

Qualify the production P0 geometry-prompt freshness boundary on licensed BricsCAD V25.2.10 x64 with public disposable fixture copies. The bounded representative matrix uses:

1. `QS3DDRAWWALL` for shared fixed-path acquisition;
2. `QS3DDRAWSLAB` for shared variable/closed-path acquisition;
3. `QS3DDRAWCOLUMN` for the inline single-point acquisition path.

Each command receives one unchanged-context success control and four fresh-process drift cases: active DWG switch, Model Space to Paper Space, `INSUNITS 4 -> 6`, and a different planar UCS. A local ignored prompt-event probe applies the one intentional drift after the final required point response is accepted but before production resumes from `GetPoint` and validates the captured context.

Every drift case must refuse before project bind/bootstrap, source CAD append, semantic or audit mutation, and generated native output. Only the deliberately armed document/space/unit/UCS change may remain until probe cleanup. Every unchanged control must complete through the real production command with coherent source, semantic owner and native output.

## Licensed result

The final executable matrix passed on exact pushed runtime source SHA `6cab2d33d6b19f9d8ce00a884bc1bdd4e9980259`, which contains required source commits `98a9a78f643d3434597091b4d3be7fb8b41e00a8` and `6203817538f108b274001d834e7213f12dee7f70`. The closeout carrier later merged current `origin/main@33a5b2629f9541edd3d67e86fff3002511d6ffd3` without rebasing, so the tested SHA remains an ancestor and is not relabeled as the runtime candidate.

- Host: licensed BricsCAD V25.2.10 Windows x64; adapter ProductVersion `0.1.0-preview.10081`.
- Exact binary identity: adapter SHA-256 `32B490AFFD66C35ECEDBDC2E97418B485B4DC1A08B5ECC76A708F1CC6783D98F`; Core SHA-256 `E63B29A7C95E45C82DF842DC62CDFB7FF57707A298A330CAF8BD724AD15497DA`; adapter/Core PDB SHA-256 `EF81FC57950D810F4C8272CC8ECDC786E4D775317C7FC963F76FA943C535E309` / `9B9BE15F167DB1AD5AC3CDD7BFA1E6F661ADC5197E2643F7E2F841C2DF021F0C`. Portable-PDB SourceLink resolved the exact runtime SHA.
- Pinned candidate gate: aggregate source preflight `1024/1024`; Core Release build `0 warnings / 0 errors`; deterministic Core smoke `ALL PASS`; installed-reference V25 `Release|x64` build `0 warnings / 0 errors`; offline WPF theme/Workspace/RightPanel smoke PASS. Generic licensed runtime smoke was deliberately skipped because its already-qualified cells were outside this P05 reservation.
- Final licensed aggregate: `2026-08-25T00:51:03.6239718Z` through `2026-08-25T00:58:33.4723850Z`; 12/12 fresh-process cases passed for Wall, Slab and Column, each with control plus Model/Paper Space, `INSUNITS 4 -> 6` and planar-UCS drift.
- The three controls each added exactly one canonical source, one semantic owner and one generated Solid3d with a pending project cache and no sidecar. All nine executable drift cases applied the change after the final required `PromptedForPoint` response but before production resumed from `GetPoint`, then refused before project bind/bootstrap, source/native append or semantic mutation. They left zero product output/state.
- Repeated prompt exit, variable-path Enter, verification, cleanup and shutdown were driven against the exact owned host. All 12 cases restored Model Space, world UCS, `INSUNITS=4`, project/cache state and environment; each observed exactly one native drawing-discard dialog and exited with code 0. The public disposable fixture remained SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- Post-run audit found zero BricsCAD processes, installed DemandLoad `LoadCtrls=2`, zero private drawing/script/sidecar/lock/backup residue, and zero BricsCAD/QS3D-related `Application Error`, `Windows Error Reporting` or `.NET Runtime` events during the bounded run interval. Five focused Direct Draw/evidence preflights passed.
- The ignored runtime probe SHA-256 was `63A6F3FD0E31A03411AE2923185501B0C887C8D2CC2A40D2DCC304E7EE1E0E83`. Raw markers, probe binaries, scripts, disposable drawings and machine/PID details remain ignored under `artifacts/`; no customer/private DWG was used or published.

### Active-document host boundary

At the exact final modal `GetPoint` callback, BricsCAD refused the attempted MDI activation with `BRX_Error__334_eDocumentSwitchDisabled`. This is `HOST_REFUSAL / NO PRODUCT VERDICT`, not a QS3D failure and not an active-DWG P05 PASS. The already-qualified P03 legal repeated-segment document-switch boundary was not rerun. Consequently the sanitized aggregate identifies its boundary as `P05_P0_GEOMETRY_PROMPT_CONTEXT_DRIFT_EXECUTABLE_SUBMATRIX` and explicitly records `production_local008_qualified=false`.

## Identity and safety requirements

- Start from a clean pushed exact SHA containing source commits `98a9a78f643d3434597091b4d3be7fb8b41e00a8` and `6203817538f108b274001d834e7213f12dee7f70`.
- Pass aggregate source gates, Core Release/smoke, installed-reference V25 `Release|x64`, portable-PDB exact-source identity and relevant offline WPF checks before native launch.
- Require zero pre-existing BricsCAD processes, exact launched-PID input, one fresh nonce/process/disposable primary copy per case, and a second clean disposable copy only for the active-DWG case.
- Capture before/after document count/active identity, Model/Paper Space, units, exact UCS, model-space entity counts, project/cache/semantic/audit counts and sidecar state.
- Restore DemandLoad, environment variables and intentional test context; close every owned document/process gracefully; preserve the repository fixture hash; remove scripts, drawing copies, native locks, sidecars and backups.
- Keep raw markers, prompt diagnostics, probe binaries, disposable drawings, machine paths and Handle lists ignored under `artifacts/`; commit only sanitized aggregate evidence.

## Excluded scope

No production source edit, Beam duplicate of the same fixed-path helper, ADV numeric prompts, Door/WallOpening geometry, Auto Host/reference, Ribbon/Workspace/DPI, repeated mode, Undo/Redo, save/reopen, broader multi-DWG lifecycle, V26, private/customer DWG, release operation or GitHub Actions dispatch. LOCAL-008 P01-P04 are already PASS and will not be rerun.

Any reproducible production defect is reduced to sanitized evidence and handed to a separate remote/source issue. This local lane does not patch ordinary source.

## Completion boundary

This PASS qualifies only `P05_P0_GEOMETRY_PROMPT_CONTEXT_DRIFT_EXECUTABLE_SUBMATRIX`. It does not qualify the active-DWG cell, close #74 or promote overall LOCAL-008. Beam representative expansion, ADV/Door/Opening context drift, Auto Host/reference and the Ribbon/Workspace/DPI matrix remain pending.
