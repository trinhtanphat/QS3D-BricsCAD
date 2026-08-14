# Agent work claim — slabOpen negative-Z auto subtract

- Status: ACTIVE
- Agent: `chatgpt-web-gpt56sol`
- Claimed at: `2026-08-14T19:14:17+07:00`
- Baseline `main`: `9d19f0fcfbedfa08cb0373c6ac93e34c2c12bce0`
- Implementation branch: `agent/chatgpt-web-gpt56sol/slab-open`
- Integration batch: `TBD`
- Priority: user-requested correctness/feature gap

## Reserved scope

Implement the exact `slabOpen` Family/contract path so a slab opening uses a cutter that traverses the slab in negative Z and the resulting `Solid3d` is automatically Boolean-subtracted from its semantic Slab host.

Expected source surfaces:

- `src/QS3D.Core/Domain/SlabOpeningContract.cs` (new)
- `src/QS3D.Core/Geometry/SlabOpeningCutPlanner.cs` (new)
- `src/QS3D.BricsCAD.V25/Cad/SlabOpeningBooleanService.cs` (new)
- `src/QS3D.BricsCAD.V25/DirectDrawSlabOpeningCommands.cs` (new)
- `src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs`
- `scripts/preflight-slab-open-negative-z-boolean.py` (new)

## Exclusions / collision boundaries

- Do not modify Slab Mesh/rebar planners/builders or their health diagnostics.
- Do not modify Quantity Insight / quantity-explanation surfaces.
- Do not change the existing wall `OpeningBooleanService` semantics except through the narrow Active Family routing boundary if required.
- Do not modify unrelated Family Manager lifecycle behavior.
- Native BricsCAD `Solid3d.BooleanOperation` runtime evidence remains `LOCAL_ONLY` unless exercised on a licensed BricsCAD runtime.

## Validation plan

- Compile-time-safe Core planner/contract with finite/positive geometry guards.
- Static preflight pins exact `slabOpen` routing, negative-Z extrusion, `BoolSubtract`, semantic `HostSlabId`, and automatic subtraction from the Direct Draw path.
- Re-read changed files and branch commit after write.
- Integration follows the repository integration/freeze protocol; final source landing to `main` relies on the standing automatic post-integration V25 workflow rather than manual CI dispatch.
- Licensed native BricsCAD acceptance remains explicitly `LOCAL_ONLY` until runtime evidence exists.

## Coordination

Targeted recent-history checks found no current `slabOpen` claim/branch. Historical Slab Mesh claims and the historical opening-boolean claim were already completed; this claim intentionally reserves a separate slab-opening lane and avoids their surfaces.

## Completion condition

`slabOpen` is routed deterministically from the active Family, creates/records a semantic slab opening against exactly one Slab host, constructs a cutter spanning the slab with a negative-Z extrusion, automatically applies `BooleanOperationType.BoolSubtract`, passes the remote-safe source/preflight gates, is integrated under the current batch/freeze protocol, and the integrated commit is verified reachable from `main`. Native BricsCAD behavior is not called PASS without licensed runtime evidence.
