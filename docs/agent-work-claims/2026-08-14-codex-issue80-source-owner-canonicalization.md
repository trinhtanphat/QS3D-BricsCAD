# Work claim — issue #80 Source Reconcile canonical source ownership

- Status: `COMPLETED`
- Agent: `codex-issue80-source-owner-canonicalization-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T14:45:00+07:00`
- Baseline main SHA: `28aaf5289f87183f3f38f10f245cb5f45624674d`
- Priority: bounded remote-safe ownership correction under product gap `#80`

## Confirmed defect

`SourceReconcileService.ResolveTargets` builds a private raw `StringComparer.OrdinalIgnoreCase` index over `ProjectElement.SourceHandles`. The canonical Core ownership contract instead uses `SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner`, which normalizes logical CAD handle identity and rejects malformed stored handles, duplicate logical identities within one element, and cross-element numeric aliases such as `A` / `00A` / `0xA`.

The private index can therefore select one semantic owner when the same logical source is ambiguously claimed under another numeric spelling, and it does not apply the same malformed/duplicate stored-source validation as capture and general semantic selection.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs`
- `scripts/preflight-source-reconcile.py`
- `scripts/preflight-source-reconcile-single-bind.py`
- this claim file

## Intended contract

- Preserve the generated-output refusal as the first selected-handle classification.
- Resolve authoritative source ownership through `SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner` in both read-only preview and canonical revalidation phases.
- Preserve unknown-source, exactly-one-authoritative-source, duplicate-selected-element, project/version/target freshness, transaction/rollback, invalidation, regeneration and explicit rebuild boundaries.
- Remove only the competing raw SourceHandles index; do not add a second geometry or ownership model.

## Excluded scope and coordination

- The active issue `#1005` claim remains authoritative for `SourceReconcileUndoCoordinator.cs`, native marker/history handling and the exact licensed LOCAL-004 rerun. This claim does not edit those surfaces.
- The active LOCAL-004 claim owns its runner/probe/gate/evidence and explicitly excludes production `SourceReconcileService` repair.
- No LOCAL-003, Curtain P10/P11, native geometry, generated invalidation, dependency/regeneration behavior, BricsCAD execution, private data, GitHub Actions, release, installer, signing or V26 work.

## Validation plan

- Update the focused static gates to require the canonical Core resolver and forbid resurrection of `BuildSourceOwnerIndex` or raw source-owner lookup.
- Reuse and execute existing Core smoke coverage for numeric handle aliases, malformed stored SourceHandles, duplicate logical source identities and cross-owner ambiguity.
- Run all focused Source Reconcile gates, Core smoke, generic preflight and installed-reference V25 `Release|x64` build without launching BricsCAD.
- Re-fetch current claims/PRs and rebase current `main` before source PR merge.

## Completion condition

The bounded source/gate correction is merged to current `main`, remote-safe validation is recorded, issue `#80` remains open for its larger interactive/native edit UX and local qualification scope, and this claim is marked `COMPLETED` without implying BricsCAD runtime evidence.

## Completion record

- Claim PR `#1196` merged as `0c51a765b74638d05ea173a62180d13c82432287`.
- Source commit `cefd2612c8b1e3c39e61bf7060ec3ed081e5848e` merged through PR `#1197` as `c1eda700301b4dc8671ba2d4c4a418f3e5ab303c`.
- `ResolveTargets` retains generated-output classification first, then uses `SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner` for canonical source ownership. The private raw SourceHandles index was removed; unknown-source, exactly-one-source and duplicate-selected-element refusals remain.
- Six focused Source Reconcile gates PASS; Core smoke `ALL PASS`; generic preflight PASS; installed-reference V25 `Release|x64` build PASS with `0 warnings / 0 errors`.
- No BricsCAD runtime, private data, GitHub Actions, LOCAL-004 runner/probe/docs, issue `#1005` coordinator, geometry or release work was changed/run. Issue `#80` remains open for the broader interactive/native modify workflow and licensed qualification.
