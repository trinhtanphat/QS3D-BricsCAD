# Work claim — Issue #1125 Beam rebar protected-handle coalescing

- Status: `COMPLETED`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T14:32:23+07:00`
- Completed: `2026-08-14T14:35:58+07:00`
- Baseline main SHA: `019643d26519655d9c3a7a6f97da3868ca54709c`
- Priority: `LOCAL-003 P0` licensed longitudinal Beam rebar build failure

## Sanitized trigger and diagnosis

The licensed BricsCAD V25.2.10 run on exact SHA `e8a071741d4450da4e353a2c21c95c28e4d38a81` reached `rebar_stage=longitudinal_build` and failed with `System.InvalidOperationException`, target `AddProtected`, HResult `0x80131509`, before a longitudinal count or range existed. Issue #1125 contains the sanitized evidence and clean-run disposition.

`BeamRebarSolidBuilder.BuildSelected(...)` invokes `GeneratedRebarOwnershipGuard.Build(project)` before selection processing or native bar creation. `Build` indexes all source and non-rebar generated handles through `AddProtected` before it indexes actual rebar owner slots through `Add`. The target therefore proves a protected-to-protected canonical-handle repetition, not a placement, Frustum, Stirrup, native live-set, or destructive rebar-owner conflict. `AddProtected` incorrectly treats repeated protective references as exclusive owners even though the collection only needs to prevent those handles from being erased as rebar.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs`: change only `AddProtected` so repeated canonical protected handles coalesce.
- `scripts/preflight-rebar-native-ownership.py`: add the minimum static contract that locks protected-reference coalescing while preserving the destructive ownership checks below.

## Preserved fail-closed behavior

- malformed/blank protected handles continue through the existing validation behavior;
- `Add` continues rejecting protected-to-rebar and rebar-to-rebar canonical ownership conflicts;
- `EnsureOwned` and complete live-set/native XData ownership validation remain unchanged;
- `BeamRebarSolidBuilder`, Beam placement/geometry, Stirrup/tie builders and all other native builders remain unchanged.

## Excluded scope

- No Level probe, runner, claim/inbox, private drawing/data, BricsCAD launch, GitHub Actions, V26, packaging or release work.
- No further Stirrup or placement change.

## Validation plan

- Run focused Beam rebar, Level rebar-placement, native rebar ownership and related ownership/static gates.
- Build and run the complete Core smoke suite.
- Compile BricsCAD V25 `Release|x64` against installed references without launching BricsCAD.
- Merge normally, record the exact production SHA on issue #1125, and return licensed qualification to the LOCAL-003 owner.

## Completion condition

The smallest two-file correction is merged and read back from current `main`; focused gates, Core smoke and installed-reference V25 compile pass; the claim is closed with exact SHAs; and no native runtime PASS is inferred from source/static evidence.

## Completion record

- Claim-only merge: `495749181e452f489821395e6b1516b696e0e7be` via PR #1191.
- Exact production merge: `0bec1083b9ce45b891b58aef174a61d7c367c436` via PR #1192.
- Current-main readback confirms only shared `GeneratedRebarOwnershipGuard.AddProtected` now coalesces repeated canonical protected references. `CanonicalHandle`, `Add` ownership conflicts, `EnsureOwned`, complete live-set validation and native ownership verification remain intact; Beam geometry/placement and Stirrup/tie sources are unchanged.
- Focused gates PASS: native rebar ownership, Beam rebar, Beam single-bind, Level rebar placement, generated owner-slot policy, geometry completion and generated-geometry lifecycle.
- Core Release build: PASS, 0 warnings / 0 errors.
- Full registered Core smoke: PASS (`ALL PASS`).
- Installed-reference BricsCAD V25 `Release|x64` compile: PASS, 0 warnings / 0 errors.
- Issue #1125 records exact production SHA `0bec1083b9ce45b891b58aef174a61d7c367c436` and remains `OPEN / PENDING_LOCAL` for the guarded licensed rerun. BricsCAD runtime and GitHub Actions were not run by this source lane.
