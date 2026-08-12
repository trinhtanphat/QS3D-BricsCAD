# Work claim — Start Center search bound

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-start-center-search-bound`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `6936bd427c02b4766f4b7af6bdecb58cc275afb8`
- Completed source commit: `a0333f654d79c8a44870751068741b4ff5797d75`
- Regression commit: `715d798603f8b28448294f47c06d9da944f781fc`
- Readback main SHA before close-out: `924a861697e4f0b54660d0449f334922dccd4393`
- Priority: P1 modeless UI responsiveness / bounded-input hardening found during owner-requested `continue all` audit.

## Confirmed defect

`StartCenterCommandCatalog.Search()` accepted an unbounded free-text query from the modeless WPF `SearchBox`; every `TextChanged` synchronously refreshed results on the UI thread. `Search()` split the entire query into an unbounded term array, and `Score()` called `ScoreTerm()` for every term and every catalog item. `ScoreTerm()` repeatedly Unicode-normalized the command/title/group/description/keywords for each term. A very large pasted query could therefore force unbounded synchronous CPU/allocation work and make the Start Center UI unresponsive.

The existing malformed-Unicode guard prevented normalization exceptions but did not bound work size.

## Implemented contract

1. `MaxSearchQueryChars = 512` bounds the trimmed launcher query before tokenization.
2. Truncation avoids splitting a UTF-16 surrogate pair at the 512-character boundary.
3. `MaxSearchTerms = 16` caps the token array before ranking/scoring.
4. Existing group filtering, accent-insensitive folding, AND semantics, priorities and catalog contents remain unchanged inside the accepted bound.
5. No command allowlist, dispatch, Favorites/Recent state, XAML, Core or project mutation behavior changed.
6. The canonical Start Center preflight pins both bounds plus the required `query-bound -> split -> term-bound -> ranking` ordering.

## Verification

- Current-main catalog readback confirmed the 512-character and 16-term bounds plus surrogate-pair preservation.
- Current-main preflight readback confirmed the bounded-search assertions and `search-bounded` contract marker.
- `715d798603f8b28448294f47c06d9da944f781fc...main` compared as `ahead` with the regression commit as merge base; concurrent commits after it touched unrelated claims/Core/runtime-health/V26 files.
- The first regression write attempt received a GitHub 409 because another agent updated the canonical preflight concurrently. The gate was re-fetched and the search assertions were merged onto the current blob without force/overwrite; the successful regression commit is `715d798603f8b28448294f47c06d9da944f781fc`.
- Full preflight execution, adapter compile, GitHub Actions and licensed BricsCAD V25 runtime responsiveness testing were not run from this remote connector session; no PASS is fabricated.
- Native WPF responsiveness validation remains `LOCAL_ONLY` under the existing local validation queue.

## Excluded

- No XAML redesign.
- No command catalog additions/removals/renames.
- No command dispatch changes.
- No state-store/Core/project mutation changes.
