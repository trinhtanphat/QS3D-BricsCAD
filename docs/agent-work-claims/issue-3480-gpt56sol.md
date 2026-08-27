# Quantity Review exact native face highlight lane

Status: COMPLETED / MERGED_MAIN
Lane-Key: issue-3480
Issue: #3480
Agent: gpt56sol
Session: quantity-review-exact-face
Baseline main: `b0635b0b2d16836850a564f2de00a9e43a2465e6`
Canonical branch: `agent/gpt56sol/3480-quantity-review-exact-face`
Canonical PR: #3487 — MERGED
Final exact feature head: `977066e1becbf3719a7c6324308288499cc7c57e`
Feature merged main: `9303fb34109e0b5859d8fc2ff1122afdc3cefa83`
Exact-face source blob: `a4e60d3ded1f649bed21ba589c21d855af37ef82`
Audited current-main descendant: `4bf7de082b8a1e6366612b10c69742fd24f5c969` — exact-face source blob unchanged
LOCAL_RUNTIME: PENDING_LOCAL_AGENT
Remote disposition: DO_NOT_RETRY_REMOTE
Local queue: `docs/LOCAL-AGENT-INBOX.md` — #3480 exact native BREP face item
Supersedes: none

## Scope completed
- Make every exact formwork BREP face row/value in Quantity Insight a model action.
- Revalidate the active DWG, canonical project/element, exact geometry fingerprint and exact face identity before locating.
- Resolve stable `SOLID-xx/FACE-yy` identities against the same ordered live Solid3d/BREP enumeration used by QuantityGeometryExplanationService.
- Highlight only the resolved native BricsCAD BREP subentity through `FullSubentityPath`; do not select/highlight the whole target solid for face actions.
- Clear the prior native face highlight on another face/action, tree/detail selection, panel unload and document switches.
- Preserve existing deduction target/cause selection plus transient exact intersection/contact preview unchanged.
- Add a feature source guard and licensed-runtime handoff/runbook.

## Source/CI validation and landing
- Feature source guard and aggregate preflight passed.
- Core build and deterministic smoke passed.
- Trusted BricsCAD V25 compile-reference validation and V25 plugin compile passed.
- Final exact-head shared CI run `32557652786` / run number `12461` completed `SUCCESS`; protected `preflight` and `core` both succeeded on `977066e1becbf3719a7c6324308288499cc7c57e`.
- PR #3487 merged through the protected PR path to `main@9303fb34109e0b5859d8fc2ff1122afdc3cefa83`.
- Session audit on 2026-08-26 confirmed current `main@4bf7de082b8a1e6366612b10c69742fd24f5c969` still carries the exact implementation blob `a4e60d3ded1f649bed21ba589c21d855af37ef82`.

## Deferred local-agent acceptance
Licensed BricsCAD V25 interactive acceptance is intentionally not claimed by source/CI. It remains `PENDING_LOCAL_AGENT` and must not block remote/source work or be retried by equivalent remote agents.

When a compatible local agent is available, it must start from `docs/LOCAL-AGENT-INBOX.md`, sync/fetch Git, use a clean checkout/worktree of the exact intended source SHA, run `docs/FEATURE-RUNBOOKS/issue-3480-quantity-exact-face.md`, and record sanitized exact-SHA PASS/FAIL evidence. At this audit the intended source-containing checkout is `4bf7de082b8a1e6366612b10c69742fd24f5c969`. If `main` has advanced, the local agent may qualify a newer exact `main` SHA only after confirming `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.ExactFace.cs` still has blob `a4e60d3ded1f649bed21ba589c21d855af37ef82`; if that source blob changed, the handoff must be refreshed before runtime qualification.

Source review, hosted CI, managed-reference compile, mocks, screenshots without real host execution, or `-SkipRuntime` output must never be promoted to `LOCAL_PASS`.
