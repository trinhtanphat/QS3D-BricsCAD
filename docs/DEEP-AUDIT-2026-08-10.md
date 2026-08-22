# Deep release audit — 2026-08-10

This audit reviews the current QS3D clean-room BricsCAD V25 implementation as a release candidate. It distinguishes source/Core correctness from claims that require a real licensed BricsCAD V25 runtime.

## Review scope

The audit covered repository/workflow policy, command and ribbon wiring, WPF workspace integration, semantic project persistence, family/type edits, generated-geometry ownership and stale lifecycle, Wall/Room/Opening/Curtain/Foundation geometry, longitudinal/shape/tie/stirrup/mesh rebar, quantity/BBS release guards, model health, licensing/update/signing scaffolding, deterministic smoke registration and V25 adapter release boundaries.

The central architecture remains intentional: the interactive 3D viewport is BricsCAD modelspace itself. QS3D palettes, ribbon, selection sync and property workflows operate around the native editor rather than replacing it with a fake WPF 3D preview.

## Defects and gaps closed in this audit

1. **Beam longitudinal regression smoke was present but not executable.** `BeamRebarRegressionSmoke` had no central invocation or module registration. `BeamRebarSmokeRegistration` now makes the regression execute, and `preflight-smoke-registration.py` checks every `*Smoke.cs` exposing `Run()` so future test files cannot silently remain dormant.
2. **Beam stirrup fabrication path lacked explicit bend/hook geometry.** `BeamStirrupLayoutPlanner` now supports explicit bend radius, hook length and hook-tail angle without guessing an engineering standard. Zero engineering values retain the legacy five-point rectangular loop.
3. **Open hook solids could be extended beyond designed endpoints by the segment-union overlap strategy.** The V25 builder now applies overlap at internal joints only for open paths; closed loops retain joint overlap around the loop.
4. **Advanced stirrup quantity/fabrication metadata was incomplete.** Generated snapshots now persist exact centerline length, total centerline length, tessellated path length, bend radius, hook length, hook-tail angle and an explicit generated mode.
5. **Advanced stirrup health was not strong enough to protect those fields.** Health now validates finite/non-negative values, exact total-length consistency, supported/missing mode and mode-versus-bend/hook consistency while preserving compatibility for legacy snapshots without advanced metadata.
6. **Invalidation could leave derived stirrup metadata behind after generated solids were removed.** Advanced length/bend/hook fields are included in generated-dependent metadata cleanup.
7. **Old Beam rebar documentation was stranded in an unmergeable PR.** Current-source documentation has been recovered and updated without reintroducing its redundant manual workflow.
8. **Static lifecycle coverage was fragmented.** `preflight-beam-stirrup-bends.py` now locks planner, adapter, ownership/stale behavior, advanced health, invalidation and deterministic smoke coverage together.

## Areas reviewed with no new source patch required

- Beam Stirrup command aliases are already consistent: workspace/compatibility build and health aliases are all registered.
- Existing Beam longitudinal V25 source already rejects non-planar sources outside the current `0.005 m` tolerance instead of silently flattening them.
- Foundation mesh from the old open PR has already been superseded by the newer mainline implementation with ownership/stale/health integration.
- Advanced shape rebar/model-review work from the old open PR has already been superseded by broader mainline geometry/review/ownership work.
- Repository scan did not expose an intentional `TODO`/placeholder/`NotImplementedException` product stub that should be converted into fake success behavior.

## Validation policy

The repository's normal CI/V25 workflows remain manual-only. The source tree contains generic and feature preflight guards, deterministic Core smoke tests and V25 compile/runtime harnesses. This audit does not turn a manual workflow into a push-triggered workflow merely to obtain a green badge.

A historical exact snapshot has passed the generic/aggregate/Core smoke gate, but that result must not be presented as proof for a later moving `main`. Current-head release proof must be tied to the exact commit that is tested.

## Runtime gates that remain intentionally unresolved by source review

The following are not source-code TODOs and must not be faked:

- compile the V25 adapter against real `BrxMgd.dll`/`TD_Mgd.dll` from the target BricsCAD V25 installation;
- `NETLOAD` the exact release DLL into a licensed interactive BricsCAD V25 session;
- exercise selection, palettes/ribbon, DPI/layout and native modelspace interaction on representative DWGs;
- generate and inspect real Boolean `Solid3d` results for openings, curtain frames and advanced rebar paths;
- capture runtime marker/log/screenshot evidence from that same exact commit;
- perform private-DWG acceptance and production signing/licensing/update-server verification where credentials/infrastructure are required.

## Release rule

Do not describe the project as "V25 runtime passed" from source inspection, Core smoke results or static preflight alone. Promote a release only when the exact `main` SHA has both source/Core evidence and the required BricsCAD V25 runtime evidence.