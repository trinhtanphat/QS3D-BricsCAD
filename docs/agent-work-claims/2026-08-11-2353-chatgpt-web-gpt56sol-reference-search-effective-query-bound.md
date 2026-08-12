# Work claim — Construction reference search effective-query bound

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:53:00+07:00`
- Pre-registration observed main SHA: `055a30e4c338f963f786b7347a9715f6b463d92a`
- Registration parent main SHA: `d22fdee7428e65aa6c91f172c5d0915c56308e9b`
- Claim commit: `b5ea9abc76d89ca6803d9d4b5aa38bea8aaf95a1`
- Claim baseline reconciliation: `87c31ad318e44d7db2761d459c6c66bd14a1ea03`
- Priority: evidence-driven source-safe input-boundary hardening

## Coordination note

`main` advanced between the pre-registration HEAD read and the contents-API claim commit. The registration parent above is authoritative; the claim-only reconciliation was committed before any substantive source edit.

## Confirmed defect fixed

`ReferenceSearchWindow` declared `MaxQueryLength = 512` and enforced the limit for raw input plus optional technical context, but the `shorts` path appended `" video ngắn shorts"` later without checking the final effective query length. A near-limit valid query could therefore exceed the declared bound before URL encoding/browser launch.

The window now routes both QS3D-added suffixes through `AppendBoundedSuffix`, which checks `query.Length + suffix.Length > MaxQueryLength` before concatenation. The `shorts` suffix is therefore bounded before `Uri.EscapeDataString(effectiveQuery)`.

## Implementation surfaces

- `src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs`
- `scripts/preflight-reference-search-effective-query-bound.py`
- `docs/UI-REFERENCE-SEARCH-EFFECTIVE-QUERY-BOUND-2026-08-11.md`
- this claim file

## Product commits

- `4b37893e7b5a7b5aebf5237643878fb3b97e9ac0` — `fix(reference): bound internally expanded queries`
- `e4d647e3dc95adf2e6c2e8b9bdb47bf2803822d2` — `test(reference): guard effective query length bound`
- `8b4f1b444cc4a1f634b4f3aacfb562bf802be0b9` — `docs(reference): document effective query bound`

## Acceptance result

1. Raw input remains capped at 512 characters.
2. Technical-context expansion now uses the shared bounded-suffix helper.
3. `shorts` expansion now uses the same helper and is bounded before URL encoding.
4. Existing active-DWG guard, `DocumentBoundWindowLifetime`, fixed HTTPS Google search endpoints, SafeSearch flags, default-browser launch and no-scrape boundary are unchanged.
5. A focused source preflight protects these invariants.

## Validation truth

The current `main` source was re-fetched after the writes and contains the shared bounded-suffix helper plus bounded technical/`shorts` calls. The focused preflight was also re-fetched from `main`. GitHub compare from implementation commit `4b37893e7b5a7b5aebf5237643878fb3b97e9ac0` to then-current `main` reported `behind_by: 0`, with the implementation commit as merge base, so concurrent commits had not removed the source fix.

GitHub Actions were not dispatched. No full local repository checkout/build or native BricsCAD V25 runtime PASS is claimed from this remote session.

## Coordination exclusions respected

The existing construction-reference command/XAML, Start Center, Project Browser query planner, Workspace/RightPanel, quantity, persistence, grid, schedule-placement and other concurrent scopes were not edited by this lane.

## Completion condition

Satisfied for remote/source scope: QS3D-added Reference Search suffixes can no longer bypass the declared effective-query bound, and the focused regression/documentation lane is on `main` and closed.
