# Work claim — release #31 Quick Door/Opening authoring preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-quick-opening-preflight`
- Registered: `2026-08-12T10:54:00+07:00`
- Baseline main SHA: `b9972422699fd76c6f5ca912d72a0243925f70d2`
- Priority: release #31 reports `scripts/preflight-quick-opening-authoring.py` failing because the gate still requires broad AutoHost command re-entry after Direct Draw was narrowed to exact single-opening host linking.

## Reserved scope

Reconcile only `scripts/preflight-quick-opening-authoring.py`. Preserve DirectDrawOpeningCommands, Ribbon, Hub and docs unchanged.

## Canonical evidence

- Quick/advanced Door and WallOpening wrappers/defaults/prompts remain present.
- Execute resolves its exact mutation project from `DirectDrawProjectPreviewContext` and tracks whether authoring bootstrapped project state for rollback cleanup.
- Direct Draw now calls `AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)` and explicitly avoids broad pick-set `AutoLinkHosts()` re-entry.
- Host identity is verified after exact linking; physical boolean cutting remains explicit and rollback erases CAD source/restores project state.

## Excluded scope

No production edits, no broad AutoHost re-entry, no automatic physical cut and no unrelated #31 work.

## Completion condition

The gate requires exact single-opening Auto Host and rejects broad re-entry while preserving all quick/advanced/rollback/UI/doc contracts, is pushed to `main`, and this claim is closed with exact evidence.