# Work claim — revision dependency canonical capture

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-dependency-canonical-capture-20260812-0016`
- Registered: `2026-08-12T00:16:00+07:00`
- Baseline main SHA: `83b7ae41046db126288caa413ac7188d917cc239`
- Priority: evidence-driven Core revision integrity during owner-requested `continue all`

## Reserved scope

Make Revision capture fail closed on non-canonical semantic dependency lists instead of silently trimming, dropping blanks or deduplicating them before snapshot creation.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- focused Core smoke regression for revision dependency capture integrity
- this claim file for close-out

## Concrete defect

`ProjectElement.DependsOn` is a mutable public list. `RevisionService.Capture()` currently routes it through `CanonicalDependencies()`, which trims entries and silently skips blank/duplicate dependencies. In contrast, `RevisionService.Index()` and `RevisionSnapshotStore.ValidateSnapshot()` require dependency values to be canonical, non-empty and unique. Capture can therefore hide corrupt source dependency state and produce a clean-looking revision artifact that no longer faithfully represents the project being audited.

## Contract

- Capture must reject blank dependency IDs.
- Capture must reject leading/trailing whitespace rather than silently trim it.
- Capture must reject case-insensitive duplicate dependency IDs rather than silently deduplicate them.
- Canonical dependency lists remain deterministically sorted case-insensitively in snapshots.

## Explicit exclusions

- No dependency graph semantics or cycle policy changes.
- No revision XML/schema/file-size, source-handle, quantity math, comparison semantics, V25/native/UI, Actions/release/LOCAL_PASS work.

## Validation plan

- Canonical dependency lists capture successfully and are deterministically ordered.
- Blank, padded and case-insensitive duplicate dependencies fail during capture.
- Existing comparison/store canonical validation remains unchanged.
- Re-fetch/compare moving `main`, publish through a feature branch/PR without force-push, then re-read remote `main` after integration.

## Completion condition

Revision capture no longer normalizes away malformed dependency evidence, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.
