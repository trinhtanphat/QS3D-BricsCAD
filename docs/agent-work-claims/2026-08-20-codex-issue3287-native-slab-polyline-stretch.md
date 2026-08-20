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

## Exact licensed evidence

- Exact clean pushed candidate: `d389fc11a6d9599735180adb34a40a04089e5494`.
- Installed BricsCAD V25.2.10 adapter build: `0 warnings / 0 errors`; adapter
  and Core ProductVersion both ended in the exact candidate SHA.
- Sanitized result: `status=PASS`,
  `production_local004_p02_qualified=true`, native vertex STRETCH,
  pre-sync generated isolation, area/perimeter reconcile, quantity refresh,
  generated invalidation/rebuild, native bounds, scoped Health and cold reopen
  all verified; `error_code=NONE`.
- Geometry/quantity boundary: area `12 -> 13.5 m2`, perimeter
  `14 -> 12 + sqrt(10) m`, volume `1.44 -> 1.62 m3`, final bounds
  `0..5 x 0..3 x 0..0.12 m`.
- Persistence/cleanup: disposable drawing and semantic sidecar persisted before
  cold reopen; process/script/private-state cleanup and drawing restoration
  passed; zero BricsCAD processes remained; repository fixture SHA-256 stayed
  `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- This evidence is bounded to one deterministic closed Slab POLYLINE vertex
  edit. Parent #80 remains open for the excluded matrix.
