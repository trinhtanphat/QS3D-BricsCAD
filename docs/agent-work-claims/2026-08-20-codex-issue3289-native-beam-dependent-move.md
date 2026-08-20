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
- after exact licensed containment evidence only,
  `src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs` plus the existing
  `scripts/preflight-beam-rebar.py` may correct and lock the isolated
  longitudinal placement defect
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
  host builder, Beam stirrup builder, ownership, persistence or UI changes.
- The Beam longitudinal builder exception is limited to the exact licensed
  centered-frustum midpoint correction recorded below; no layout/count/cover,
  ownership, transaction or semantic metadata redesign is authorized.
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

## Licensed diagnostic evidence and bounded source continuation

- Exact pushed diagnostic candidate:
  `87ea546b484fb690fde300be26eb12f662c4d937`.
- Installed BricsCAD V25.2.10 returned sanitized
  `failure_phase=dependent_baseline` /
  `failure_code=OUTPUT_HOST_CONTAINMENT_REJECTED` before MOVE or reconcile.
  The probe checks the four longitudinal bars before the stirrup set, so the
  result isolates the `GeneratedRebarHandles` geometry family.
- Source correlation is exact: `BeamRebarSolidBuilder` creates each full-length
  bar with centered `Solid3d.CreateFrustum`, rotates it onto the Beam axis, then
  translates its center to the covered source start rather than
  `covered start + axis * usableLength/2`. This places half of each bar outside
  the host envelope while counts and ownership still appear healthy.
- Process/script/private-state cleanup and disposable drawing restoration all
  passed; zero BricsCAD processes remained and the repository fixture hash was
  unchanged. This failure does not authorize any other production surface.

## Licensed PASS handoff

- The bounded correction computes a finite positive half usable bar length and
  places each centered longitudinal frustum at
  `covered start + beam axis * usableLength/2`; transverse layout remains
  relative to that longitudinal midpoint. Counts, cover, notation, ownership,
  native transaction and semantic metadata behavior are unchanged.
- Exact pushed licensed candidate:
  `a49342145020b154479eaa780ef3a1af597a2b3f`.
- Installed BricsCAD V25.2.10 x64 loaded matching adapter/Core ProductVersion
  `0.1.0-preview.10081+a49342145020b154479eaa780ef3a1af597a2b3f`.
  The installed-reference V25 `Release|x64` build passed with zero warnings and
  zero errors; plugin SHA-256 was
  `523A0430FF7F0858CC9062586226CF96E1C50312C1351ECA41AB5789F1BC1C6E`.
- The exact runtime returned `production_local004_p03_qualified=true` and
  `error_code=NONE`. The baseline contained one 5 m Beam host, four `4D16`
  longitudinal bars and six `D8@1000` stirrups. Native top-level `MOVE`
  translated only the source LINE by `+1 m` WCS Y while all old generated
  geometry remained unchanged before reconcile.
- Production `QS3DSYNCSOURCE` erased every old host/rebar/stirrup handle and
  removed all three metadata families. Production `QS3DBUILD3D`,
  `QS3DBEAMREBAR3D` and `QS3DBEAMSTIRRUP3D` built distinct complete translated
  replacements. Containment, volume, bounds, ownership, scoped Core/runtime
  Health, save, sidecar persistence and fresh-process cold reopen all passed.
- Process/script/private-state cleanup and drawing restoration passed, zero
  BricsCAD processes remained, and the repository fixture retained SHA-256
  `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- This claim stays `ACTIVE` until the implementation PR is integrated. The
  bounded P03 is ready for review, while parent issue `#80` and the broader
  LOCAL-004 interactive/dependent matrix remain open.
