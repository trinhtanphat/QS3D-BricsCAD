# Work claim — issue #3289 native Beam dependent-output MOVE qualification

- Status: `ACTIVE`
- Lane-Key: `issue-3289`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue3289-native-beam-dependent-move`
- Parent product gap: `#80`
- Baseline main SHA: `972b3d67528e0008c6941596ea2156ca210a6479`

## Confirmed qualification gap

LOCAL-004 P01 proves native LINE MOVE/ROTATE/STRETCH for a single generated
host, while P02 is separately awaiting integration with closed Slab POLYLINE
vertex evidence. Neither proves that one real native source edit invalidates a
complete dependent-output set spanning the host solid, Beam longitudinal bars
and Beam stirrups, or that all three families can be rebuilt and cold-reopened
without stale ownership.

## Reserved scope

- one additive V25 automation-only Beam dependent-output runtime probe under
  `src/QS3D.BricsCAD.V25/`
- one additive guarded exact-SHA PowerShell runner under `scripts/`
- one additive focused auto-discovered preflight under `scripts/`
- `docs/LOCAL-AGENT-INBOX.md` and `docs/SOURCE-EDIT-WORKFLOW.md` only after an
  exact licensed result is available
- this claim file

## Intended contract

On a repository-sample disposable drawing:

1. Use production `QS3DDRAWBEAM` to author and build one horizontal 5 m Beam
   LINE with the projectless 0.3 m by 0.5 m defaults.
2. Use an automation-only preparation command to set bounded fixture notation
   `4D16` and `D8@1000`, then invoke production `QS3DBEAMREBAR3D` and
   `QS3DBEAMSTIRRUP3D`. Require one host solid, four longitudinal bars and the
   deterministic stirrup set with exact native ownership.
3. Issue real top-level native `MOVE` on only the authoritative source LINE by
   +1 m WCS Y. Before reconcile, require the source to have moved while every
   old host/rebar/stirrup handle, volume and bounds remain unchanged.
4. Run production `QS3DSYNCSOURCE`; require the same source identity and length,
   complete erasure of all old generated handles, and removal of all three
   generated metadata families without partial invalidation.
5. Run production `QS3DBUILD3D`, `QS3DBEAMREBAR3D` and
   `QS3DBEAMSTIRRUP3D`; require distinct complete live owned replacements at
   the moved location with the same bounded counts.
6. Save, close and cold-reopen; prove exact source placement, semantic length,
   generated ownership, native geometry and scoped Core/rebar/stirrup health.

## Exclusions

- No production Source Reconcile, Undo coordinator, Direct Draw, structural
  builder, Beam rebar/stirrup builder, ownership, persistence or UI changes
  unless exact runtime evidence proves a separately bounded defect.
- No synthetic database edit in place of native top-level `MOVE`; only bounded
  test-fixture notation provisioning may mutate semantics directly.
- No changes to the completed P01 files or the open P02 branch/PR.
- No Beam STRETCH/count redistribution, grip/jig/manual ESC, repeated authoring
  under #74, Slab/Foundation mesh under #83, customer/private DWG,
  release/signing or workflow/Actions edits.
- Parent issue #80 remains open after this bounded P03.

## Validation plan

- Publish this claim on the canonical issue branch before automation edits.
- Run the focused gate, all Source Reconcile/rebar ownership guards,
  PowerShell AST parsing, generic/aggregate preflight, Core Release build/smoke
  and installed-reference V25 Release|x64 build.
- Push one exact clean candidate, verify ProductVersion, then run installed
  BricsCAD V25 only on disposable repository-sample copies with sanitized
  bounded evidence and zero residual test processes/private state.
- Update local workflow documentation only from an exact licensed PASS and
  stop before main merge unless the owner explicitly authorizes this named PR.
