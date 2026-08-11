# Work claim — ProjectElement MarkClean no-op freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-element-markclean-noop-freshness`
- Registered: `2026-08-12T00:18:00+07:00`
- Baseline main SHA: `86712f56885f7b77b1de9b98b1bf8dd8dac7e02b`
- Priority: deterministic follow-up to the completed dirty-None no-op invariant

## Confirmed defect

The completed dirty-None lane made `MarkClean(ElementDirtyFlags.None)` timestamp-stable, but `MarkClean(nonEmptyFlags)` still always assigns `UpdatedUtc = DateTime.UtcNow` even when none of the requested flags are currently dirty. Repeating a clean operation on already-clean bits therefore creates a false freshness mutation without changing `Dirty`.

## Reserved scope

After existing range/None validation, make `MarkClean` return without mutation when `(Dirty & flags) == ElementDirtyFlags.None`. If any requested bit is currently dirty, preserve the existing bit clear and timestamp update.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs` (`MarkClean` only)
- `tests/QS3D.Core.SmokeTests/ProjectElementMarkCleanNoOpSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No `MarkDirty`, Category, SetProperty, SetQuantity, generated stale or relation mutation changes.
- No ProjectState ChangeVersion/regeneration/persistence/V25/UI behavior changes.
- No reinterpretation of a partial clean: if at least one requested bit is dirty, the call remains a real mutation.
- No GitHub Actions dispatch.

## Validation plan

- First clean of a dirty bit clears it and advances `UpdatedUtc`.
- Repeating the same non-empty clean when the bit is already clean leaves `Dirty` and `UpdatedUtc` unchanged.
- A multi-flag clean with one dirty and one already-clean bit remains a real mutation and advances timestamp.
- Invalid flag bits still throw before mutation.
- Existing `None` behavior remains unchanged.
- Re-fetch target after claim publication, inspect exact source diff, and read back current source/test from `main`.

## Coordination

Historical commit `0581b5db3a0e185b6855d1dbfce58282439c74e6` intentionally handled exact `None` only and preserved non-empty behavior. This follow-up owns only the now-demonstrated no-op subset case. Current concurrent claims on diagnostics/Grid/V25/enumeration/revision surfaces do not overlap.

## Completion condition

Current `main` does not advance element freshness when `MarkClean` clears no dirty bit, still updates on actual clears, includes focused deterministic regression coverage, and this claim is closed `COMPLETED`.
