# Work claim — Rebar shape positive-leg collapse

- Status: `ACTIVE`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:45:00+07:00`
- Baseline main SHA: `71dcb3b4cd2b06c8510bf60a6b1e1851a0f7f55e`
- Issue: `#76`
- Priority: remote-safe fabrication-path numeric correctness

## Verified gap

`RebarShapePathBuilder.Build` validates every parsed leg as finite and positive, but ordinary double endpoint addition can absorb a smaller positive leg at a large existing coordinate. For example, `Build("CUSTOM", 1e16, "10000000000000000;1", "0")` passes the rounded total-length comparison and returns the final two points at the same coordinates because `1e16 + 1 == 1e16`. The published path therefore contains a zero-length segment even though its source leg was explicitly positive.

No open PR or active exact claim owns positive-leg endpoint representability. Prior shape-path snapshot/bounds claims are completed and preserve different contracts.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarShapePath.cs`: inside the existing path-construction loop, fail closed when a positive leg produces no change in either endpoint coordinate.
- `tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs`: add the concrete large-coordinate collapse regression while retaining ordinary Straight/L/U/custom coverage.
- this claim document for closeout only.

## Preserved contracts and exclusions

- Preserve notation/list parsing, positive-leg rule, leg/turn/text bounds, length-match tolerance, preset/custom turn semantics, and every representable path.
- No fabrication code/standard, bend-radius/lap/anchorage policy, BBS/weight formulas, native geometry/UI, LOCAL automation, Browser fixture lane, BricsCAD/private data, release/signing, or GitHub Actions changes.
- Validate focused advanced-geometry, geometry-completion, and rebar numeric gates, Core `Release` build, and full Core smoke; report any independent blocker without expanding.

Completion means the bounded source/smoke fix is merged through normal PR, this claim is closed, and the exact merged-main SHA is returned to `/root`.
