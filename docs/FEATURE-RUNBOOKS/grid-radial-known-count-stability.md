# Grid radial known-Count stability

## Scope

This runbook covers the deterministic Core boundary in `GridRadialOrderingPlanner.OrderConcentricArcs`. It does not change Grid family inference, radial center/radius tolerances, ARC geometry acceptance, ordering policy, native CAD behavior, or licensed runtime acceptance.

## Defect contract

The historical planner bounded input with `Take(MaxCurves + 1).ToList()`. That limits a pure stream but ignores supported collection Count metadata. A hostile or drifting collection could therefore report negative, conflicting, over-cap, overrun, under-yield, or transiently changed Count values while traversal proceeded to caller-controlled `Current`.

The corrected planner delegates bounded materialization to `GridSnapInputMaterializer.Materialize(curves, MaxCurves, "Grid radial ordering input")`. The shared materializer binds supported generic/read-only/non-generic Count surfaces, fails closed on invalid or conflicting metadata, revalidates the admitted Count immediately before `MoveNext` and again after a successful `MoveNext` before `Current`, enforces the hard cap, and validates terminal/final Count consistency. Pure streaming sources remain supported.

## Deterministic validation

Run:

```text
python scripts/preflight-grid-radial-known-count-stability.py
python scripts/preflight-grid-radial-ordering.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The smoke contract covers over-cap, negative Count, conflicting Count surfaces, transient growth, transient shrink, known-count under-yield, no `Current` read before transient rejection, and stable counted/streaming ascending/descending controls.

## Preserved invariants

- radial ordering remains ARC-only;
- centers must remain concentric within `centerTolerance`;
- radii and ARC sweep remain finite and valid;
- duplicate IDs and near-equal radii remain fail-closed;
- ascending ordering and explicit descending reversal remain deterministic;
- no Grid System/Intersection engine takeover or CAD/vendor dependency is introduced.

Hosted Core/source CI is authoritative for this package. No licensed BricsCAD/private-DWG `LOCAL_PASS` is required or implied.
