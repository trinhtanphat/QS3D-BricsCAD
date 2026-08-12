# Work claim — release #30 semantic untrack single-bind preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-untrack-single-bind-preflight`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `578505f2d869d4996b535b8a0f9ff0c07f5657d8`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports one semantic-untrack single-bind token failure after preview target resolution was wrapped in an explicit try/catch and its declaration split from assignment.

## Reserved scope

Reconcile only `scripts/preflight-untrack-single-bind.py` with the current exception-isolated preview-target resolution shape. Preserve ViewportCommands/SemanticUntrackService production behavior unchanged.

## Canonical evidence

- `UntrackSelectedElements` still reads implied selection and handles before any mutation bind.
- It resolves an existing project read-only and captures ProjectId/ChangeVersion.
- `previewTargetIds` is now declared as `List<string> previewTargetIds;`, then assigned inside a try block via `previewTargetIds = ResolveUntrackTargetIds(previewProject, handles, predicate);`; resolution failures are reported and return before binding.
- Zero targets still finalize/no-op before `ExistingProjectMutationContext.Require`.
- Canonical binding still occurs exactly once; ProjectId/ChangeVersion and current target set are revalidated before Core mutation.
- `ResolveUntrackTargetIds` remains read-only, case-insensitive deduplicated and deterministically ordered.

## Expected surfaces

- `scripts/preflight-untrack-single-bind.py`
- this claim file for close-out

## Excluded scope

- No edits to ViewportCommands.cs, SemanticUntrackService, selection behavior, post-commit UI or Core mutation semantics.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace the obsolete one-line `var previewTargetIds = ...` token with current separate declaration + assignment requirements.
- Pin the try/catch around preview target resolution and `ReportUntrackError(...); return;` before zero-target/bind flow.
- Preserve the complete existing ordering, exact one-bind, no-bootstrap and read-only resolver assertions.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for semantic untrack single-bind or this preflight.

## Completion condition

The untrack gate follows the current exception-isolated preview resolution without weakening no-op-before-bind/freshness/ownership guarantees, is pushed to `main`, and this claim is closed with exact evidence.
