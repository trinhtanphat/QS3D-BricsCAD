# Work claim — #3830 LOCAL-008/P05 P0 geometry-prompt context drift

- Status: `ACTIVE / LOCAL_ONLY`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3830`
- Parent: `#74`
- Canonical local parent: `#72`
- Lane-Key: `issue-3830`
- Branch: `agent/codex/issue3830-local008-p05-context-drift`
- Registered: `2026-08-25T06:13:00+07:00`
- Baseline `origin/main`: `991fdcfb00f49d7ad5b66f38a07b304f719db53b`
- Priority: `LOCAL-008 / P1 / P05`

## Reserved runtime cell

Qualify the production P0 geometry-prompt freshness boundary on licensed BricsCAD V25.2.10 x64 with public disposable fixture copies. The bounded representative matrix uses:

1. `QS3DDRAWWALL` for shared fixed-path acquisition;
2. `QS3DDRAWSLAB` for shared variable/closed-path acquisition;
3. `QS3DDRAWCOLUMN` for the inline single-point acquisition path.

Each command receives one unchanged-context success control and four fresh-process drift cases: active DWG switch, Model Space to Paper Space, `INSUNITS 4 -> 6`, and a different planar UCS. A local ignored prompt-event probe applies the one intentional drift after the final required point response is accepted but before production resumes from `GetPoint` and validates the captured context.

Every drift case must refuse before project bind/bootstrap, source CAD append, semantic or audit mutation, and generated native output. Only the deliberately armed document/space/unit/UCS change may remain until probe cleanup. Every unchanged control must complete through the real production command with coherent source, semantic owner and native output.

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

A PASS qualifies only `P05_P0_GEOMETRY_PROMPT_CONTEXT_DRIFT`. It does not close #74 or overall LOCAL-008. Beam representative expansion, ADV/Door/Opening context drift, Auto Host/reference and the Ribbon/Workspace/DPI matrix remain pending.

