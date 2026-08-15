# PR #1580 validation reconciliation claim

- Status: `ACTIVE`
- Agent: Codex `/root`
- Registered: `2026-08-15T11:22+07:00`
- Exact baseline `main`: `5a13195e2b49a64c5b2d728bf4af668d1b9bff88`
- Integration source: PR `#1580` merge `44e3c9aacc4f8d00b8e9db486889d5d42d017927`
- Priority: restore exact-main Core smoke and aggregate source-gate health after concurrent integration

## Confirmed blockers

PR #1580 integrated the ProjectState persisted-XML hardening and the residual structural recovery while their narrower source PRs were still under coordinator review.

1. `FloorGeneratedIdentityUnicodeSmoke.MalformedNamesAreRejected` still constructs a malformed lone-surrogate `FloorDefinition` directly. The new public persisted-text boundary correctly rejects that value before the smoke can exercise the intended `FloorGeneratedIdentityPlanner` defense-in-depth boundary, causing module initialization to fail before `Main`.
2. `preflight-direct-draw.py` still requires the former private helper name `BuildClosedPolylinePrism`, although the integrated structural implementation generalized it to `BuildClosedProfilePrism` for POLYLINE and CIRCLE profiles.
3. `preflight-level-native-host-placement.py` still requires the former `polyline.Elevation` subtraction token, although the generalized helper correctly resolves the same Level offset from its explicit `sourceElevation` argument.
4. After the Floor fixture is reconciled, `ProjectInterchangeJsonSurrogateIntegritySmoke` likewise constructs malformed `ProjectState.ProjectId` text through the newly validating public constructor, so its intended exporter defense-in-depth boundary is unreachable. `QsdbProjectXmlTextPreflightSmoke` has the analogous stale direct `DrawingPath` setter setup before its filesystem-preflight assertion.
5. The integrated curved-structural runtime probe reads nullable snapshot measurement properties again after `HasValue`, which nullable flow analysis correctly rejects as `CS8629` in the installed-reference V25 warnings-as-errors build.

Exact baseline evidence: Core/Smoke Release build succeeds with 0 warnings/errors; full smoke fails at the Floor fixture above; the aggregate reports exactly the two structural gate failures above.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/FloorGeneratedIdentityUnicodeSmoke.cs`
- `scripts/preflight-direct-draw.py`
- `scripts/preflight-level-native-host-placement.py`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeJsonSurrogateIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/QsdbProjectXmlTextPreflightSmoke.cs`
- `src/QS3D.BricsCAD.V25/CurvedStructuralRuntimeProbeCommands.cs`
- this claim record

The Floor and Interchange/QSDB smokes will assert the supported public mutation rejection, construct valid state, inject only the exact malformed legacy/raw backing field through test-local reflection, assert the injection, and retain the downstream planner/exporter/persistence rejection. The two gates will change only their directly stale generalized helper/source-elevation tokens. The runtime probe will capture nullable measurement properties in stable locals before `HasValue`/`Value`, without changing marker, geometry, runner, or privacy behavior.

## Exclusions

No production source, runtime probe/runner, BricsCAD execution, native geometry change, XML policy weakening, release/workflow/GitHub Actions operation, private data, or unrelated fixture/gate cleanup. PR #1580 implementation remains intact; licensed curved-structural and updater behavior remain `PENDING_LOCAL`.

## Validation

Run the Floor/Direct Draw/Level/Sheet-residual focused gates, Core and Smoke Release builds, full Core smoke, installed-reference V25 Release|x64 build, generic preflight, aggregate preflight, and diff-check. Do not operate the already-running external BricsCAD process.
