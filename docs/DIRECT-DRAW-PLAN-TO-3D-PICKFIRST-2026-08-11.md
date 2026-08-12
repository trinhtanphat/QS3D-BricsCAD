# QS3D Plan-to-3D — PICKFIRST

Updated: 2026-08-11 (UTC+7)

## Goal

Remove the redundant re-selection step when converting an existing 2D plan into QS3D walls.

The three existing commands already implement implied-selection-first acquisition in `AcquireSelection(...)`; this batch makes that intent explicit at the BricsCAD command boundary with `CommandFlags.UsePickSet`:

- `QS3DCONVERT2D`
- `QS3DPLAN2WALLS`
- `QS3DCONVERT2DADV`

## Quick workflow

```text
preselect one or more LINE/open POLYLINE
-> run QS3DCONVERT2D or QS3DPLAN2WALLS
-> consume PICKFIRST selection
-> validate source geometry/freshness
-> semantic capture
-> scoped regeneration per created wall
-> native wall build
```

If there is no usable implied selection, the existing explicit `Editor.GetSelection()` fallback remains unchanged.

`QS3DCONVERT2DADV` uses the same PICKFIRST acquisition but still asks for its intended shared Thickness / Height / BottomOffset overrides.

## Preserved safety boundaries

This interaction-only change does not alter the conversion pipeline:

- source types remain limited to LINE and open POLYLINE;
- closed POLYLINE and unsupported entity types still fail closed;
- Model Space and UCS guards remain active;
- source acquisition occurs before project preview/mutation;
- source geometry is preflighted before prompts, re-read after prompts, and re-read again after project resolution;
- deterministic geometry fingerprints must still match before semantic mutation;
- existing semantic/generated ownership freshness remains required;
- `ProjectStateSnapshot` remains after final source freshness checks;
- each created wall uses `RegenerateDirtySubset` rather than broad project regeneration;
- generated CAD cleanup remains ownership-verified before semantic rollback;
- original 2D source CAD remains the semantic source and is not converted into operation-owned geometry.

No geometry heuristic or source-type widening is introduced by PICKFIRST.

## Local BricsCAD V25 qualification

Remote/source completion is not runtime qualification. Exact editor behavior remains LOCAL_ONLY under the existing local authoring gates.

Local V25 verification should cover:

1. preselect one LINE, run `QS3DCONVERT2D`, verify the command does not ask to select it again;
2. preselect multiple LINE/open POLYLINE objects and verify the complete implied selection is consumed once;
3. run with no PICKFIRST selection and verify the normal explicit selection prompt still works;
4. verify `QS3DPLAN2WALLS` follows the same quick PICKFIRST behavior;
5. verify `QS3DCONVERT2DADV` consumes PICKFIRST first and still presents the intended shared style prompts;
6. cancel explicit selection or any ADV prompt and verify no semantic/native/project residue is created;
7. include invalid/closed/stale/mixed unsupported objects and confirm the existing preflight rejects them without guessing;
8. modify selected source geometry during the prompt/project-resolution windows and verify the existing geometry-fingerprint freshness guards still reject stale conversion;
9. switch active DWG during the interaction and verify fail-closed behavior;
10. save/reopen and verify BQ/XLSX/Locate/Health continue to resolve the normal semantic walls.

## Static regression guard

`scripts/preflight-plan-to-3d-pickfirst.py` requires all three commands to retain `UsePickSet`, requires implied selection to precede explicit selection fallback, and pins the post-resolve source-freshness ordering plus scoped regeneration.

GitHub Actions remain manual-only under `CI_POLICY.md`; this batch does not authorize workflow dispatch.
