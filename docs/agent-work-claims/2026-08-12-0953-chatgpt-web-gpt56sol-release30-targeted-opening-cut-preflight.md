# Work claim — release #30 targeted opening-cut preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-targeted-opening-cut-preflight`
- Registered: `2026-08-12T09:53:00+07:00`
- Baseline main SHA: `2fd8a0f6a0f38ee4123bd18ad8902b15cb34d392`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports two targeted-opening-cut failures after Direct Draw Auto Host was intentionally narrowed to exact single-opening linking and selected-cut target resolution moved behind a helper.

## Reserved scope

Reconcile only `scripts/preflight-targeted-opening-cut.py` with the current exact Auto Host and helper-based selected-opening contracts. Preserve opening boolean/Direct Draw production source unchanged.

## Canonical evidence

- Direct Draw Door/WallOpening now calls `AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)` against the exact project/created opening and explicitly avoids broad pick-set AutoHost re-entry.
- Physical boolean cutting remains explicit; Direct Draw still contains no `CutLinkedOpenings`, OpeningBooleanCommands cut call or `QS3DCUT*` command dispatch.
- `QS3DCUTSELECTEDOPENINGS` reads current selection, resolves `openingIds = ResolveOpeningIds(previewProject, handles)`, binds canonical state once, re-resolves current ids, compares exact target sets, then executes the targeted overload.
- `ResolveOpeningIds` filters through `.Where(IsOpening)`, semantic selection matching, `.Distinct(StringComparer.OrdinalIgnoreCase)` and deterministic ordering.
- The gate currently searches those helper internals only inside the command entry slice and still requires obsolete broad `new AutoHostLinkCommands().AutoLinkHosts()`.

## Expected surfaces

- `scripts/preflight-targeted-opening-cut.py`
- this claim file for close-out

## Excluded scope

- No edits to DirectDrawOpeningCommands.cs, OpeningBooleanCommands.cs, OpeningBooleanService.cs, docs/UI/ribbon or AutoHost implementation.
- No automatic physical cut and no broad AutoHost command re-entry.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace the obsolete Direct Draw broad AutoHost requirement with exact `LinkSingleOpening(...)` and explicitly fail if `new AutoHostLinkCommands().AutoLinkHosts()` returns.
- Require selected command to call `ResolveOpeningIds` before and after canonical binding, compare target sets, and keep targeted `CutLinkedOpenings(..., openingIds)`.
- Validate `.Where(IsOpening)` and `.Distinct(...)` in the actual `ResolveOpeningIds` helper slice rather than only the command entry body.
- Retain all existing physical-cut service guards, UI/docs wiring, targeted-overload normalization and Direct Draw no-boolean prohibitions.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for the targeted opening-cut preflight. Current Structural Wall Opening Host claim is a different Core/domain lane and does not reserve these files.

## Completion condition

The targeted opening-cut gate follows exact single-opening Auto Host and helper-based deduplicated selected targets without weakening explicit-cut/ownership safety, is pushed to `main`, and this claim is closed with exact evidence.
