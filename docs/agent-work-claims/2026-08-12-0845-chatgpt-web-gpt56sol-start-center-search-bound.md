# Work claim — Start Center search bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-search-bound`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `6936bd427c02b4766f4b7af6bdecb58cc275afb8`
- Priority: P1 modeless UI responsiveness / bounded-input hardening found during owner-requested `continue all` audit.

## Confirmed defect

`StartCenterCommandCatalog.Search()` accepts an unbounded free-text query from the modeless WPF `SearchBox`; every `TextChanged` synchronously refreshes results on the UI thread. `Search()` splits the entire query into an unbounded term array, and `Score()` calls `ScoreTerm()` for every term and every catalog item. `ScoreTerm()` repeatedly Unicode-normalizes the command/title/group/description/keywords for each term. A very large pasted query can therefore force unbounded synchronous CPU/allocation work and make the Start Center UI unresponsive.

The existing malformed-Unicode guard prevents normalization exceptions but does not bound work size. Search is read-only, so a deterministic bounded prefix/term policy can preserve normal UX without affecting command dispatch or project state.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/StartCenterCommandCatalog.cs`
- `scripts/preflight-start-center.py`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current catalog/preflight and this claim before writes.
2. Add explicit small constants for maximum search query characters and term count.
3. Trim the free-text query, cap it before splitting, and cap the resulting terms before ranking; preserve current accent-insensitive AND semantics inside the accepted bound.
4. Do not alter the command allowlist, priorities, grouping, dispatch, Favorites/Recent state, or project behavior.
5. Extend canonical Start Center preflight to pin both query and term bounds before scoring.
6. Read back current `main`; do not dispatch Actions and do not claim BricsCAD runtime PASS remotely.
7. Close claim after source/regression commits remain visible on current `main`.

## Excluded

- No XAML redesign.
- No command catalog additions/removals/renames.
- No command dispatch changes.
- No state-store/Core/project mutation changes.
- Native WPF responsiveness validation remains `LOCAL_ONLY`.
