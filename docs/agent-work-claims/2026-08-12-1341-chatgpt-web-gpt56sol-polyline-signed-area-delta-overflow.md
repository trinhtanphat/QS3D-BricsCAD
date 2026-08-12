# Work claim — Polyline SignedArea coordinate-delta overflow

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-polyline-signed-area-delta-overflow`
- Registered: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `92c2a3486b632012fcc5e368e5871616ceebdaa2`
- Priority: P0 — finite polygons with representable signed area must not fail because a translated coordinate delta overflows first.
- Task Key: `CORE-POLYLINE-SIGNED-AREA-DELTA-OVERFLOW`

## Confirmed defect

`PolylineMetrics.SignedArea(...)` triangulated around `points[0]` and formed translated coordinate deltas before the overflow-aware cross-product path. The finite triangle `(-double.MaxValue, 0)`, `(double.MaxValue, 0)`, `(0, 2.2250738585072014e-308)` has mathematical area near `4`, but the old implementation overflowed while forming `double.MaxValue - (-double.MaxValue)`.

## Completed contract

- Existing direct translated-cross behavior remains the fast path when all coordinate deltas are finite.
- When translation itself cannot be represented, the same cross product is evaluated with independent X/Y scaling before subtraction.
- Extreme anisotropic finite triangles retain representable signed area and winding sign.
- Ordinary geometry remains on the old fast path.
- Genuine area overflow and non-finite coordinates remain fail-closed.

## Evidence

- Claim commit: `954be0f4e24fc28960a6bacfeb5b2e28d75b88c1`
- Source branch commit: `a7c307cd2fd467440c211feb52a12dde83fac408`
- Smoke branch commit: `bf695703c2d14a611020c0f9290a2858f20aeee5`
- PR: `#936`
- Squash merge: `1e2b69a4f87eedcffefb64a392327ab8a73bd1a1`
- Merged source blob: `58117fa14f4c4df5e8259033448204043e7f2b82`
- Merged smoke blob: `4e30bb5a45509b6ff592d06597256d253ded9c80`
- Ancestry verified against `main@ecc277828b99c04050ce0d322eeb9c3c783a0b49`; the only later file change was unrelated QSDB XML validation.

No GitHub Actions were dispatched. No full local .NET build, executable smoke process, or BricsCAD V25/V26 runtime PASS is claimed.
