# Work claim — BricsCAD V26 .NET 8 compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T23:59:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Priority: Owner explicitly requested support for the latest BricsCAD V26 while preserving the existing V25 lane.

## Reserved scope

Add a real BricsCAD V26 Windows managed-plugin compatibility lane for QS3D, accounting for BricsCAD V26's .NET 8 host architecture. Preserve V25 as a supported backward-compatible build/runtime lane instead of relabeling the existing net48 assembly. Cover source-safe project/build selection, V26 host reference probing, installer/runtime host targeting, deterministic static regression guards, and the minimum canonical documentation/local-qualification updates required by this compatibility change.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` and/or a sibling V26 host project that reuses the same host source safely
- `QS3D-BricsCAD.sln`, root build props/targets if required
- BricsCAD install/support-path detection used by packaging/runtime registration
- `installer/` and source-safe build/release helper scripts where host-major selection is currently V25-only
- deterministic preflight/static tests for V26 targeting and V25 preservation
- `README.md`, `BUILD.md`, `docs/PRODUCT-BOUNDARY.md`, release/runtime docs only where compatibility claims must change
- `docs/LOCAL-AGENT-INBOX.md` / a V26 local qualification note for licensed interactive V26 proof that cannot be produced remotely

## Excluded scope

- Publishing a GitHub Release, dispatching/rerunning GitHub Actions, or claiming a customer-release runtime PASS
- Removing V25 support or changing unrelated V25 behavior
- Unrelated Direct Draw, quantity, geometry, UI, persistence, takeoff, or feature work owned by other agents
- Broad dependency/framework modernization outside what V26 compatibility strictly requires
- AutoCAD/Civil 3D runtime support

## Validation plan

- Re-fetch `main` before implementation and before the final push; preserve concurrent commits without force updates.
- Validate project XML/solution/build scripts deterministically from source and add targeted static regression coverage for V26 `.NET 8` + BricsCAD reference selection and V25 preservation.
- Run repository source-safe preflight scripts that do not dispatch CI or require proprietary BricsCAD binaries, when available through repository evidence/tooling.
- Record licensed BricsCAD V26 `NETLOAD`/DemandLoad, WPF/UI, command smoke, installer clean-machine, and V25/V26 side-by-side runtime proof as `LOCAL_ONLY`; do not manufacture runtime PASS remotely.

## Coordination

Read-only claim search found no V26 reservation and no active installer/release claim matching this compatibility lane before registration. The repository is moving quickly; any newly published overlapping claim will be respected and this scope will be narrowed before touching implementation.

## Completion condition

A coherent source/docs/static-guard batch for genuine V26 .NET 8 compatibility is pushed on top of current `main`, V25 compatibility remains explicit, required V26 local qualification is parked precisely, and this claim is marked `COMPLETED` with implementation SHA(s) and validation actually executed.
