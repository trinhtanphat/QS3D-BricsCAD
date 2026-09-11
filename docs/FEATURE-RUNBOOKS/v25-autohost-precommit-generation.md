# V25 Auto Host precommit document/project generation fence

## Scope

Lane C03. This runbook covers `QS3DAUTOLINKHOSTS` in `src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs` only. It does not change MCP runtime/transport, installer/release, Quantity, or the Auto Host geometry matcher.

## Defect contract

Auto Host first resolves the selected Door/WallOpening semantics, binds the existing canonical project exactly once, and validates ProjectId/ChangeVersion plus the target set. It then performs a potentially non-trivial read-only CAD source scan and host-matching pass. A document activation or project-generation replacement during that scan must not leave the earlier project reference authorized to mutate semantics.

Immediately after the CAD scan transaction completes and before `HostLinkService`, rollback snapshot capture, metadata writes, project touch, or regeneration, the command must fail closed unless all of the following are still true:

- the exact managed `Document` that started the command remains `MdiActiveDocument`;
- `ProjectContextCoordinator.TryGetReadOnly` returns the exact same canonical `ProjectState` object that was bound for mutation;
- ProjectId and ChangeVersion still equal the preview authority;
- resolving the original selected source handles against that current project yields the same Door/WallOpening target-id set.

The final verification is read-only. It must not call `ExistingProjectMutationContext.TryGet` again, create a project, repair stale state, or silently retarget the operation.

## REMOTE_SAFE verification

`python scripts/preflight-auto-host-project-lifecycle.py`

The source guard requires the final authority fence to occur after the CAD scan and before semantic service creation, rollback snapshot capture, and `HostLinkService.LinkOpening`. It also preserves the exactly-once canonical mutation bind and the existing no-project/zero-target/rollback contracts.

Run the normal protected Shared pipeline and require fresh exact-head GREEN for source guards, deterministic smoke, package-integrity/trusted-reference admission, and BricsCAD V25 plugin compilation against the admitted locked references.

## LOCAL_ONLY licensed BricsCAD V25 qualification

Do not infer native PASS from hosted CI. When a licensed BricsCAD V25 runtime is available, qualify at least:

1. Begin `QS3DAUTOLINKHOSTS` in DWG A with valid captured opening targets, then activate DWG B while source/host evaluation is in progress. No semantic link or metadata mutation may be committed to A after authority is lost.
2. Replace/reload the QS3D project generation for the same DWG during the scan. The stale generation must not receive HostWallId/dependency/AutoHost metadata writes.
3. Exercise A→B→A before mutation. Reappearance of the same managed document is insufficient if canonical project generation or ChangeVersion changed.
4. Keep the document/project stable and verify the normal matched, ambiguous, unmatched, unchanged, regeneration, rollback, and post-commit UI paths remain behaviorally unchanged.
5. Verify user-facing failures remain redacted; no exception detail should be emitted to the palette/editor.

Classification for these host-timing cases is `LOCAL_ONLY / NO_RESULT` until actually executed in licensed BricsCAD V25.
