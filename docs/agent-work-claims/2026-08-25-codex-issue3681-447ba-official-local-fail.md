# #3681 exact 447ba980 V25 official runner: unit-bound fixture blocker

- Status: `LOCAL_FAIL / REMOTE_SOURCE_REQUIRED / DO_NOT_RERUN_UNCHANGED`
- Lane-Key: `issue-3681-881f-local-evidence`
- Evidence PR: #3849
- Supersedes: closed PR #3764 as the current canonical evidence carrier; its historical branch and unresolved local worktree remain preserved
- Parent local queue: #72
- Licensed qualification: #3681
- Remote/source blocker: #3846
- Tested exact Git SHA: `447ba9805d777a3225827117587d932135cf0959`
- Tested exact tree: `0af95270fff29475b461c7562bf14dff171bb473`
- Required source-ready ancestor: `c64eb8c1b83761e155da670904a72e64669464b7`
- Evidence carrier baseline: `origin/main@23aff4f44615009015dcad816324390a695eedfe` (documentation descendant only; not the tested runtime binary)
- BricsCAD host: V25.2.10 Windows x64, licensed native runtime
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `775F571363C88ECA193F7DE44BAD2B9D6453EBE56FD984FED8B9F294BBF3C8B6`
- Co-located Core SHA-256: `56A9C970AC41488F52C2D9F545E9EB3567C07CE0AD338480B78375CD2A5B70EF`
- Local qualification harness SHA-256: `A3C0A64A0B1936D70C60DE104BCC386D78905C02DF8E01F30620F0B346DBB6C7`
- Bounded run: `2026-08-25T01:12:42.7978078Z` through `2026-08-25T01:27:58.9676838Z`

## Exact identity and repository-safe gates

The official committed `scripts/run-local-v25-wall-contact-3681.ps1` runner executed from a clean detached worktree at the tested SHA. The runner verified the integrated #3833/#3836 source-ready ancestor before building. Its source-build baseline passed the manual-CI policy and repository preflights, aggregate feature preflights, Core Release build, deterministic Core smoke, V25 `Release|x64` build, offline WPF checks and the focused local qualification harness build. The general runtime smoke was intentionally skipped because the #3681 runner owns the licensed native phases.

## Mandatory native controls passed

The unchanged fail-fast source-fix gate passed both required controls on the exact binary:

```text
touching-only one end:
  deduction_m2=0.16
  residual_m2=2.5088
  contact_cuts=1
  volume_cuts=0
  failed_native=0

penetration_005m:
  deduction_m2=0.16
  residual_m2=2.5088
  contact_cuts=0
  volume_cuts=1
  failed_native=0
```

This confirms the previously repaired touching and penetration paths on the official candidate. It does not promote the remaining matrix to PASS.

## Broader geometry phase failed safely

The first broader `QS3D3681GEOMETRY` process returned a sanitized failure marker:

```text
schema=qs3d-local-3681-v1
phase=geometry
status=FAIL
error_type=TargetInvocationException
error_code=Existing_semantic_quantities_have_no_trustworthy_drawing-unit_binding__Keep_the_legacy_unit_or_remeasure_source_geometry
```

The official report classified the run as `LOCAL_FAIL`. Because the geometry marker is written only after the whole command completes, no baseline, partial-contact, union, top/bottom, capture-refresh, stale-clear, read-only, second-process or save/cold-reopen cell is promoted from this run.

## Root-cause boundary

Source inspection narrows the failure to the qualification fixture, not to a reason to weaken the production unit guard. `RunCaptureRefreshAndMissingTargetClear` gets or creates a project, clears it, adds a synthetic wall and regenerates quantities without first establishing canonical drawing-unit metadata. Its later call through production `SemanticCaptureService.Capture` correctly asks `CadUnitService.TryGetPolicy` to validate a now non-empty project. `DrawingUnitResolutionPolicy.ValidateQuantityCompatibility` sees semantic elements with neither a canonical bound unit nor trustworthy legacy effective-unit evidence and refuses the operation.

Remote/source issue #3846 owns the correction. It must bind the synthetic millimeter fixture before the first semantic element or quantity is introduced, review the same boundary for the persistence/direct-measure fixtures, and add a deterministic guard. It must not bypass production capture, weaken `DrawingUnitResolutionPolicy`, alter production wall-contact geometry, round results, or relax the `1e-6` acceptance tolerance.

## Cleanup and rerun handoff

The wrapper restored DemandLoad to `LoadCtrls=2`, preserved the installed loader path and SHA-256 `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`, and left zero BricsCAD processes. Raw scripts, DWGs, Handles, sidecars, machine paths and host logs remain Git-ignored under `artifacts/`; none is committed.

The local lane made no production or committed harness/tolerance patch. Keep #3681 open and do not rerun `447ba980...`. After #3846 publishes one exact source-ready descendant, run the unchanged official command again and require both mandatory controls, the complete broader matrix, second fresh process/DWG equality, save/cold reopen and the BLT `2.6688 - 0.3200 = 2.3488 m2` control before claiming `LOCAL_PASS`.
