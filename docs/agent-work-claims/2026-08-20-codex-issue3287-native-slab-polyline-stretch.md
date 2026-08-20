# Work claim — issue #3287 native Slab POLYLINE edit qualification

- Status: `ACTIVE`
- Lane-Key: `issue-3287`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue3287-native-slab-polyline-stretch`
- Parent product gap: `#80`
- Baseline main SHA: `972b3d67528e0008c6941596ea2156ca210a6479`

## Confirmed qualification gap

Issue #3281 P01 proves native top-level MOVE, ROTATE and crossing-window
STRETCH for one authoritative LINE source. The remaining source-edit matrix has
no exact licensed proof that a real closed POLYLINE vertex edit refreshes area,
perimeter and Slab quantities, invalidates the overlapping old generated solid,
rebuilds distinct native output, and survives save plus cold reopen.

## Reserved scope

- one additive V25 automation-only closed-POLYLINE runtime probe under
  `src/QS3D.BricsCAD.V25/`
- one additive guarded exact-SHA PowerShell runner under `scripts/`
- one additive focused auto-discovered preflight under `scripts/`
- `docs/LOCAL-AGENT-INBOX.md` and `docs/SOURCE-EDIT-WORKFLOW.md` only after an
  exact licensed result is available
- this claim file

## Intended contract

On a repository-sample disposable drawing:

1. Use production `QS3DDRAWSLAB` to author and build one closed four-vertex
   4 m by 3 m Slab POLYLINE with 0.12 m thickness.
2. Clear retained PICKFIRST and issue real top-level `STRETCH` with a crossing
   window around only the `(4 m, 3 m)` vertex, remove the last-created
   overlapping generated solid from the native selection, and displace the
   source vertex `+1 m` in WCS X.
3. Before reconcile, require final source vertices
   `(0,0),(4,0),(5,3),(0,3)` while semantic metrics and the overlapping old
   1.44 m3 generated solid remain unchanged.
4. Run production `QS3DSYNCSOURCE`; require the same closed source identity,
   AreaM2 13.5, LengthM and PerimeterM `12 + sqrt(10)`, gross/net volume
   1.62 m3, formwork `13.5 + 0.12 * (12 + sqrt(10))`, and erased stale output.
5. Run production `QS3DBUILD3D`; require one distinct live owned replacement
   solid with bounds X 0..5 m, Y 0..3 m, Z 0..0.12 m.
6. Save, close and cold-reopen; prove exact source geometry, semantic metrics,
   quantities, generated ownership and scoped health remain coherent.

## Exclusions

- No production Source Reconcile, Undo coordinator, Direct Draw, builder,
  geometry, persistence or UI changes unless exact runtime evidence proves a
  separately bounded defect.
- No changes to the completed #3281 P01 files or its LINE qualification.
- No synthetic database edit in place of native top-level `STRETCH`.
- No closed/open topology transition, polygon holes/multi-region/rebar under
  #83, grip/jig/manual ESC or repeated authoring under #74, Door/Opening,
  Curtain/dependent-category matrix, customer/private DWG, release/signing or
  workflow/Actions edits.
- Parent issue #80 remains open after this bounded P02.

## Validation plan

- Publish this claim on the canonical issue branch before automation edits.
- Run the focused gate, all Source Reconcile gates, PowerShell AST parsing,
  generic/aggregate preflight, Core Release build/smoke and installed-reference
  V25 Release|x64 build.
- Push one exact clean candidate, verify ProductVersion, then run installed
  BricsCAD V25 only on disposable repository-sample copies with sanitized
  bounded evidence and zero residual test processes/private state.
- Update local workflow documentation only from an exact licensed PASS and
  stop before main merge unless the owner explicitly authorizes this named PR.
