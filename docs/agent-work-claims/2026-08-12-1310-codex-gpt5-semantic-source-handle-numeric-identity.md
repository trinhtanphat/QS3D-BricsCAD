# Work claim — Semantic SourceHandle numeric CAD identity

- Status: `COMPLETED`
- Agent: `codex-gpt5-audit-blt-notes-latest` (`/root/audit_blt_notes_latest`)
- Registered: `2026-08-12T13:10:13+07:00`
- Baseline main SHA: `688827f27ff832dbc380a2a7f82353eb956471e7`
- Priority: `P0` — semantic source ownership and capture must use the same numeric CAD Handle identity as BricsCAD and the completed shared generated-ownership policy.

## Confirmed defect

BricsCAD and `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` treat `A`, `0A`, and `0xA` as one positive hexadecimal CAD Handle identity. `SemanticHandleOwnershipResolver` still trims and compares stored/query/selected SourceHandles only as case-insensitive text. A project can therefore hide two semantic owners behind aliases of the same native object, selection can resolve only the textual alias it was given, and semantic capture can fail to find stored owner `0A` when the live BricsCAD snapshot reports `A`, then return `null` and create a second semantic element.

## Reserved scope

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipDuplicateSourceSmoke.cs`
- `scripts/preflight-semantic-source-handle-numeric-identity.py` (new focused auto-discovered gate)
- this claim file for close-out

## Intended contract

- Use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` consistently for semantic source query, stored-source duplicate/owner keys, caller selection inputs, and the resolver owner/channel dictionaries.
- Treat positive hexadecimal aliases such as `A`, `0A`, and `0xA` as one semantic SourceHandle identity.
- Fail closed when one element stores numeric aliases twice or different semantic elements claim numeric aliases of the same CAD object.
- Resolve an existing alias owner for capture rather than returning `null`; normalize duplicate caller selections to one owner.
- Preserve current empty/padded stored-value rejection, caller input bound, ChangeVersion/structural freshness, generated logical-slot behavior, and malformed/zero/non-resolvable textual identity compatibility.
- Do not rewrite persisted SourceHandle spelling.

## Excluded scope

- No QSDB/interchange schema or persistence normalization.
- No `SourceHandleResolver`, reporting/BQ/ED2 exporters/readers, BricsCAD command/UI/runtime, CAD selection, native capture mutation, generated health/provider, builder or project-model changes.
- No takeover of active Create Similar, Core persistence/session atomicity, semantic-selection instance override, standalone rebar-family health, Build3D, LOCAL-002 or LOCAL-003 surfaces.
- No private/customer fixtures, GitHub Actions, release/package work or BricsCAD V25/V26 execution.

## Validation plan

- Extend the already-registered duplicate-source smoke with same-element numeric alias rejection, cross-element numeric ambiguity across all resolver entry points, alias capture-owner reuse, selected-alias deduplication, malformed textual compatibility, and failure-side ChangeVersion stability.
- Add a focused static gate pinning shared policy use and the numeric-alias regression cases.
- Run the focused gate/smoke, Core Release build/full smoke, smoke-registration/general preflight and aggregate preflight as the moving source-only baseline permits; record unrelated blockers exactly.
- Run `git diff --check` and re-read merged source/test/gate from current `main`.

## Coordination

All `ACTIVE` / `BLOCKED` claims and open PRs were re-read at the baseline. None reserves `SemanticHandleOwnershipResolver.cs`, the existing duplicate-source smoke, or the proposed gate. The completed generated numeric-identity lane supplies the shared normalization policy but did not change semantic source selection/capture. The active Core atomicity lane is narrowed to QSDB recovery and `ProjectSession.Save()`; the semantic-selection override owns a different Selection service; the active standalone rebar-family lane owns six diagnostics providers only.

## Completion condition

The bounded resolver/smoke/gate batch is merged into current `main`, exact source-only validation and any unrelated baseline blockers are recorded, the claim is marked `COMPLETED` with exact PR/commit SHAs, and no prohibited runtime/private/Actions operation has occurred.

## Completion evidence

- Implementation commit `66115d5fa7632b6b35c2f567aff4d418710753a0` was merged by PR `#947` as `577ba66401e02650e01ae7737912ba859d8aecb5`.
- Core Release and SmokeTests Release builds completed with zero warnings/errors. The new self-registered semantic-handle smoke completed before the full current-main smoke later stopped at the unrelated pre-existing `SemanticTagRendererSmoke` owner-ID fixture.
- `scripts/preflight-semantic-source-handle-numeric-identity.py`, `scripts/preflight.py`, smoke-registration and `git diff --check` passed.
- No GitHub Actions, BricsCAD runtime, private/customer fixture, release or package operation was used.
