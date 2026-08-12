# Work claim — release #31 Quick Window authoring preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-quick-window-authoring-preflight`
- Registered: `2026-08-12T10:51:00+07:00`
- Baseline main SHA: `ae0f0e8cd3e8864600aa951c2f3b54ba39ded294`
- Priority: release #31 reports `scripts/preflight-quick-window-authoring.py` failing after Direct Draw Window gained prompt-context/project freshness hardening.

## Reserved scope

Reconcile only `scripts/preflight-quick-window-authoring.py`. Preserve DirectDrawWindowCommands, Ribbon and documentation production behavior unchanged.

## Canonical evidence

- Direct Draw captures `DirectDrawProjectPreviewContext`, default project state and expected ChangeVersion before prompts.
- After prompts it rechecks active document, Model Space, UCS and drawing-unit policy before `BindProjectAfterPrompts` resolves the mutation project.
- Canonical Execute now receives `projectExistedBeforeAuthoring` so rollback can forget only a project bootstrapped by this operation.
- Execute retains semantic capture, OpeningUsage=Window, exact single-opening Auto Host, host verification, rollback/erase and explicit physical-boolean boundary.
- The gate still searches the pre-hardening Execute call shape.

## Excluded scope

No production command/ribbon/docs edits, no broad AutoHost, no automatic physical cut and no unrelated #31 work.

## Completion condition

The gate pins prompt/project freshness plus the current Execute call without weakening quick-vs-advanced semantics, is pushed to `main`, and this claim is closed with exact evidence.