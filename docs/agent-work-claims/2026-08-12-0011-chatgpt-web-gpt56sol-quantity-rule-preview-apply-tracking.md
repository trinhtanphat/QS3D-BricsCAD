# Work claim — Quantity Rule preview apply mutation tracking

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-preview-apply-tracking`
- Registered: `2026-08-12T00:11:00+07:00`
- Completed: `2026-08-12T00:13:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Reservation commit: `6841a673045d325370d309614150a6257e0e49b5`
- Priority: P0 — make successful reviewed quantity-rule applies participate in project persistence/version state and keep no-change element applies side-effect free.

## Defects fixed

`QuantityRulePreviewService.ApplyElement(...)`, `ApplyProject(...)` and `ApplyProjectWithHealthGuard(...)` ultimately call `QuantityRuleEngine.ApplyMatching(...)`, which intentionally remains revision-agnostic because regeneration batches own their own project revision boundary. The reviewed preview-apply service did not supply that missing boundary, so persisted quantity/provenance output could change while `ProjectState.ChangeVersion` remained unchanged.

`ApplyElement(...)` also invoked the rule engine for a fresh preview with zero semantic changes. Since rule application writes quantities, this could update element persistence timestamps despite the reviewed operation being a semantic no-op.

Changed element apply now executes rule application plus one project `Touch()` inside `ProjectSemanticMutationExecutor`. Project-level apply touches once after the complete reviewed changed-element batch; the existing project snapshot/rollback boundary covers that revision update. Fresh no-change element apply returns zero before entering the mutation executor.

## Published commits

- `68edffe2afd5bd363f85cc6eff659e104b1994aa` — `fix(quantity): track reviewed rule apply mutations`.
- `47b2eb1715d9a25718e33e75fbf08207d465f616` — `test(quantity): guard reviewed rule apply revision tracking`.
- `209365d50817fa5542724bcfaab85c44478cd3c4` — `test(quantity): pin reviewed rule apply revision tracking`.

## Preserved contract

- `QuantityRuleEngine.ApplyMatching(...)` remains revision-agnostic for regeneration and other batch owners.
- Stale preview, exact project-owned element, semantic equivalence and health regression guards are unchanged.
- Changed element apply is rollback-safe and owns one project revision advancement.
- Changed project/health-guarded project apply own one revision advancement for the reviewed batch rather than one per element/rule.
- Fresh no-change element apply is a true version/timestamp no-op.

## Validation notes

Current `main` source, focused smoke and dedicated static preflight were re-fetched after publication and contain the intended mutation-owner/no-op contracts. Concurrent main commits were preserved through current-blob Contents API writes; no force-push was used. The smoke/preflight were not executed in a repository checkout in this connector-only lane, so no executable Core PASS is claimed. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No rule formula/category policy changes, no quantity calculation redesign, no UI/native changes and no release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: reviewed quantity-rule apply paths now own explicit project revision tracking, fresh no-change element apply is side-effect free, focused regression/static coverage is on `main`, and this reservation is released.
